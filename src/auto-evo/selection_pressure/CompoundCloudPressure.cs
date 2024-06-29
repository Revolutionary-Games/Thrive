namespace AutoEvo;

using System;

public class CompoundCloudPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("COMPOUND_CLOUD_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly float totalEnergy;
    private readonly Compound compound;
    private readonly bool isDayNightCycleEnabled;
    private readonly Patch patch;

    public CompoundCloudPressure(Patch patch, float weight, Compound compound, bool isDayNightCycleEnabled) :
        base(weight, [
            new ChangeMembraneRigidity(true),
            new ChangeMembraneType(SimulationParameters.Instance.GetMembrane("single")),
        ])
    {
        if (!compound.IsCloud)
            throw new ArgumentException("Given compound to cloud pressure is not of cloud type");

        this.compound = compound;
        this.isDayNightCycleEnabled = isDayNightCycleEnabled;
        this.patch = patch;

        if (patch.Biome.AverageCompounds.TryGetValue(compound, out var compoundData))
        {
            totalEnergy = compoundData.Density * compoundData.Amount * Constants.AUTO_EVO_COMPOUND_ENERGY_AMOUNT;
        }
        else
        {
            totalEnergy = 0.0f;
        }
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        var score = cache.GetBaseSpeedForSpecies(species);

        // Species that are less active during the night get a small penalty here based on their activity
        if (isDayNightCycleEnabled && cache.GetUsesVaryingCompoundsForSpecies(species, patch.Biome))
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

    public override float GetEnergy()
    {
        return totalEnergy;
    }

    public override IFormattable GetDescription()
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
