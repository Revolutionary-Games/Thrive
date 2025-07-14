namespace AutoEvo;

using Newtonsoft.Json;

/// <summary>
///   Selection pressure that favours sessile (non-motile) species in high temperature environments.
///   This pressure encourages species to become less mobile in hot conditions.
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
    ///   Calculates the selection pressure score based on temperature and species speed.
    ///   Higher scores are given to slower species in high temperature environments.
    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        // Get temperature from the patch
        if (!patch.Biome.TryGetCompound(Compound.Temperature, CompoundAmountType.Biome, out var temperatureAmount))
            return 0.0f;

        float temperature = temperatureAmount.Ambient;
        float speed = cache.GetSpeedForSpecies(microbeSpecies);

        // If temperature is not high enough, no bonus
        if (temperature <= 60.0f)
            return 0.0f;

        // Calculate score based on speed
        // The faster the species, the higher the score
        float normalizedSpeed = speed / Constants.MAX_SPECIES_SPEED;
        return normalizedSpeed;
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

    /// <summary>
    ///   Gets the description of this selection pressure for display purposes.
    /// </summary>
    /// <returns>Localized description string</returns>
    public override LocalizedString GetDescription()
    {
        return new LocalizedString("TEMPERATURE_SESSILITY_PRESSURE_DESCRIPTION");
    }
}
