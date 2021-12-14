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

            var compoundUseScore = EnergyGenerationScore(microbeSpecies, compound);

            var energyCost = simulationCache
                .GetEnergyBalanceForSpecies(microbeSpecies, patch)
                .TotalConsumptionStationary;

            return compoundUseScore / energyCost;
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
