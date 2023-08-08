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
    [With(typeof(TimedLife))]
    public sealed class ToxinCollisionSystem : AEntitySetSystem<float>
    {
        public ToxinCollisionSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var collisions = ref entity.Get<CollisionManagement>();

            // Quickly detect already hit projectiles
            if (collisions.AllCollisionsDisabled)
                return;

            ref var damageSource = ref entity.Get<ToxinDamageSource>();
            if (!damageSource.ProjectileInitialized)
            {
                damageSource.ProjectileInitialized = true;

                // Need to setup callbacks etc. for this to work

                // TODO: make sure this system runs before the collision management to make sure no double data apply
                // happens

                collisions.CollisionFilter = FilterCollisions;
                collisions.RecordActiveCollisions = 4;
                collisions.StateApplied = false;
            }

            var activeCollisions = collisions.ActiveCollisions;
            if (activeCollisions == null || activeCollisions.Length < 1)
                return;

            // Check for active collisions that count as a hit and use up this projectile
            int count = activeCollisions.Length;
            for (int i = 0; i < count; ++i)
            {
                ref var collision = ref activeCollisions[i];

                if (!collision.Active)
                    continue;

                if (!HandlePotentiallyDamagingCollision(ref collision))
                    continue;

                // Applied a damaging hit, destroy this toxin
                // TODO: We should probably get some *POP* effect here.

                // Expire right now
                ref var timedLife = ref entity.Get<TimedLife>();
                timedLife.TimeToLiveRemaining = -1;

                // And make sure the flag we check for is set immediately to not process this projectile again
                // (this is just extra safety against the time over callback configuration not working correctly)
                collisions.AllCollisionsDisabled = true;
                collisions.StateApplied = false;
            }
        }

        /// <summary>
        ///   Collision filter to disable collisions with microbes the toxin can't damage
        /// </summary>
        /// <returns>False when should pass through</returns>
        private static bool FilterCollisions(ref PhysicsCollision collision)
        {
            // TODO: maybe this could cache something for slight speed up? (though the cache would need clearing
            // periodically)

            // TODO: consider if dumping extra data like 64 bytes in the physics body would be enough to make this
            // check not need to look up multiple entities
            var toxin = collision.FirstEntity;
            var damageTarget = collision.SecondEntity;

            // Detect which is the toxin based on what has the damage component
            if (!collision.FirstEntity.Has<ToxinDamageSource>())
            {
                toxin = collision.SecondEntity;
                damageTarget = collision.FirstEntity;
            }

            if (!damageTarget.Has<SpeciesMember>())
            {
                // Hit something other than a microbe
                return true;
            }

            ref var speciesComponent = ref damageTarget.Get<SpeciesMember>();

            try
            {
                ref var damageSource = ref toxin.Get<ToxinDamageSource>();

                // Don't hit microbes of the same species as the toxin shooter
                if (speciesComponent.Species == damageSource.ToxinProperties.Species)
                    return false;
            }
            catch (Exception e)
            {
                GD.PrintErr($"Entity that collided as a toxin is missing {nameof(ToxinDamageSource)} component: ", e);
            }

            // No reason why this shouldn't collie
            return true;
        }

        private static bool HandlePotentiallyDamagingCollision(ref PhysicsCollision collision)
        {
            // TODO: switch this to also take ref once we use .NET 5 or newer:
            // https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.collectionsmarshal.asspan

            // TODO: see the TODOs about combining code with FilterCollisions

            var toxin = collision.FirstEntity;
            var damageTarget = collision.SecondEntity;

            if (!collision.FirstEntity.Has<ToxinDamageSource>())
            {
                toxin = collision.SecondEntity;
                damageTarget = collision.FirstEntity;
            }

            // Skip if hit something that's not a microbe (we don't know how to damage other things currently)
            if (!damageTarget.Has<SpeciesMember>())
                return false;

            ref var speciesComponent = ref damageTarget.Get<SpeciesMember>();

            try
            {
                ref var damageSource = ref toxin.Get<ToxinDamageSource>();

                // Disallow friendly fire
                if (speciesComponent.Species == damageSource.ToxinProperties.Species)
                    return false;

                ref var health = ref damageTarget.Get<Health>();

                if (health.Invulnerable)
                {
                    // Consume this even though this won't deal damage
                    return true;
                }

                if (damageTarget.Has<MicrobeColony>())
                {
                    // Hit a microbe colony, forward the damage to the exact colony member that was hit
                    // TODO: forward damage to specific microbe
                    throw new NotImplementedException();
                }

                damageSource.ToxinProperties.DealDamage(ref health, damageSource.ToxinAmount);
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
