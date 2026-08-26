namespace Systems;

using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Generates intercellular graphical connections between cells
/// </summary>
[ReadsComponent(typeof(MicrobeColonyMember))]
[ReadsComponent(typeof(SpatialInstance))]
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(SpatialAnimation))]
[ReadsComponent(typeof(MicrobeColony))]
[ReadsComponent(typeof(AttachedToEntity))]
[RuntimeCost(0.25f)]
[RunsOnMainThread]
public partial class IntercellularMatrixSystem : BaseSystem<World, float>
{
    private static readonly Lazy<PackedScene> ConnectionScene =
        new(() => GD.Load<PackedScene>("res://src/multicellular_stage/IntercellularConnection.tscn"));

    private static readonly StringName TintParameter = new("tint");

    public IntercellularMatrixSystem(World world) : base(world)
    {
    }

    private static void AddIntercellularConnection(in Entity entity, ref IntercellularMatrix intercellularMatrix,
        ref MicrobeColony colony, ref SpatialInstance spatialInstance, ref CellProperties cellProperties,
        ref AttachedToEntity ourAttachedPosition)
    {
        Entity parentEntity;

        try
        {
            parentEntity = colony.ColonyStructure[entity];
        }
        catch (Exception e)
        {
            GD.PrintErr("Invalid colony structure data in intercellular matrix creation: ", e);
            return;
        }

        var instance = spatialInstance.GraphicalInstance;
        if (instance == null)
        {
            GD.PrintErr("Tried to add an intercellular connection while a cell's graphical instance is null");
            return;
        }

        var ourMembrane = cellProperties.CreatedMembrane;
        var targetMembrane = parentEntity.Get<CellProperties>().CreatedMembrane;
        if (ourMembrane?.IsMulticellular != true || targetMembrane?.IsMulticellular != true ||
            ourMembrane?.IsChangingShape == true || targetMembrane?.IsChangingShape == true)
            return;

        Vector3 targetRelativePos;

        Quaternion ourRotation;
        Quaternion targetRotation = Quaternion.Identity;

        if (parentEntity == colony.Leader)
        {
            targetRelativePos = -ourAttachedPosition.RelativePosition;
            ourRotation = ourAttachedPosition.RelativeRotation;

            if (entity.TryGet<SpatialAnimation>(out var animation))
            {
                targetRelativePos = -animation.FinalPosition;
            }
        }
        else
        {
            ref var targetAttachedPosition = ref parentEntity.Get<AttachedToEntity>();

            targetRelativePos = targetAttachedPosition.RelativePosition
                - ourAttachedPosition.RelativePosition;

            ourRotation = ourAttachedPosition.RelativeRotation;
            targetRotation = targetAttachedPosition.RelativeRotation;

            if (entity.TryGet<SpatialAnimation>(out var animation))
            {
                if (parentEntity.TryGet<SpatialAnimation>(out var parentAnimation))
                {
                    targetRelativePos = parentAnimation.FinalPosition - animation.FinalPosition;
                }
                else
                {
                    targetRelativePos = targetAttachedPosition.RelativePosition - animation.FinalPosition;
                }
            }
        }

        var (pointA, pointB) = FindGoodConnectionPoints(ourMembrane.MembraneData,
            targetMembrane.MembraneData, targetRelativePos, ourRotation, targetRotation);

        var relativePosition = pointB - pointA;
        float relativePosLength = relativePosition.Length();

        if (relativePosLength < 0.5f)
        {
            intercellularMatrix.RemoveConnection();
            intercellularMatrix.ShouldRegenerateConnection = false;
            return;
        }

        var angle = relativePosition.AngleTo(Vector3.Forward);
        if (relativePosition.X > 0.0f)
            angle *= -1.0f;

        var connection = ConnectionScene.Value.Instantiate<Node3D>();
        instance.AddChild(connection);

        connection.Scale = new Vector3(5.0f, 1.0f, relativePosLength + 3.0f);
        connection.Quaternion = Quaternion.FromEuler(new Vector3(0.0f, angle, 0.0f));
        connection.Position += (pointA + pointB) * 0.5f;

        intercellularMatrix.RemoveConnection();
        intercellularMatrix.ShouldRegenerateConnection = false;
        intercellularMatrix.GeneratedConnection = connection;

        ApplyConnectionMaterialParameters(entity, ref intercellularMatrix);
    }

    private static (Vector3 PointA, Vector3 PointB) FindGoodConnectionPoints(MembranePointData membraneA,
        MembranePointData membraneB, Vector3 membraneBOffset, Quaternion rotationA, Quaternion rotationB)
    {
        // Centroids of both membranes, expressed in membrane A's local coordinate frame.
        var centroidA = new Vector3(membraneA.AverageVertex.X, 0.0f, membraneA.AverageVertex.Y);
        var centroidB = (rotationB * new Vector3(membraneB.AverageVertex.X, 0.0f, membraneB.AverageVertex.Y)
            + membraneBOffset) * rotationA;

        var segmentStart = new Vector2(centroidA.X, centroidA.Z);
        var segmentEnd = new Vector2(centroidB.X, centroidB.Z);

        // Both membranes are convex, so the line between their centroids is guaranteed
        // to cross each membrane's boundary exactly once.
        Vector3 pointA, pointB;
        if (FindBoundaryCrossingA(segmentStart, segmentEnd, membraneA, out var crossingA))
        {
            pointA = new Vector3(crossingA.X, 0.0f, crossingA.Y);
        }
        else
        {
            GD.PrintErr("Failed to find boundary crossing for membrane A, using centroid instead");
            pointA = centroidA;
        }

        if (FindBoundaryCrossingB(segmentStart, segmentEnd, membraneB, membraneBOffset, rotationB,
                rotationA, out var crossingB))
        {
            pointB = new Vector3(crossingB.X, 0.0f, crossingB.Y);
        }
        else
        {
            GD.PrintErr("Failed to find boundary crossing for membrane B, using centroid instead");
            pointB = centroidB;
        }

        return (pointA, pointB);
    }

    private static bool FindBoundaryCrossingA(Vector2 segmentStart, Vector2 segmentEnd,
        MembranePointData membrane, out Vector2 crossing)
    {
        crossing = default;
        int count = membrane.VertexCount;
        var previous = membrane.Vertices2D[count - 1];

        for (int i = 0; i < count; ++i)
        {
            var current = membrane.Vertices2D[i];

            if (TryGetSegmentIntersection(segmentStart, segmentEnd, previous, current, out crossing))
                return true;

            previous = current;
        }

        return false;
    }

    private static bool FindBoundaryCrossingB(Vector2 segmentStart, Vector2 segmentEnd,
        MembranePointData membrane, Vector3 offset, Quaternion rotation, Quaternion rotationA, out Vector2 crossing)
    {
        crossing = default;
        int count = membrane.VertexCount;
        var previous = ToMembraneAFrame(membrane.Vertices2D[count - 1], offset, rotation, rotationA);

        for (int i = 0; i < count; ++i)
        {
            var current = ToMembraneAFrame(membrane.Vertices2D[i], offset, rotation, rotationA);

            if (TryGetSegmentIntersection(segmentStart, segmentEnd, previous, current, out crossing))
                return true;

            previous = current;
        }

        return false;
    }

    private static Vector2 ToMembraneAFrame(Vector2 point, Vector3 offset, Quaternion rotation,
        Quaternion rotationA)
    {
        var transformed = (rotation * new Vector3(point.X, 0.0f, point.Y) + offset) * rotationA;
        return new Vector2(transformed.X, transformed.Z);
    }

    /// <summary>
    ///   Tests for 2 segments intersection. Returns the intersection point.
    /// </summary>
    private static bool TryGetSegmentIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 intersection)
    {
        intersection = default;
        var segmentA = b - a;
        var segmentB = d - c;

        float denominator = segmentA.Cross(segmentB);

        // Skip parallel (or almost) edges
        if (Math.Abs(denominator) < MathUtils.EPSILON)
        {
            return false;
        }

        var diff = c - a;

        float pointAlongAb = diff.Cross(segmentB) / denominator;
        float pointAlongCd = diff.Cross(segmentA) / denominator;

        if (pointAlongAb is >= 0.0f and <= 1.0f && pointAlongCd is >= 0.0f and <= 1.0f)
        {
            intersection = a + segmentA * pointAlongAb;
            return true;
        }

        return false;
    }

    private static void ApplyConnectionMaterialParameters(in Entity entity,
        ref IntercellularMatrix intercellularMatrix)
    {
        if (intercellularMatrix.GeneratedConnection == null)
        {
            GD.PrintErr("Intercellular connection is null, can't apply material parameters");
            return;
        }

        var material = ((GeometryInstance3D)intercellularMatrix.GeneratedConnection).MaterialOverride;
        ((ShaderMaterial)material).SetShaderParameter(TintParameter, entity.Get<CellProperties>().Colour);
    }

    [Query]
    [None<MicrobeColony>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref IntercellularMatrix matrix, ref SpatialInstance spatialInstance,
        ref CellProperties cellProperties, in Entity entity)
    {
        if (entity.Has<MicrobeColonyMember>())
        {
            if (matrix.ShouldRegenerateConnection)
            {
                var leader = entity.Get<MicrobeColonyMember>().ColonyLeader;

                if (!leader.IsAliveAndHas<MicrobeColony>())
                {
                    GD.PrintErr($"Leader of a colony is missing or missing MicrobeColony component, " +
                        $"can't generate intercellular matrix for {entity}");
                    return;
                }

                ref var colony = ref leader.Get<MicrobeColony>();

                ref var attachedTo = ref entity.Get<AttachedToEntity>();
                AddIntercellularConnection(entity, ref matrix, ref colony, ref spatialInstance, ref cellProperties,
                    ref attachedTo);
            }
        }
        else
        {
            if (matrix.GeneratedConnection != null)
            {
                matrix.RemoveConnection();
            }
        }
    }
}
