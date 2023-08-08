namespace Systems
{
    using System;
    using System.Threading;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Handles <see cref="DamageOnTouch"/> component setup and dealing the damage
    /// </summary>
    [With(typeof(DamageOnTouch))]
    [With(typeof(CollisionManagement))]
    public sealed class DamageOnTouchSystem : AEntitySetSystem<float>
    {
        private readonly WorldSimulation worldSimulation;

        public DamageOnTouchSystem(WorldSimulation worldSimulation, World world, IParallelRunner runner) : base(world,
            runner)
        {
            this.worldSimulation = worldSimulation;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var damageTouch = ref entity.Get<DamageOnTouch>();

            if (damageTouch.StartedDestroy)
                return;

            ref var collisionManagement = ref entity.Get<CollisionManagement>();

            // Entity setup
            if (!damageTouch.RegisteredWithCollisions)
            {
                if (collisionManagement.RecordActiveCollisions < Constants.MAX_SIMULTANEOUS_DAMAGE_COLLISIONS)
                {
                    Interlocked.Add(ref collisionManagement.RecordActiveCollisions,
                        Constants.MAX_SIMULTANEOUS_DAMAGE_COLLISIONS);

                    collisionManagement.StateApplied = false;
                }

                damageTouch.RegisteredWithCollisions = true;
            }

            // Handle any current collisions
            var collisions = collisionManagement.ActiveCollisions;
            if (collisions is { Length: > 0 })
            {
                bool collided = false;

                int count = collisions.Length;
                for (int i = 0; i < count; ++i)
                {
                    ref var collision = ref collisions[i];

                    if (!collision.Active)
                        continue;

                    bool reverseOrder = collision.FirstEntity != entity;

                    if (reverseOrder)
                    {
                        // Skip collisions with things that can't be damaged
                        if (!collision.FirstEntity.Has<Health>())
                            continue;

                        ref var health = ref collision.FirstEntity.Get<Health>();

                        // TODO: disable dealing damage to a pilus
                        throw new NotImplementedException();

                        DealDamage(ref health, ref damageTouch, delta);
                        collided = true;
                    }
                    else
                    {
                        // This is just the above true conditions of the if flipped to deal with the other body
                        if (!collision.SecondEntity.Has<Health>())
                            continue;

                        ref var health = ref collision.SecondEntity.Get<Health>();

                        // TODO: pilus

                        DealDamage(ref health, ref damageTouch, delta);
                        collided = true;
                    }
                }

                if (collided && damageTouch.DestroyOnTouch)
                {
                    // Destroy this entity
                    damageTouch.StartedDestroy = true;

                    collisionManagement.AllCollisionsDisabled = true;
                    collisionManagement.StateApplied = false;

                    if (damageTouch.UsesMicrobialDissolveEffect)
                    {
                        // We assume that damage on touch is always done by chunks
                        entity.StartDissolveAnimation(true);
                    }
                    else
                    {
                        worldSimulation.DestroyEntity(entity);
                    }
                }
            }
        }

        private void DealDamage(ref Health health, ref DamageOnTouch damageTouch, float delta)
        {
            if (damageTouch.DestroyOnTouch)
            {
                health.DealDamage(damageTouch.DamageAmount, damageTouch.DamageType);
            }
            else
            {
                health.DealDamage(damageTouch.DamageAmount * delta, damageTouch.DamageType);
            }
        }
    }
}
