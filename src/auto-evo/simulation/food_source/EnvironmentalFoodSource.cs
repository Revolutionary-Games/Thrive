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
        if (worldSettings.DayNightCycleEnabled)
        {
            var nightTime = worldSettings.DaytimeFraction * worldSettings.HoursPerDay;

            // If a species consumes a lot, it ought to store more.
            // NOTE: might artificially penalize overproducers but I'm willing to accept it for now - Maxonovien
            var reserveScore = simulationCache.GetCompoundUseScoreForSpecies(species, patch.Biome, compound) *
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
}
