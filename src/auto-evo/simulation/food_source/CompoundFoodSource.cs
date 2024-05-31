namespace AutoEvo;

using System;

public class CompoundFoodSource : RandomEncounterFoodSource
{
    private readonly Patch patch;
    private readonly Compound compound;
    private readonly float totalCompound;
    private readonly bool isDayNightCycleEnabled;

    public CompoundFoodSource(Patch patch, Compound compound, bool isDayNightCycleEnabled)
    {
        this.patch = patch;
        this.compound = compound;
        this.isDayNightCycleEnabled = isDayNightCycleEnabled;

        if (patch.Biome.AverageCompounds.TryGetValue(compound, out var compoundData))
        {
            // TODO: multiply by storing score if average different from max?
            totalCompound = compoundData.Density * compoundData.Amount;
        }
        else
        {
            totalCompound = 0.0f;
        }
    }

    public override float FitnessScore(Species species, SimulationCache simulationCache,
        WorldGenerationSettings worldSettings)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var compoundUseScore = CompoundUseScore(microbeSpecies, compound, patch,
            simulationCache, worldSettings);

        // Species that are less active during the night get a small penalty here based on their activity
        if (isDayNightCycleEnabled &&
            MicrobeInternalCalculations.UsesDayVaryingCompounds(((MicrobeSpecies)species).Organelles, patch.Biome,
                null))
        {
            var multiplier = species.Behaviour.Activity / Constants.AI_ACTIVITY_TO_BE_FULLY_ACTIVE_DURING_NIGHT;

            // Make the multiplier less extreme
            multiplier *= Constants.AUTO_EVO_NIGHT_SESSILITY_COLLECTING_PENALTY_MULTIPLIER;

            multiplier = Math.Max(multiplier, Constants.AUTO_EVO_MAX_NIGHT_SESSILITY_COLLECTING_PENALTY);

            if (multiplier <= 1)
                compoundUseScore *= multiplier;
        }

        var energyCost = simulationCache
            .GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome)
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
