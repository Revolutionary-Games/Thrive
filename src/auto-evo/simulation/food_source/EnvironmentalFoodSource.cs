namespace AutoEvo
{
    using System;

    public class EnvironmentalFoodSource : FoodSource
    {
        private readonly Compound compound;
        private readonly Patch patch;
        private readonly float totalEnvironmentalEnergySource;

        public EnvironmentalFoodSource(Patch patch, Compound compound, float foodCapacityMultiplier)
        {
            if (compound.IsCloud)
                throw new ArgumentException("Given compound to environmental source is a cloud type");

            this.patch = patch;
            this.compound = compound;
            totalEnvironmentalEnergySource = patch.Biome.Compounds[this.compound].Ambient * foodCapacityMultiplier;
        }

        public override float FitnessScore(Species species, SimulationCache simulationCache)
        {
            var microbeSpecies = (MicrobeSpecies)species;

            var energyCreationScore = CompoundUseScore(microbeSpecies, compound, patch, simulationCache);

            var energyCost = simulationCache
                .GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome)
                .TotalConsumptionStationary;

            return energyCreationScore / energyCost;
        }

        public override IFormattable GetDescription()
        {
            // TODO: somehow allow the compound name to translate properly. Maybe we need to use bbcode to refer to the
            // compounds?
            return new LocalizedString("DISSOLVED_COMPOUND_FOOD_SOURCE", compound.Name);
        }

        public override float TotalEnergyAvailable()
        {
            return totalEnvironmentalEnergySource;
        }

        protected override float StorageScore(MicrobeSpecies species, Compound compound, Patch patch,
             SimulationCache simulationCache)
        {
            if (simulationCache.DayNightCycleEnabled)
            {
                var dayNightCycleConfiguration = SimulationParameters.Instance.GetDayNightCycleConfiguration()!;
                var nightTime = dayNightCycleConfiguration.DaytimePercentage * dayNightCycleConfiguration.HoursPerDay;

                // If a species consumes a lot, it ought to store more.
                // NB: this might artificially penalize overproducers but I'm willing to accept it for now - Maxonovien
                var reserveScore = CompoundUse(species, compound, patch, simulationCache) *
                    simulationCache.GetStorageCapacityForSpecies(species);

                // Severely penalize species not storing enough for their production
                var minimumViableReserveScore = Constants.AUTO_EVO_MINIMUM_VIABLE_RESERVE_PER_TIME_UNIT * nightTime;

                if (reserveScore <= minimumViableReserveScore)
                    return minimumViableReserveScore / Constants.AUTO_EVO_NON_VIABLE_RESERVE_PENALTY;

                // TODO: consider using a cap, e.g two times the viable reserve (i.e. osmoregulation + base movement.
                return reserveScore;
            }

            return 1.0f;
        }

        // TODO CACHE IT, AS FOR ENERGY GENERATION, TO AVOID MULTIPLE LOOPS.
        private float CompoundUse(MicrobeSpecies species, Compound compound, Patch patch,
             SimulationCache simulationCache)
        {
            var compoundUse = 0.0f;

            // We check generation from all the processes of the cell../
            foreach (var organelle in species.Organelles)
            {
                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    // ... that uses the given compound (regardless of usage)
                    if (process.Process.Inputs.TryGetValue(compound, out var inputAmount))
                    {
                        var processEfficiency = simulationCache.GetProcessMaximumSpeed(process, patch.Biome).Efficiency;

                        compoundUse += inputAmount * processEfficiency;
                    }
                }
            }

            return compoundUse;
        }
    }
}
