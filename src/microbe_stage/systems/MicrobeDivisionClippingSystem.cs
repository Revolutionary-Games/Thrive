namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
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
    public MicrobeDivisionClippingSystem(World world) : base(world)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref CellDivisionCollisionDisabler collisionDisabler,
        ref CollisionManagement collisionManagement, ref CellProperties cellProperties,
        ref WorldPosition worldPosition, in Entity entity)
    {
        ref var otherEntity = ref collisionDisabler.IgnoredCollisionWith;

        if (collisionManagement.IgnoredCollisionsWith != null)
        {
            if (!collisionManagement.IgnoredCollisionsWith.Contains(otherEntity))
                return;
        }

        if (otherEntity.Has<WorldPosition>())
        {
            // 2.4 = 2 (radiuses) * 1.1
            var clipOutDistanceSquared = cellProperties.Radius * 2.2f;

            // Square
            clipOutDistanceSquared *= clipOutDistanceSquared;
            if (otherEntity.Get<WorldPosition>().Position.DistanceSquaredTo(
                worldPosition.Position) >= clipOutDistanceSquared)
            {
                if (collisionManagement.RemoveIgnoredCollisions == null)
                    collisionManagement.RemoveIgnoredCollisions = new();

                if (!collisionManagement.RemoveIgnoredCollisions.Contains(otherEntity))
                {
                    collisionManagement.RemoveIgnoredCollisions.Add(otherEntity);
                    collisionManagement.StateApplied = false;
                }
            }
            else
            {
                if (collisionManagement.IgnoredCollisionsWith == null)
                    collisionManagement.IgnoredCollisionsWith = new();

                if (!collisionManagement.IgnoredCollisionsWith.Contains(otherEntity))
                {
                    collisionManagement.IgnoredCollisionsWith.Add(otherEntity);

                    collisionManagement.StateApplied = false;
                }
            }
        }
    }
}
