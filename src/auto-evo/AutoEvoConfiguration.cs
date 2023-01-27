/// <summary>
///   Settings for auto-evo that have been (potentially) tweaked
/// </summary>
[CustomizedRegistryType]
public class AutoEvoConfiguration : IAutoEvoConfiguration
{
    public int MutationsPerSpecies { get; set; }

    public bool AllowSpeciesToNotMutate { get; set; }

    public int MoveAttemptsPerSpecies { get; set; }

    public bool AllowSpeciesToNotMigrate { get; set; }

    public int LowBiodiversityLimit { get; set; }

    public float SpeciesSplitByMutationThresholdPopulationFraction { get; set; }

    public int SpeciesSplitByMutationThresholdPopulationAmount { get; set; }

    public float BiodiversityAttemptFillChance { get; set; }

    public bool UseBiodiversityForceSplit { get; set; }

    public float BiodiversityFromNeighbourPatchChance { get; set; }

    public bool BiodiversityNearbyPatchIsFreePopulation { get; set; }

    public int NewBiodiversityIncreasingSpeciesPopulation { get; set; }

    public bool BiodiversitySplitIsMutated { get; set; }

    public bool StrictNicheCompetition { get; set; }

    public int MaximumSpeciesInPatch { get; set; }

    public bool ProtectNewCellsFromSpeciesCap { get; set; }

    public bool ProtectMigrationsFromSpeciesCap { get; set; }

    public bool RefundMigrationsInExtinctions { get; set; }
}
