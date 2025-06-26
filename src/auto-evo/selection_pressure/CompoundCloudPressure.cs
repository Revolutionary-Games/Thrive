namespace AutoEvo;

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Systems;

[JSONDynamicTypeAllowed]
public class CompoundCloudPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_COMPOUND_CLOUD_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    [JsonProperty]
    private readonly Compound compound;

    private readonly CompoundDefinition compoundDefinition;

    [JsonProperty]
    private readonly float amount;

    [JsonProperty]
    private readonly bool isDayNightCycleEnabled;

    public CompoundCloudPressure(Compound compound, float amount, bool isDayNightCycleEnabled, float weight) :
        base(weight, [
            new ChangeMembraneRigidity(true),
            new ChangeMembraneType("single"),
        ])
    {
        compoundDefinition = SimulationParameters.GetCompound(compound);

        if (!compoundDefinition.IsCloud)
            throw new ArgumentException("Given compound to cloud pressure is not of cloud type");

        this.compound = compound;
        this.amount = amount;
        this.isDayNightCycleEnabled = isDayNightCycleEnabled;
    }

    [JsonIgnore]
    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var score = MathF.Pow(cache.GetSpeedForSpecies(microbeSpecies), 0.6f);

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
        return new LocalizedString("COMPOUND_FOOD_SOURCE",
            new LocalizedString(compoundDefinition.GetUntranslatedName()));
    }

    public Compound GetUsedCompoundType()
    {
        return compound;
    }

    public override string ToString()
    {
        return $"{Name} ({compoundDefinition.Name})";
    }
}
