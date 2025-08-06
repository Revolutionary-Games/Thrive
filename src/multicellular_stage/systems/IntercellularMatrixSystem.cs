namespace Systems;

using System;
using System.Collections.Generic;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;

[With(typeof(IntercellularMatrix))]
[With(typeof(SpatialInstance))]
[Without(typeof(MicrobeColony))]
[ReadsComponent(typeof(MicrobeColonyMember))]
[ReadsComponent(typeof(SpatialInstance))]
[ReadsComponent(typeof(CellProperties))]
[RuntimeCost(1.0f)]
[RunsOnMainThread]
public sealed class IntercellularMatrixSystem : AEntitySetSystem<float>
{
    private static readonly Lazy<PackedScene> ConnectionScene =
        new(() => GD.Load<PackedScene>("res://src/multicellular_stage/IntercellularConnection.tscn"));

    private static readonly List<ShaderMaterial> TempMaterialList = new();

    public IntercellularMatrixSystem(World world) : base(world, null)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var matrix = ref entity.Get<IntercellularMatrix>();

        if (entity.Has<MicrobeColonyMember>())
        {
            if (matrix.GeneratedConnection == null)
            {
                ref var colony = ref entity.Get<MicrobeColonyMember>().ColonyLeader.Get<MicrobeColony>();

                AddIntercellularConnection(entity, ref matrix, ref colony, ref entity.Get<AttachedToEntity>());
            }
        }
        else
        {
            if (matrix.GeneratedConnection != null)
            {
                RemoveConnection(entity, ref matrix);
            }
        }
    }

    private static void AddIntercellularConnection(in Entity entity, ref IntercellularMatrix intercellularMatrix,
        ref MicrobeColony colony, ref AttachedToEntity attachPosition)
    {
        Entity parentEntity = colony.ColonyStructure[entity];

        var instance = entity.Get<SpatialInstance>().GraphicalInstance;
        if (instance == null)
        {
            GD.PrintErr("Tried to add an intercellular connection while a cell's graphical instance is null");
            return;
        }

        var ourMembrane = entity.Get<CellProperties>().CreatedMembrane;
        var targetMembrane = parentEntity.Get<CellProperties>().CreatedMembrane;
        if (ourMembrane == null || targetMembrane == null)
            return;

        var connection = ConnectionScene.Value.Instantiate<Node3D>();
        instance.AddChild(connection);

        // Get target's relative position (taking rotation into account) using inverse transform multiplication
        var inverseColonyTransform = entity.Get<WorldPosition>().ToTransform().Inverse();
        var targetRelativePos = inverseColonyTransform * parentEntity.Get<WorldPosition>().Position;

        Vector3 pointA, pointB;
        (pointA, pointB) = FindGoodConnectionPoints(ourMembrane.MembraneData,
            targetMembrane.MembraneData, targetRelativePos);

        var relativePosition = pointB - pointA;
        float relativePosLength = relativePosition.Length();

        var angle = relativePosition.AngleTo(Vector3.Forward);
        if (relativePosition.X > 0.0f)
            angle *= -1.0f;

        connection.Scale = new Vector3(5.0f, 1.0f, relativePosLength + 3.0f);
        connection.RotateY(angle);
        connection.Position += (pointA + pointB) * 0.5f;

        intercellularMatrix.GeneratedConnection = connection;

        ApplyConnectionMaterialParameters(entity, ref intercellularMatrix);
    }

    private static (Vector3 PointA, Vector3 PointB) FindGoodConnectionPoints(MembranePointData membraneA,
        MembranePointData membraneB, Vector3 membraneBOffset)
    {
        Vector2 closestVertexB = FindClosestVertex(Vector2.Zero, membraneB.Vertices2D,
            new Vector2(membraneBOffset.X, membraneBOffset.Z));

        Vector2 closestVertexA = FindClosestVertex(closestVertexB, membraneA.Vertices2D, Vector2.Zero);

        return (new Vector3(closestVertexA.X, 0.0f, closestVertexA.Y),
            new Vector3(closestVertexB.X, 0.0f, closestVertexB.Y));
    }

    private static Vector2 FindClosestVertex(Vector2 target, Vector2[] vertices, Vector2 offset)
    {
        Vector2 closestVertex = Vector2.Zero;
        float minDistance = float.MaxValue;
        foreach (var vertex in vertices)
        {
            var relativeVertex = vertex + offset;

            float squareDistance = relativeVertex.DistanceSquaredTo(target);

            if (squareDistance < minDistance)
            {
                closestVertex = relativeVertex;
                minDistance = squareDistance;
            }
        }

        return closestVertex;
    }

    private static void RemoveConnection(in Entity entity, ref IntercellularMatrix intercellularMatrix)
    {
        intercellularMatrix.GeneratedConnection?.QueueFree();
        intercellularMatrix.GeneratedConnection = null;
    }

    private static void ApplyConnectionMaterialParameters(in Entity entity,
        ref IntercellularMatrix intercellularMatrix)
    {
        if (intercellularMatrix.GeneratedConnection == null)
        {
            GD.PrintErr("Intercellular connection is null, can't apply material parameters");
            return;
        }

        TempMaterialList.Clear();
        intercellularMatrix.GeneratedConnection.GetMaterial(TempMaterialList);

        foreach (var material in TempMaterialList)
        {
            material.SetShaderParameter("tint", entity.Get<CellProperties>().Colour);
        }
    }
}
