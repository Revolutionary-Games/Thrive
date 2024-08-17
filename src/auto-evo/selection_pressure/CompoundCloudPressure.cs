namespace AutoEvo;

using System;

public class CompoundCloudPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_COMPOUND_CLOUD_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly Compound compound;
    private readonly bool isDayNightCycleEnabled;

    public CompoundCloudPressure(Compound compound, bool isDayNightCycleEnabled, float weight) :
        base(weight, [
            new ChangeMembraneRigidity(true),
            new ChangeMembraneType("single"),
        ])
    {
        if (!compound.IsCloud)
            throw new ArgumentException("Given compound to cloud pressure is not of cloud type");

        this.compound = compound;
        this.isDayNightCycleEnabled = isDayNightCycleEnabled;
    }

    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var score = cache.GetSpeedForSpecies(microbeSpecies);

        // Species that are less active during the night get a small penalty here based on their activity
        if (isDayNightCycleEnabled && cache.GetUsesVaryingCompoundsForSpecies(microbeSpecies, patch.Biome))
        {
            var multiplier = species.Behaviour.Activity / Constants.AI_ACTIVITY_TO_BE_FULLY_ACTIVE_DURING_NIGHT;

            // Make the multiplier less extreme
            multiplier *= Constants.AUTO_EVO_NIGHT_SESSILITY_COLLECTING_PENALTY_MULTIPLIER;

            multiplier = Math.Max(multiplier, Constants.AUTO_EVO_MAX_NIGHT_SESSILITY_COLLECTING_PENALTY);

            if (multiplier <= 1)
                score *= multiplier;
        }

        return score;
    }

    public override float GetEnergy(Patch patch)
    {
        if (patch.Biome.AverageCompounds.TryGetValue(compound, out var compoundData))
        {
            return compoundData.Density * compoundData.Amount * Constants.AUTO_EVO_COMPOUND_ENERGY_AMOUNT;
        }

        return 0.0f;
    }

    public override LocalizedString GetDescription()
    {
        // TODO: somehow allow the compound name to translate properly. Maybe we need to use bbcode to refer to the
        // compounds?
        return new LocalizedString("COMPOUND_FOOD_SOURCE", compound.Name);
    }

    public override string ToString()
    {
        return $"{Name} ({compound.Name})";
    }
}
