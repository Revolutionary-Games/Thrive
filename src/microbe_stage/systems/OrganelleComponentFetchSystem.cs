namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Fills out the component vectors like <see cref="OrganelleContainer.SlimeJets"/>
    /// </summary>
    [With(typeof(OrganelleContainer))]
    [RunsAfter(typeof(MicrobeReproductionSystem))]
    [RunsBefore(typeof(MicrobeMovementSystem))]
    [RunsBefore(typeof(OrganelleTickSystem))]
    public sealed class OrganelleComponentFetchSystem : AEntitySetSystem<float>
    {
        public OrganelleComponentFetchSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var container = ref entity.Get<OrganelleContainer>();

            if (container.OrganelleComponentsCached)
                return;

            container.FetchLayoutOrganelleComponents();
        }
    }
}
