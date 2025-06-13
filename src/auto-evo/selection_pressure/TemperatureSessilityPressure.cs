namespace AutoEvo;

using Newtonsoft.Json;

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

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        // Get temperature from the patch
        if (!patch.Biome.TryGetCompound(Compound.Temperature, CompoundAmountType.Biome, out var temperatureAmount))
            return 1.0f;

        float temperature = temperatureAmount.Ambient;
        float speed = cache.GetSpeedForSpecies(microbeSpecies);

        // If temperature is not high enough, no penalty
        if (temperature <= 60.0f)
            return 1.0f;

        // Calculate penalty based on speed
        // The slower the species, the higher the penalty
        float speedFactor = speed / Constants.MAX_SPECIES_SPEED;
        float penalty = 1.0f - (speedFactor * 0.7f);

        return Math.Max(penalty, 0.3f);
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }

    public override LocalizedString GetDescription()
    {
        return new LocalizedString("TEMPERATURE_SESSILITY_PRESSURE_DESCRIPTION");
    }
}
