namespace Systems
{
    using System;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Handles absorbing compounds from <see cref="CompoundCloudSystem"/> into <see cref="CompoundStorage"/>
    /// </summary>
    [With(typeof(CompoundAbsorber))]
    [With(typeof(CompoundStorage))]
    [With(typeof(WorldPosition))]
    public sealed class CompoundAbsorptionSystem : AEntitySetSystem<float>
    {
        private readonly CompoundCloudSystem compoundCloudSystem;

        public CompoundAbsorptionSystem(CompoundCloudSystem compoundCloudSystem, World world, IParallelRunner runner) :
            base(world, runner, Constants.SYSTEM_NORMAL_ENTITIES_PER_THREAD)
        {
            this.compoundCloudSystem = compoundCloudSystem;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var absorber = ref entity.Get<CompoundAbsorber>();

            if (absorber.AbsorbRadius <= 0 || absorber.AbsorbSpeed < 0)
                return;

            if (absorber.AbsorbSpeed != 0)
            {
                // Rate limited absorbing is not implemented
                throw new NotImplementedException();
            }

            ref var storage = ref entity.Get<CompoundStorage>();

            if (absorber.OnlyAbsorbUseful && !storage.Compounds.HasAnyBeenSetUseful())
            {
                // Skip processing until something is set useful
                // TODO: maybe there is a conceivable scenario where only generally useful compounds should be absorbed
                // in which case this check fails even though the generally useful stuff should be absorbed
                return;
            }

            if (!absorber.OnlyAbsorbUseful)
            {
                // The clouds by default check that the bag has a compound set useful before absorbing it, so if this
                // flag is set to false we would need to communicate that to the clouds someway
                throw new NotImplementedException();
            }

            ref var position = ref entity.Get<WorldPosition>();

            compoundCloudSystem.AbsorbCompounds(position.Position, absorber.AbsorbRadius, storage.Compounds,
                absorber.TotalAbsorbedCompounds, delta, absorber.AbsorptionRatio);

            // Player infinite compounds cheat, doesn't *really* belong here but this is probably the best place to put
            // this instead of creating a dedicated cheats handling system
            if (CheatManager.InfiniteCompounds && entity.Has<PlayerMarker>())
            {
                var usefulCompounds =
                    SimulationParameters.Instance.GetCloudCompounds().Where(storage.Compounds.IsUseful);
                foreach (var usefulCompound in usefulCompounds)
                {
                    storage.Compounds.AddCompound(usefulCompound,
                        storage.Compounds.GetFreeSpaceForCompound(usefulCompound));
                }
            }
        }
    }
}
