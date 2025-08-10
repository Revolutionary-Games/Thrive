namespace Systems;

using System;
using Components;
using DefaultEcs;
using DefaultEcs.System;
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

    private static readonly StringName TintParameter = new("tint");

    public IntercellularMatrixSystem(World world) : base(world, null)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var matrix = ref entity.Get<IntercellularMatrix>();

        if (entity.Has<MicrobeColonyMember>())
        {
            if (!matrix.IsConnectionRedundant && matrix.GeneratedConnection == null)
            {
                ref var colony = ref entity.Get<MicrobeColonyMember>().ColonyLeader.Get<MicrobeColony>();

                AddIntercellularConnection(entity, ref matrix, ref colony);
            }
        }
        else
        {
            if (matrix.GeneratedConnection != null)
            {
                RemoveConnection(ref matrix);
            }
        }
    }

    private static void AddIntercellularConnection(in Entity entity, ref IntercellularMatrix intercellularMatrix,
        ref MicrobeColony colony)
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

        Vector3 targetRelativePos;

        Quaternion ourRotation = Quaternion.Identity;
        Quaternion targetRotation = Quaternion.Identity;

        if (parentEntity == colony.Leader)
        {
            var ourAttachedPosition = entity.Get<AttachedToEntity>();

            targetRelativePos = -ourAttachedPosition.RelativePosition;
            ourRotation = ourAttachedPosition.RelativeRotation;
        }
        else
        {
            var ourAttachedPosition = entity.Get<AttachedToEntity>();
            var targetAttachedPosition = parentEntity.Get<AttachedToEntity>();

            targetRelativePos = targetAttachedPosition.RelativePosition
                - ourAttachedPosition.RelativePosition;

            ourRotation = ourAttachedPosition.RelativeRotation;
            targetRotation = targetAttachedPosition.RelativeRotation;
        }

        Vector3 pointA, pointB;
        (pointA, pointB) = FindGoodConnectionPoints(ourMembrane.MembraneData,
            targetMembrane.MembraneData, targetRelativePos, ourRotation, targetRotation);

        var relativePosition = pointB - pointA;
        float relativePosLength = relativePosition.Length();

        if (relativePosLength < 0.5f)
        {
            intercellularMatrix.IsConnectionRedundant = true;
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

        intercellularMatrix.GeneratedConnection = connection;

        ApplyConnectionMaterialParameters(entity, ref intercellularMatrix);
    }

    private static (Vector3 PointA, Vector3 PointB) FindGoodConnectionPoints(MembranePointData membraneA,
    MembranePointData membraneB, Vector3 membraneBOffset, Quaternion rotationA, Quaternion rotationB)
    {
        float min = float.MaxValue;
        Vector3 pointA = Vector3.Zero;
        Vector3 pointB = membraneBOffset;
        foreach (var a in membraneA.Vertices2D)
        {
            var rotatedA = new Vector3(a.X, 0.0f, a.Y);

            foreach (var b in membraneB.Vertices2D)
            {
                // First rotate the vertex by membrane B rotation
                // Then inversely rotate it by A's rotation to get the true relative coordinates
                var rotatedB = (rotationB * new Vector3(b.X, 0.0f, b.Y) + membraneBOffset) * rotationA;

                float distance = rotatedA.DistanceSquaredTo(rotatedB);

                if (distance < min)
                {
                    min = distance;
                    pointA = rotatedA;
                    pointB = rotatedB;
                }
            }
        }

        return (pointA, pointB);
    }

    private static void RemoveConnection(ref IntercellularMatrix intercellularMatrix)
    {
        intercellularMatrix.GeneratedConnection?.QueueFree();
        intercellularMatrix.GeneratedConnection = null;
        intercellularMatrix.IsConnectionRedundant = false;
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
}
