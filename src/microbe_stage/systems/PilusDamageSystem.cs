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
    [With(typeof(Health))]
    public sealed class PilusDamageSystem : AEntitySetSystem<float>
    {
        public PilusDamageSystem(World world, IParallelRunner parallelRunner) :
            base(world, parallelRunner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var health = ref entity.Get<Health>();

            // TODO: implement this

            // TODO: as this will be done differently ensure game balance still works
            // // Give immunity to prevent massive damage at some angles
            // // https://github.com/Revolutionary-Games/Thrive/issues/3267
            // MakeInvulnerable(Constants.PILUS_INVULNERABLE_TIME);
        }
    }
}
