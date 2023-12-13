namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Handles applying pilus damage to microbes
    /// </summary>
    [With(typeof(OrganelleContainer))]
    [With(typeof(CollisionManagement))]
    [With(typeof(MicrobePhysicsExtraData))]
    [With(typeof(SpeciesMember))]
    [ReadsComponent(typeof(CellProperties))]
    public sealed class PilusDamageSystem : AEntitySetSystem<float>
    {
        public PilusDamageSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var collisionManagement = ref entity.Get<CollisionManagement>();

            var count = collisionManagement.GetActiveCollisions(out var collisions);
            if (count < 1)
                return;

            ref var ourExtraData = ref entity.Get<MicrobePhysicsExtraData>();
            ref var ourSpecies = ref entity.Get<SpeciesMember>();

            for (int i = 0; i < count; ++i)
            {
                ref var collision = ref collisions![i];

                // Only process just started collisions for pilus damage
                if (!collision.JustStarted)
                    continue;

                if (!collision.SecondEntity.Has<MicrobePhysicsExtraData>())
                    continue;

                ref var otherExtraData = ref collision.SecondEntity.Get<MicrobePhysicsExtraData>();

                bool otherIsPilus = otherExtraData.IsSubShapePilus(collision.SecondSubShapeData);
                bool oursIsPilus = ourExtraData.IsSubShapePilus(collision.FirstSubShapeData);

                // Pilus logic
                if (otherIsPilus && oursIsPilus)
                {
                    // Pilus on pilus doesn't deal damage
                    continue;
                }

                if (!oursIsPilus)
                    continue;

                // Us attacking the other microbe. In the case the other entity is attacking us it will be
                // detected by that entity's physics callback

                // Disallow cannibalism
                if (ourSpecies.ID == collision.SecondEntity.Get<SpeciesMember>().ID)
                    return;

                if (collision.SecondEntity.Has<MicrobeColony>())
                {
                    if (collision.SecondEntity.Get<MicrobeColony>().GetMicrobeFromSubShape(ref otherExtraData,
                            collision.SecondSubShapeData, out var hitEntity))
                    {
                        DealPilusDamage(ref ourExtraData, ref collision, hitEntity);
                        continue;
                    }
                }

                DealPilusDamage(ref ourExtraData, ref collision, collision.SecondEntity);
            }
        }

        private void DealPilusDamage(ref MicrobePhysicsExtraData ourExtraData, ref PhysicsCollision collision,
            in Entity targetEntity)
        {
            ref var targetHealth = ref targetEntity.Get<Health>();

            if (ourExtraData.IsSubShapeInjectisomeIfIsPilus(collision.FirstSubShapeData))
            {
                // Injectisome attack, this deals non-physics force based damage, so this uses a cooldown
                ref var cooldown = ref collision.SecondEntity.Get<DamageCooldown>();

                if (cooldown.IsInCooldown())
                    return;

                targetHealth.DealMicrobeDamage(ref collision.SecondEntity.Get<CellProperties>(),
                    Constants.INJECTISOME_BASE_DAMAGE, "injectisome");

                cooldown.StartInjectisomeCooldown();
                return;
            }

            float damage = Constants.PILUS_BASE_DAMAGE * collision.PenetrationAmount;

            // TODO: as this will be done differently ensure game balance still works
            // // Give immunity to prevent massive damage at some angles
            // // https://github.com/Revolutionary-Games/Thrive/issues/3267
            // MakeInvulnerable(Constants.PILUS_INVULNERABLE_TIME);

            // Skip too small damage
            if (damage < 0.01f)
                return;

            if (damage > Constants.PILUS_MAX_DAMAGE)
                damage = Constants.PILUS_MAX_DAMAGE;

            targetHealth.DealMicrobeDamage(ref collision.SecondEntity.Get<CellProperties>(), damage, "pilus");
        }
    }
}
