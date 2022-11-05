using Newtonsoft.Json;

/// <summary>
///   Settings for auto-evo that are loaded from the configuration JSON
/// </summary>
[JSONAlwaysDynamicType]
public class PredefinedAutoEvoConfiguration : IAutoEvoConfiguration, IRegistryType
{
    [JsonProperty]
    public int MutationsPerSpecies { get; private set; }

    [JsonProperty]
    public bool AllowSpeciesToNotMutate { get; private set; }

    [JsonProperty]
    public int MoveAttemptsPerSpecies { get; private set; }

    [JsonProperty]
    public bool AllowSpeciesToNotMigrate { get; private set; }

    [JsonProperty]
    public int LowBiodiversityLimit { get; private set; }

    [JsonProperty]
    public float SpeciesSplitByMutationThresholdPopulationFraction { get; private set; }

    [JsonProperty]
    public int SpeciesSplitByMutationThresholdPopulationAmount { get; private set; }

    [JsonProperty]
    public float BiodiversityAttemptFillChance { get; private set; }

    [JsonProperty]
    public bool UseBiodiversityForceSplit { get; private set; }

    [JsonProperty]
    public float BiodiversityFromNeighbourPatchChance { get; private set; }

    [JsonProperty]
    public bool BiodiversityNearbyPatchIsFreePopulation { get; private set; }

    [JsonProperty]
    public int NewBiodiversityIncreasingSpeciesPopulation { get; private set; }

    [JsonProperty]
    public bool BiodiversitySplitIsMutated { get; private set; }

    [JsonProperty]
    public bool StrictNicheCompetition { get; private set; }

    [JsonProperty]
    public int MaximumSpeciesInPatch { get; private set; }

    [JsonProperty]
    public bool ProtectNewCellsFromSpeciesCap { get; private set; }

    [JsonProperty]
    public bool ProtectMigrationsFromSpeciesCap { get; private set; }

    [JsonProperty]
    public bool RefundMigrationsInExtinctions { get; private set; }

    /// <summary>
    ///   Set to <see cref="SimulationParameters.AUTO_EVO_CONFIGURATION_NAME"/> to make saving and loading work
    /// </summary>
    public string InternalName { get; set; } = null!;

    public void ApplyTranslations()
    {
    }

    public void Check(string name)
    {
        this.Check();
    }
}
