namespace Systems;

using System;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;

[With(typeof(IntercellularMatrix))]
[With(typeof(SpatialInstance))]
[With(typeof(AttachedToEntity))]
[Without(typeof(MicrobeColony))]
[ReadsComponent(typeof(MicrobeColonyMember))]
[RuntimeCost(1.0f)]
[RunsOnMainThread]
public sealed class IntercellularMatrixSystem : AEntitySetSystem<float>
{
    private static readonly Lazy<PackedScene> ConnectionScene =
        new(() => GD.Load<PackedScene>("res://src/multicellular_stage/IntecellularConnection.tscn"));

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

        var connection = ConnectionScene.Value.Instantiate<Node3D>();

        instance.AddChild(connection);

        // Get cells' positions relative to the colony leader using transform math
        var colonyTransform = colony.Leader.Get<WorldPosition>().ToTransform().Inverse();
        var ourPos = colonyTransform * entity.Get<WorldPosition>().Position;
        var targetPos = colonyTransform * parentEntity.Get<WorldPosition>().Position;

        var relativePosition = targetPos - ourPos;

        connection.Scale = Vector3.One * relativePosition.Length();

        var angle = relativePosition.AngleTo(Vector3.Forward);

        if (relativePosition.X > 0.0f)
            angle *= -1.0f;

        connection.RotateY(angle);

        connection.Position += relativePosition * 0.5f;

        intercellularMatrix.GeneratedConnection = connection;
    }

    private static void RemoveConnection(in Entity entity, ref IntercellularMatrix intercellularMatrix)
    {
        intercellularMatrix.GeneratedConnection?.QueueFree();
    }
}
