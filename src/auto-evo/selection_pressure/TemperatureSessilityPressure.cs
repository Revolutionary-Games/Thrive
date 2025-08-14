namespace AutoEvo;

using Newtonsoft.Json;

/// <summary>
///   Selection pressure that penalizes sessile (non-motile) species in high-temperature environments.
///   This pressure encourages species to become more mobile in hot conditions.
/// </summary>
[JSONDynamicTypeAllowed]
public class TemperatureSessilityPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_TEMPERATURE_SESSILITY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public TemperatureSessilityPressure(float weight) : base(weight, [
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Activity, 150.0f),
        new UpgradeOrganelle(organelle => organelle.HasMovementComponent, new FlagellumUpgrades(1.0f)),
    ])
    {
    }

    [JsonIgnore]
    public override LocalizedString Name => NameString;

    /// <summary>
    ///   Calculates the selection pressure score based on temperature and species activity.
    ///   Higher scores are given to more active (less sessile) species in high-temperature environments.
    /// </summary>
    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        // Get temperature from the patch
        if (!patch.Biome.TryGetCompound(Compound.Temperature, CompoundAmountType.Biome, out var temperatureAmount))
            return 0.0f;

        float temperature = temperatureAmount.Ambient;

        // If the temperature is not high enough, no bonus
        if (temperature <= 60.0f)
            return 0.0f;

        // Calculate score based on activity (higher activity = higher score for mobile behavior)
        // The more active the species, the higher the score
        float activityScore = microbeSpecies.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY;
        
        return activityScore;
    }

    /// <summary>
    ///   This pressure doesn't provide energy to species.
    /// </summary>
    /// <param name="patch">The patch to evaluate</param>
    /// <returns>Always returns 0 as this pressure doesn't provide energy</returns>
    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
