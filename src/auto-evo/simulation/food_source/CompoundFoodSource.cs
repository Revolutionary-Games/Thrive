namespace AutoEvo;

using System;

public class CompoundFoodSource : RandomEncounterFoodSource
{
    private readonly Patch patch;
    private readonly Compound compound;
    private readonly float totalCompound;

    public CompoundFoodSource(Patch patch, Compound compound)
    {
        this.patch = patch;
        this.compound = compound;
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
