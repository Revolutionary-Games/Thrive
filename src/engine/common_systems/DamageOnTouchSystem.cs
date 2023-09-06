namespace Systems
{
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
                collisionManagement.StartCollisionRecording(Constants.MAX_SIMULTANEOUS_COLLISIONS_SMALL);

                damageTouch.RegisteredWithCollisions = true;
            }

            // Handle any current collisions
            bool collided = false;

            var count = collisionManagement.GetActiveCollisions(out var collisions);
            for (int i = 0; i < count; ++i)
            {
                ref var collision = ref collisions![i];

                // Skip collisions with things that can't be damaged
                if (!collision.SecondEntity.Has<Health>())
                    continue;

                ref var health = ref collision.SecondEntity.Get<Health>();

                if (DealDamage(collision.SecondEntity, ref health, ref damageTouch, delta))
                {
                    collided = true;
                }
            }

            if (collided && damageTouch.DestroyOnTouch)
            {
                // Destroy this entity
                damageTouch.StartedDestroy = true;

                ref var physics = ref entity.Get<Physics>();

                // Disable *further* collisions (any active collisions will stay)
                physics.DisableCollisionState = Physics.CollisionState.DisableCollisions;

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

        private bool DealDamage(in Entity entity, ref Health health, ref DamageOnTouch damageTouch, float delta)
        {
            if (damageTouch.DestroyOnTouch)
            {
                return HandlePotentialMicrobeDamage(ref health, entity, damageTouch.DamageAmount,
                    damageTouch.DamageType);
            }

            return HandlePotentialMicrobeDamage(ref health, entity, damageTouch.DamageAmount * delta,
                damageTouch.DamageType);
        }

        private bool HandlePotentialMicrobeDamage(ref Health health, in Entity entity, float damageValue,
            string damageType)
        {
            if (entity.Has<CellProperties>())
            {
                // TODO: disable dealing damage to a pilus
                // return false

                health.DealMicrobeDamage(ref entity.Get<CellProperties>(), damageValue, damageType);
            }
            else
            {
                health.DealDamage(damageValue, damageType);
            }

            return true;
        }
    }
}
