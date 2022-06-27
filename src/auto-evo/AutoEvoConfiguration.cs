using System;
using Newtonsoft.Json;

public class AutoEvoConfiguration : IRegistryType, ICloneable
{
    [JsonProperty]
    public int MutationsPerSpecies { get; set; }

    [JsonProperty]
    public bool AllowNoMutation { get; set; }

    [JsonProperty]
    public int MoveAttemptsPerSpecies { get; set; }

    [JsonProperty]
    public bool AllowNoMigration { get; set; }

    [JsonProperty]
    public int LowBiodiversityLimit { get; set; }

    [JsonProperty]
    public float SpeciesSplitByMutationThresholdPopulationFraction { get; set; }

    [JsonProperty]
    public int SpeciesSplitByMutationThresholdPopulationAmount { get; set; }

    [JsonProperty]
    public float BiodiversityAttemptFillChance { get; set; }

    [JsonProperty]
    public bool UseBiodiversityForceSplit { get; set; }

    [JsonProperty]
    public float BiodiversityFromNeighbourPatchChance { get; set; }

    [JsonProperty]
    public bool BiodiversityNearbyPatchIsFreePopulation { get; set; }

    [JsonProperty]
    public int NewBiodiversityIncreasingSpeciesPopulation { get; set; }

    [JsonProperty]
    public bool BiodiversitySplitIsMutated { get; set; }

    [JsonProperty]
    public bool StrictNicheCompetition { get; set; }

    /// <summary>
    ///   Maximum number of species kept in a patch at the end of auto-evo.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Always ignores the player species, and may ignore new cells and migrations.
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public int MaximumSpeciesInPatch { get; set; }

    /// <summary>
    ///   If true newly created species can't be forced to go extinct in the same auto-evo cycle
    /// </summary>
    [JsonProperty]
    public bool ProtectNewCellsFromSpeciesCap { get; set; }

    /// <summary>
    ///   TODO: this is meant to protect migrating species from forced extinction, however this is currently only
    ///   assumes that migrations don't happen and works with population numbers as if they weren't there, which
    ///   doesn't really guarantee that this does anything useful
    /// </summary>
    [JsonProperty]
    public bool ProtectMigrationsFromSpeciesCap { get; set; }

    /// <summary>
    ///   Whether or not a migration wiped by a force extinction should be refunded in the original patch.
    /// </summary>
    [JsonProperty]
    public bool RefundMigrationsInExtinctions { get; set; }

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

    public object Clone()
    {
        return new AutoEvoConfiguration
        {
            AllowNoMutation = AllowNoMutation,
            AllowNoMigration = AllowNoMigration,
            BiodiversityAttemptFillChance = BiodiversityAttemptFillChance,
            BiodiversityFromNeighbourPatchChance = BiodiversityFromNeighbourPatchChance,
            BiodiversityNearbyPatchIsFreePopulation = BiodiversityNearbyPatchIsFreePopulation,
            BiodiversitySplitIsMutated = BiodiversitySplitIsMutated,
            InternalName = InternalName,
            LowBiodiversityLimit = LowBiodiversityLimit,
            MaximumSpeciesInPatch = MaximumSpeciesInPatch,
            MoveAttemptsPerSpecies = MoveAttemptsPerSpecies,
            MutationsPerSpecies = MutationsPerSpecies,
            NewBiodiversityIncreasingSpeciesPopulation = NewBiodiversityIncreasingSpeciesPopulation,
            ProtectMigrationsFromSpeciesCap = ProtectMigrationsFromSpeciesCap,
            ProtectNewCellsFromSpeciesCap = ProtectNewCellsFromSpeciesCap,
            RefundMigrationsInExtinctions = RefundMigrationsInExtinctions,
            StrictNicheCompetition = StrictNicheCompetition,
            SpeciesSplitByMutationThresholdPopulationAmount = SpeciesSplitByMutationThresholdPopulationAmount,
            SpeciesSplitByMutationThresholdPopulationFraction = SpeciesSplitByMutationThresholdPopulationFraction,
            UseBiodiversityForceSplit = UseBiodiversityForceSplit,
        };
    }
}
