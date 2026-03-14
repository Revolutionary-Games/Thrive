/// <summary>
///   Microbe variant of the spawn info giving more microbe-specific data access
/// </summary>
public interface IMicrobeSpawnEnvironment : ISpawnEnvironmentInfo
{
    public BiomeConditions CurrentBiome { get; }

    /// <summary>
    ///   Gets microbe environmental tolerances in a resolved manner for use with spawning. This method exists to allow
    ///   caching.
    /// </summary>
    /// <param name="microbeSpecies">Species to get tolerances for</param>
    public ResolvedMicrobeTolerances GetSpeciesTolerances(MicrobeSpecies microbeSpecies);

    public ResolvedMicrobeTolerances GetSpeciesTolerances(MulticellularSpecies multicellularSpecies);
}

public class DummyMicrobeSpawnEnvironment : IMicrobeSpawnEnvironment
{
    // ReSharper disable once StringLiteralTypo
    public DummyMicrobeSpawnEnvironment(string biomeType = "aavolcanic_vent")
    {
        CurrentBiome = SimulationParameters.Instance.GetBiome(biomeType).Conditions;
    }

    public IDaylightInfo DaylightInfo { get; set; } = new DummyLightCycle();
    public WorldGenerationSettings WorldSettings { get; set; } = new();
    public BiomeConditions CurrentBiome { get; set; }

    public ResolvedMicrobeTolerances GetSpeciesTolerances(MicrobeSpecies microbeSpecies)
    {
        return new ResolvedMicrobeTolerances
        {
            ProcessSpeedModifier = 1,
            OsmoregulationModifier = 1,
            HealthModifier = 1,
        };
    }

    public ResolvedMicrobeTolerances GetSpeciesTolerances(MulticellularSpecies multicellularSpecies)
    {
        return new ResolvedMicrobeTolerances
        {
            ProcessSpeedModifier = 1,
            OsmoregulationModifier = 1,
            HealthModifier = 1,
        };
    }
}
