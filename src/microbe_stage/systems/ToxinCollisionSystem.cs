namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles detected toxin collisions with microbes
    /// </summary>
    [With(typeof(ToxinDamageSource))]
    [With(typeof(CollisionManagement))]
    [With(typeof(Physics))]
    [With(typeof(TimedLife))]
    [ReadsComponent(typeof(MicrobeSpeciesMember))]
    [ReadsComponent(typeof(SpeciesMember))]
    [ReadsComponent(typeof(Health))]
    [ReadsComponent(typeof(CellProperties))]
    [ReadsComponent(typeof(MicrobeColony))]
    [ReadsComponent(typeof(MicrobePhysicsExtraData))]
    [RunsAfter(typeof(PhysicsCollisionManagementSystem))]
    [RuntimeCost(0.5f, false)]
    public sealed class ToxinCollisionSystem : AEntitySetSystem<float>
    {
        public ToxinCollisionSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var damageSource = ref entity.Get<ToxinDamageSource>();

            // Quickly detect already hit projectiles
            if (damageSource.ProjectileUsed)
                return;

            ref var collisions = ref entity.Get<CollisionManagement>();

            if (!damageSource.ProjectileInitialized)
            {
                damageSource.ProjectileInitialized = true;

                // Need to setup callbacks etc. for this to work

                // TODO: make sure this system runs before the collision management to make sure no double data apply
                // happens

                collisions.CollisionFilter = FilterCollisions;

                collisions.StartCollisionRecording(Constants.MAX_SIMULTANEOUS_COLLISIONS_TINY);

                collisions.StateApplied = false;
            }

            // Check for active collisions that count as a hit and use up this projectile
            var count = collisions.GetActiveCollisions(out var activeCollisions);
            for (int i = 0; i < count; ++i)
            {
                ref var collision = ref activeCollisions![i];

                if (!HandlePotentiallyDamagingCollision(ref collision))
                    continue;

                // Applied a damaging hit, destroy this toxin
                // TODO: We should probably get some *POP* effect here.

                // Expire right now
                ref var timedLife = ref entity.Get<TimedLife>();
                timedLife.TimeToLiveRemaining = -1;

                ref var physics = ref entity.Get<Physics>();

                // TODO: should this instead of disabling the further collisions be removed from the world immediately
                // to cause less of a physics impact?
                // physics.BodyDisabled = true;
                physics.DisableCollisionState = Physics.CollisionState.DisableCollisions;

                // And make sure the flag we check for is set immediately to not process this projectile again
                // (this is just extra safety against the time over callback configuration not working correctly)
                damageSource.ProjectileUsed = true;

                // Only deal damage at most to a single thing
                break;
            }
        }

        /// <summary>
        ///   Collision filter to disable collisions with microbes the toxin can't damage
        /// </summary>
        /// <returns>False when should pass through</returns>
        private static bool FilterCollisions(ref PhysicsCollision collision)
        {
            try
            {
                // TODO: maybe this could cache something for slight speed up? (though the cache would need clearing
                // periodically)

                // Toxin is always the first entity as it is what registers this collision callback
                if (!collision.SecondEntity.Has<MicrobeSpeciesMember>())
                {
                    // Hit something other than a microbe
                    return true;
                }

                ref var speciesComponent = ref collision.SecondEntity.Get<MicrobeSpeciesMember>();

                try
                {
                    ref var damageSource = ref collision.FirstEntity.Get<ToxinDamageSource>();

                    // Don't hit microbes of the same species as the toxin shooter
                    if (speciesComponent.Species.ID == damageSource.ToxinProperties.Species.ID)
                        return false;
                }
                catch (Exception e)
                {
                    GD.PrintErr($"Entity that collided as a toxin is missing {nameof(ToxinDamageSource)} component: ",
                        e);
                }
            }
            catch (Exception e)
            {
                // Catch any exceptions to not let them escape up to the native code calling side which would blow up
                // everything
                GD.PrintErr("Unexpected error in collision filter: ", e);
            }

            // No reason why this shouldn't collie
            return true;
        }

        private static bool HandlePotentiallyDamagingCollision(ref PhysicsCollision collision)
        {
            // TODO: switch this to also take ref once we use .NET 5 or newer:
            // https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.collectionsmarshal.asspan

            // TODO: see the TODOs about combining code with FilterCollisions

            var damageTarget = collision.SecondEntity;

            // TODO: there is a pretty rare bug where the collision data has random entities in it

            // Skip if hit something that's not a microbe (we don't know how to damage other things currently)
            if (!damageTarget.Has<SpeciesMember>())
                return false;

            ref var speciesComponent = ref damageTarget.Get<SpeciesMember>();

            try
            {
                ref var damageSource = ref collision.FirstEntity.Get<ToxinDamageSource>();

                // Disallow friendly fire
                if (speciesComponent.Species.ID == damageSource.ToxinProperties.Species.ID)
                    return false;

                // Pilus and colony handling requires extra data
                if (damageTarget.Has<MicrobePhysicsExtraData>())
                {
                    ref var extraData = ref damageTarget.Get<MicrobePhysicsExtraData>();

                    // Skip damage if hit a pilus
                    if (extraData.IsSubShapePilus(collision.SecondSubShapeData))
                        return false;

                    if (damageTarget.Has<MicrobeColony>())
                    {
                        // Hit a microbe colony, forward the damage to the exact colony member that was hit
                        if (damageTarget.Get<MicrobeColony>().GetMicrobeFromSubShape(ref extraData,
                                collision.SecondSubShapeData, out var hitEntity))
                        {
                            damageTarget = hitEntity;
                        }
                    }
                }

                ref var health = ref damageTarget.Get<Health>();

                if (health.Invulnerable)
                {
                    // Consume this even though this won't deal damage
                    return true;
                }

                if (damageTarget.Has<CellProperties>())
                {
                    damageSource.ToxinProperties.DealDamage(ref health, ref damageTarget.Get<CellProperties>(),
                        damageSource.ToxinAmount);
                }
                else
                {
                    damageSource.ToxinProperties.DealDamage(ref health, damageSource.ToxinAmount);
                }

                return true;
            }
            catch (Exception e)
            {
                GD.PrintErr("Error processing toxin collision: ", e);

                // Destroy this toxin to avoid recurring error printing spam
                return true;
            }
        }
    }
}
