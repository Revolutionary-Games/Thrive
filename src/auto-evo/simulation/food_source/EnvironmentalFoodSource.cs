using System;
using AutoEvo;

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
        totalEnvironmentalEnergySource = patch.Biome.Compounds[this.compound].Dissolved * foodCapacityMultiplier;
    }

    public override float FitnessScore(Species species, SimulationCache simulationCache)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var energyCreationScore = EnergyGenerationScore(microbeSpecies, compound);

        var energyCost = simulationCache.GetEnergyBalanceForSpecies(microbeSpecies, patch).FinalBalanceStationary;

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
}
