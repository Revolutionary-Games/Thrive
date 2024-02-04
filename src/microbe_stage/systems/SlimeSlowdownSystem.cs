namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Handles slowing down cells that are currently moving through slime (and don't have slime jets themselves)
    /// </summary>
    [With(typeof(MicrobeControl))]
    [With(typeof(OrganelleContainer))]
    [With(typeof(WorldPosition))]
    [Without(typeof(AttachedToEntity))]
    [ReadsComponent(typeof(WorldPosition))]
    [RunsAfter(typeof(OrganelleComponentFetchSystem))]
    [RunsBefore(typeof(MicrobeMovementSystem))]
    public sealed class SlimeSlowdownSystem : AEntitySetSystem<float>
    {
        private readonly IReadonlyCompoundClouds compoundCloudSystem;

        private readonly Compound mucilage;

        public SlimeSlowdownSystem(IReadonlyCompoundClouds compoundCloudSystem, World world, IParallelRunner runner) :
            base(world, runner)
        {
            this.compoundCloudSystem = compoundCloudSystem;

            mucilage = SimulationParameters.Instance.GetCompound("mucilage");
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var control = ref entity.Get<MicrobeControl>();

            ref var organelles = ref entity.Get<OrganelleContainer>();

            // Cells with jets aren't affected by mucilage
            if (organelles.SlimeJets is { Count: > 0 })
            {
                control.SlowedBySlime = false;
                return;
            }

            ref var position = ref entity.Get<WorldPosition>();

            control.SlowedBySlime = compoundCloudSystem.AmountAvailable(mucilage, position.Position, 1.0f) >
                Constants.COMPOUND_DENSITY_CATEGORY_FAIR_AMOUNT;
        }
    }
}
