/// <summary>
///   Microbe variant of the spawn info giving more microbe-specific data access
/// </summary>
public interface IMicrobeSpawnEnvironment : ISpawnEnvironmentInfo
{
    public BiomeConditions CurrentBiome { get; }
}

public class DummyMicrobeSpawnEnvironment : IMicrobeSpawnEnvironment
{
    // ReSharper disable once StringLiteralTypo
    public DummyMicrobeSpawnEnvironment(string biomeType = "aavolcanic_vent")
    {
        CurrentBiome = SimulationParameters.Instance.GetBiome(biomeType).Conditions;
    }

    public IDaylightInfo DaylightInfo { get; set; } = new DummyLightCycle();
    public BiomeConditions CurrentBiome { get; set; }
}
