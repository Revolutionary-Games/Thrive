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
///   Handles turning off collisions between dividing cells, so they can smoothly overlap before eventually moving
///   away from each other.
/// </summary>
[RuntimeCost(1)]
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(WorldPosition))]
[ReadsComponent(typeof(CellDivisionCollisionDisabler))]
[ReadsComponent(typeof(MicrobeColony))]
[ReadsComponent(typeof(AttachedToEntity))]
[ReadsComponent(typeof(OrganelleContainer))]
[WritesToComponent(typeof(CollisionManagement))]
[RunsBefore(typeof(PhysicsCollisionManagementSystem))]
public partial class MicrobeDivisionClippingSystem : BaseSystem<World, float>
{
    private readonly IWorldSimulation worldSimulation;

    public MicrobeDivisionClippingSystem(IWorldSimulation worldSimulation, World world) : base(world)
    {
        this.worldSimulation = worldSimulation;
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref CellDivisionCollisionDisabler collisionDisabler,
        ref CollisionManagement collisionManagement, ref CellProperties cellProperties,
        ref WorldPosition worldPosition, ref Physics physics, ref OrganelleContainer organelleContainer,
        in Entity entity)
    {
        ref var otherEntity = ref collisionDisabler.IgnoredCollisionWith;

        collisionDisabler.SeparationForce += delta * 1.7f;

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

            // 2.2 = 2 (radii) * 1.1
            var clipOutDistanceSquared = cellProperties.Radius * 2.2f;

            // A partial fix for multicellular division
            if (otherEntity.Has<MicrobeColony>())
            {
                ref var colony = ref otherEntity.Get<MicrobeColony>();

                clipOutDistanceSquared += colony.GetApproximateColonyRadius();
            }

            if (entity.Has<MicrobeColony>())
            {
                ref var colony = ref entity.Get<MicrobeColony>();

                clipOutDistanceSquared += colony.GetApproximateColonyRadius();
            }

            // Square
            var clipOutDistance = clipOutDistanceSquared;
            clipOutDistanceSquared *= clipOutDistanceSquared;

            var difference = worldPosition.Position - otherEntity.Get<WorldPosition>().Position;

            if (difference.LengthSquared() >= clipOutDistanceSquared)
            {
                collisionManagement.RemoveTemporaryCollisionIgnoreWith(otherEntity);

                RemoveDivisionComponentFromEntity(entity);
            }

            // Very important to not apply force if the body is disabled
            if (physics.Body != null && !physics.BodyDisabled)
            {
                // Ensure the difference is not 0, which would break the animation
                // Note that this rarely hits, so this doesn't help in increasing the initial speed
                if (difference.IsZeroApprox())
                    difference += Vector3.Left * 0.01f;

                // Cap the difference here to set a speed limit for the animation rather than infinitely be able to
                // increase the speed
                difference = difference.Clamp(clipOutDistance * -0.2f, clipOutDistance * 0.2f);

                // Make bigger cells get more force to ensure the animation keeps playing fast
                var sizeMultiplier = Math.Clamp((organelleContainer.HexCount - 3) * 0.9f, 1, 100);

                // NOTE: the force gets bigger the distance is!
                physics.QueuedImpulse += difference * 300.0f * collisionDisabler.SeparationForce * sizeMultiplier;
                physics.QueuedForceApplied = false;
            }
        }
        else
        {
            collisionManagement.RemoveTemporaryCollisionIgnoreWith(otherEntity);

            RemoveDivisionComponentFromEntity(entity);
        }
    }

    private void RemoveDivisionComponentFromEntity(Entity entity)
    {
        var recorder = worldSimulation.StartRecordingEntityCommands();
        recorder.Remove<CellDivisionCollisionDisabler>(entity);
        worldSimulation.FinishRecordingEntityCommands(recorder);
    }
}
