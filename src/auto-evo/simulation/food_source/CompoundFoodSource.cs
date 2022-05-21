namespace AutoEvo
{
    using System;

    public class CompoundFoodSource : FoodSource
    {
        private readonly Patch patch;
        private readonly Compound compound;
        private readonly float totalCompound;

        public CompoundFoodSource(Patch patch, Compound compound)
        {
            this.patch = patch;
            this.compound = compound;
            if (patch.Biome.Compounds.TryGetValue(compound, out var compoundData))
            {
                totalCompound = compoundData.Density * compoundData.Amount;
            }
            else
            {
                totalCompound = 0.0f;
            }
        }

        public override float FitnessScore(Species species, SimulationCache simulationCache)
        {
            var microbeSpecies = (MicrobeSpecies)species;

            float compoundFindScore = 0.0f;
            if (patch.Biome.Compounds.TryGetValue(compound, out var compoundData))
            {
                if (microbeSpecies.ComputeChemoreceptedCompounds().Contains(compound))
                {
                    compoundFindScore = Constants.AUTO_EVO_CHEMORECEPTOR_FIND_SCORE;
                }
                else
                {
                    // Score if you have to find the compound by chance
                    // Scales with concentration (= density) but never outmatches chemoreceptor.
                    compoundFindScore = Math.Max(compoundData.Density, Constants.AUTO_EVO_CHEMORECEPTOR_FIND_SCORE);
                }
            }

            var compoundUseScore = EnergyGenerationScore(microbeSpecies, compound, patch);

            var energyCost = simulationCache
                .GetEnergyBalanceForSpecies(microbeSpecies, patch)
                .TotalConsumptionStationary;

            return compoundFindScore * compoundUseScore / energyCost;
        }

        public override IFormattable GetDescription()
        {
            // TODO: somehow allow the compound name to translate properly. Maybe we need to use bbcode to refer to the
            // compounds?
            return new LocalizedString("COMPOUND_FOOD_SOURCE", compound.Name);
        }

        public override float TotalEnergyAvailable()
        {
            return totalCompound * Constants.AUTO_EVO_COMPOUND_ENERGY_AMOUNT;
        }
    }
}
