namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Evenly distributes compounds (except ones that can't be shared between cells like ATP) between cells in a
    ///   colony
    /// </summary>
    [With(typeof(MicrobeColony))]
    [Without(typeof(AttachedToEntity))]
    public sealed class ColonyCompoundDistributionSystem : AEntitySetSystem<float>
    {
        public ColonyCompoundDistributionSystem(World world, IParallelRunner parallelRunner) : base(world,
            parallelRunner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var colony = ref entity.Get<MicrobeColony>();

            colony.GetCompounds().DistributeCompoundSurplus();
        }
    }
}
