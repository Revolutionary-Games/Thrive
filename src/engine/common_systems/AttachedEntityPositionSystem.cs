namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;

/// <summary>
///   Handles positioning of entities attached to each other
/// </summary>
[ReadsComponent(typeof(AttachedToEntity))]
[RunsAfter(typeof(PhysicsUpdateAndPositionSystem))]
[RunsBefore(typeof(SpatialPositionSystem))]
[RuntimeCost(0.5f)]
public partial class AttachedEntityPositionSystem : BaseSystem<World, float>
{
    private readonly IWorldSimulation worldSimulation;

    public AttachedEntityPositionSystem(IWorldSimulation worldSimulation, World world) : base(world)
    {
        this.worldSimulation = worldSimulation;
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref AttachedToEntity attachInfo, ref WorldPosition position, in Entity entity)
    {
        if (!attachInfo.AttachedTo.Has<WorldPosition>())
        {
            // This can happen if the entity is dead now

            if (attachInfo.AttachedTo != default(Entity) && !attachInfo.AttachedTo.IsAlive())
            {
                // Delete this dependent entity if configured to do so
                // TODO: could this probably switch this to use child entity feature in Arch
                if (attachInfo.DeleteIfTargetIsDeleted)
                {
                    worldSimulation.DestroyEntity(entity);
                }
            }

            return;
        }

        // TODO: optimize for attached entities where the position / parent position doesn't change each frame?

        ref var parentPosition = ref attachInfo.AttachedTo.Get<WorldPosition>();

        position.Position = parentPosition.Position + parentPosition.Rotation * attachInfo.RelativePosition;
        position.Rotation = parentPosition.Rotation * attachInfo.RelativeRotation;
    }
}
