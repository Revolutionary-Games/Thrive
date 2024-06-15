namespace AutoEvo;

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
        totalEnvironmentalEnergySource =
            patch.Biome.AverageCompounds[this.compound].Ambient * foodCapacityMultiplier;
    }

    public override float FitnessScore(Species species, SimulationCache simulationCache,
        WorldGenerationSettings worldSettings)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var energyCreationScore = CompoundUseScore(microbeSpecies, compound, patch,
            simulationCache, worldSettings);

        var energyCost = simulationCache
            .GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome)
            .TotalConsumptionStationary;

        return energyCreationScore / energyCost;
    }

    public override IFormattable GetDescription()
    {
        // TODO: somehow allow the compound name to translate properly. We now have custom BBCode to refer to
        // compounds so this should be doable
        return new LocalizedString("DISSOLVED_COMPOUND_FOOD_SOURCE", compound.Name);
    }

    public override float TotalEnergyAvailable()
    {
        return totalEnvironmentalEnergySource;
    }

    protected override float StorageScore(MicrobeSpecies species, Compound compound, Patch patch,
        SimulationCache simulationCache, WorldGenerationSettings worldSettings)
    {
        if (worldSettings.DayNightCycleEnabled &&
            simulationCache.GetUsesVaryingCompoundsForSpecies(species, patch.Biome))
        {
            // Penalize species that cannot store enough compounds to survive the night
            return simulationCache.GetStorageAndDayGenerationScore(species, patch.Biome, compound);
        }

        return 1.0f;
    }
}
