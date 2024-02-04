namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles <see cref="DamageOnTouch"/> component setup and dealing the damage
    /// </summary>
    [With(typeof(DamageOnTouch))]
    [With(typeof(CollisionManagement))]
    [RunsAfter(typeof(PhysicsCollisionManagementSystem))]
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

                if (DealDamage(collision.SecondEntity, ref health, ref damageTouch, delta,
                        collision.SecondSubShapeData))
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
                physics.SetCollisionDisableState(true);

                if (damageTouch.UsesMicrobialDissolveEffect)
                {
                    // We assume that damage on touch is always done by chunks
                    entity.StartDissolveAnimation(worldSimulation, true, true);
                }
                else
                {
                    worldSimulation.DestroyEntity(entity);
                }
            }
        }

        private bool DealDamage(in Entity entity, ref Health health, ref DamageOnTouch damageTouch, float delta,
            uint subShape)
        {
            if (damageTouch.DestroyOnTouch)
            {
                return HandlePotentialMicrobeDamage(ref health, entity, damageTouch.DamageAmount,
                    damageTouch.DamageType, subShape);
            }

            return HandlePotentialMicrobeDamage(ref health, entity, damageTouch.DamageAmount * delta,
                damageTouch.DamageType, subShape);
        }

        private bool HandlePotentialMicrobeDamage(ref Health health, in Entity entity, float damageValue,
            string damageType, uint subShape)
        {
            if (entity.Has<CellProperties>())
            {
                if (!entity.Has<MicrobePhysicsExtraData>())
                {
                    GD.PrintErr("Microbe missing physics extra data when checking with damage on touch entity");
                    return true;
                }

                ref var entityExtraData = ref entity.Get<MicrobePhysicsExtraData>();

                // Can't damage through a pilus
                if (entityExtraData.IsSubShapePilus(subShape))
                    return false;

                if (entity.Has<MicrobeColony>())
                {
                    if (entity.Get<MicrobeColony>().GetMicrobeFromSubShape(ref entityExtraData,
                            subShape, out var hitEntity))
                    {
                        hitEntity.Get<Health>()
                            .DealMicrobeDamage(ref hitEntity.Get<CellProperties>(), damageValue, damageType);

                        return true;
                    }
                }

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
