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
    /// <remarks>
    ///   <para>
    ///     This runs before the engulfing system to allow newly spawned multicellular members to stay in engulf mode
    ///     when growing as otherwise the cell would have no compounds and get immediately kicked out of engulf mode
    ///     due to missing ATP.
    ///   </para>
    /// </remarks>
    [With(typeof(MicrobeColony))]
    [Without(typeof(AttachedToEntity))]
    [WritesToComponent(typeof(CompoundStorage))]
    [RunsBefore(typeof(EngulfingSystem))]
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
