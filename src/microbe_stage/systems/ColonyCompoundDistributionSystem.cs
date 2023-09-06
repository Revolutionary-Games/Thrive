namespace Systems
{
    using System;
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

        protected override void Update(float state, in Entity entity)
        {
            throw new NotImplementedException();
        }
    }
}
