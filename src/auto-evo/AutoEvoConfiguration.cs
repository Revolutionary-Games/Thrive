using Newtonsoft.Json;

public class AutoEvoConfiguration : IRegistryType
{
    [JsonProperty]
    public int MutationsPerSpecies { get; private set; }

    [JsonProperty]
    public bool AllowNoMutation { get; private set; }

    [JsonProperty]
    public int MoveAttemptsPerSpecies { get; private set; }

    [JsonProperty]
    public bool AllowNoMigration { get; private set; }

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

    /// <summary>
    ///   Maximum number of species kept in a patch at the end of auto-evo.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Always ignores the player species, and may ignore new cells and migrations.
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public int MaximumSpeciesInPatch { get; private set; }

    /// <summary>
    ///   If true newly created species can't be forced to go extinct in the same auto-evo cycle
    /// </summary>
    [JsonProperty]
    public bool ProtectNewCellsFromSpeciesCap { get; private set; }

    /// <summary>
    ///   TODO: this is meant to protect migrating species from forced extinction, however this is currently only
    ///   assumes that migrations don't happen and works with population numbers as if they weren't there, which
    ///   doesn't really guarantee that this does anything useful
    /// </summary>
    [JsonProperty]
    public bool ProtectMigrationsFromSpeciesCap { get; private set; }

    /// <summary>
    ///   Whether or not a migration wiped by a force extinction should be refunded in the original patch.
    /// </summary>
    [JsonProperty]
    public bool RefundMigrationsInExtinctions { get; private set; }

    /// <summary>
    ///   Unused
    /// </summary>
    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (MutationsPerSpecies < 0)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", GetType().Name,
                "Mutations per species must be positive");
        }

        if (MoveAttemptsPerSpecies < 0)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", GetType().Name,
                "Move attempts per species must be positive");
        }

        if (SpeciesSplitByMutationThresholdPopulationFraction is < 0 or > 1)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", GetType().Name,
                "SpeciesSplitByMutationThresholdPopulationFraction not between 0 and 1");
        }

        if (SpeciesSplitByMutationThresholdPopulationFraction <= 0 ||
            SpeciesSplitByMutationThresholdPopulationAmount <= 0)
        {
            SpeciesSplitByMutationThresholdPopulationFraction = 0;
            SpeciesSplitByMutationThresholdPopulationAmount = 0;
        }
    }

    public void ApplyTranslations()
    {
    }
}
