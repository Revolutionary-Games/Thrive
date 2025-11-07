namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using World = Arch.Core.World;

/// <summary>
///   Handles turning off collisions between dividing cells, so they can smoothly overlap before eventually moving
///   away from each other.
/// </summary>
[RuntimeCost(1)]
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(WorldPosition))]
[ReadsComponent(typeof(CellDivisionCollisionDisabler))]
[WritesToComponent(typeof(CollisionManagement))]
[RunsBefore(typeof(PhysicsCollisionManagementSystem))]
public partial class MicrobeDivisionClippingSystem : BaseSystem<World, float>
{
    private readonly PhysicalWorld physicalWorld;

    public MicrobeDivisionClippingSystem(PhysicalWorld physicalWorld, World world) : base(world)
    {
        this.physicalWorld = physicalWorld;
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref CellDivisionCollisionDisabler collisionDisabler,
        ref CollisionManagement collisionManagement, ref CellProperties cellProperties,
        ref WorldPosition worldPosition, ref Physics physics, in Entity entity)
    {
        ref var otherEntity = ref collisionDisabler.IgnoredCollisionWith;

        collisionDisabler.SeparationForce += delta * 4;

        if (collisionManagement.IgnoredCollisionsWith != null)
        {
            if (!collisionManagement.IgnoredCollisionsWith.Contains(otherEntity))
                collisionManagement.AddTemporaryCollisionIgnoreWith(otherEntity);
        }
        else
        {
            collisionManagement.AddTemporaryCollisionIgnoreWith(otherEntity);
        }

        if (otherEntity.IsAliveAndHas<WorldPosition>())
        {
            if (cellProperties.Radius == 0)
                return;

            // 2.2 = 2 (radiuses) * 1.1
            var clipOutDistanceSquared = cellProperties.Radius * 2.2f;

            // Square
            clipOutDistanceSquared *= clipOutDistanceSquared;

            var difference = worldPosition.Position - otherEntity.Get<WorldPosition>().Position;

            if (difference.LengthSquared() >= clipOutDistanceSquared)
            {
                collisionManagement.RemoveTemporaryCollisionIgnoreWith(otherEntity);

                World.Remove<CellDivisionCollisionDisabler>(entity);
            }

            if (physics.Body != null)
            {
                physicalWorld.GiveImpulse(physics.Body, difference * 300.0f * collisionDisabler.SeparationForce, true);
            }
        }
        else
        {
            collisionManagement.RemoveTemporaryCollisionIgnoreWith(otherEntity);

            World.Remove<CellDivisionCollisionDisabler>(entity);
        }
    }
}
