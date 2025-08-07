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

        // Get target's relative position (taking rotation into account) using inverse transform multiplication
        var inverseColonyTransform = entity.Get<WorldPosition>().ToTransform().Inverse();
        var targetRelativePos = inverseColonyTransform * parentEntity.Get<WorldPosition>().Position;

        Vector3 pointA, pointB;
        (pointA, pointB) = FindGoodConnectionPoints(ourMembrane.MembraneData,
            targetMembrane.MembraneData, targetRelativePos);

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
        connection.RotateY(angle);
        connection.Position += (pointA + pointB) * 0.5f;

        intercellularMatrix.GeneratedConnection = connection;

        ApplyConnectionMaterialParameters(entity, ref intercellularMatrix);
    }

    private static (Vector3 PointA, Vector3 PointB) FindGoodConnectionPoints(MembranePointData membraneA,
        MembranePointData membraneB, Vector3 membraneBOffset)
    {
        Vector2 offset2D = new Vector2(membraneBOffset.X, membraneBOffset.Z);

        float min = float.MaxValue;
        Vector2 pointA = Vector2.Zero;
        Vector2 pointB = Vector2.Zero;
        foreach (var a in membraneA.Vertices2D)
        {
            foreach (var b in membraneB.Vertices2D)
            {
                float distance = a.DistanceSquaredTo(b + offset2D);

                if (distance < min)
                {
                    min = distance;
                    pointA = a;
                    pointB = b;
                }
            }
        }

        return (new Vector3(pointA.X, 0.0f, pointA.Y),
            new Vector3(pointB.X, 0.0f, pointB.Y) + membraneBOffset);
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
