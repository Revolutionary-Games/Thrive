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
    [With(typeof(Species))]
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

                bool otherIsPilus = collision.SecondEntity.Get<MicrobePhysicsExtraData>()
                    .IsSubShapePilus(collision.SecondSubShapeData);
                bool oursIsPilus = ourExtraData.IsSubShapePilus(collision.FirstSubShapeData);

                // Pilus logic
                if (otherIsPilus && oursIsPilus)
                {
                    // Pilus on pilus doesn't deal damage
                    continue;
                }

                if (otherIsPilus || oursIsPilus)
                {
                    // Us attacking the other microbe, or it is attacking us

                    // Disallow cannibalism
                    if (ourSpecies.ID == collision.SecondEntity.Get<SpeciesMember>().ID)
                        return;

                    ref var targetHealth = ref collision.SecondEntity.Get<Health>();

                    // TODO: readjust the pilus damage now that this takes penetration depth into account
                    float damage = Constants.PILUS_BASE_DAMAGE * collision.PenetrationAmount;

                    // TODO: as this will be done differently ensure game balance still works
                    // // Give immunity to prevent massive damage at some angles
                    // // https://github.com/Revolutionary-Games/Thrive/issues/3267
                    // MakeInvulnerable(Constants.PILUS_INVULNERABLE_TIME);

                    // Skip too small damage
                    if (damage < 0.0001f)
                        continue;

                    targetHealth.DealMicrobeDamage(ref collision.SecondEntity.Get<CellProperties>(), damage, "pilus");
                }
            }
        }
    }
}
