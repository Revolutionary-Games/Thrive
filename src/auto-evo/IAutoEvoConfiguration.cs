/// <summary>
///   Auto-evo configuration parameters to be used
/// </summary>
[SupportsCustomizedRegistryType(typeof(AutoEvoConfiguration))]
public interface IAutoEvoConfiguration : IRegistryAssignable
{
    public int MutationsPerSpecies { get; }

    public bool AllowSpeciesToNotMutate { get; }

    public int MoveAttemptsPerSpecies { get; }

    public bool AllowSpeciesToNotMigrate { get; }

    public int LowBiodiversityLimit { get; }

    public float SpeciesSplitByMutationThresholdPopulationFraction { get; }

    public int SpeciesSplitByMutationThresholdPopulationAmount { get; }

    public float BiodiversityAttemptFillChance { get; }

    public bool UseBiodiversityForceSplit { get; }

    public float BiodiversityFromNeighbourPatchChance { get; }

    public bool BiodiversityNearbyPatchIsFreePopulation { get; }

    public int NewBiodiversityIncreasingSpeciesPopulation { get; }

    public bool BiodiversitySplitIsMutated { get; }

    public bool StrictNicheCompetition { get; }

    /// <summary>
    ///   Maximum number of species kept in a patch at the end of auto-evo.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Always ignores the player species, and may ignore new cells and migrations.
    ///   </para>
    /// </remarks>
    public int MaximumSpeciesInPatch { get; }

    /// <summary>
    ///   If true newly created species can't be forced to go extinct in the same auto-evo cycle
    /// </summary>
    public bool ProtectNewCellsFromSpeciesCap { get; }

    /// <summary>
    ///   TODO: this is meant to protect migrating species from forced extinction, however this is currently only
    ///   assumes that migrations don't happen and works with population numbers as if they weren't there, which
    ///   doesn't really guarantee that this does anything useful
    /// </summary>
    public bool ProtectMigrationsFromSpeciesCap { get; }

    /// <summary>
    ///   Whether or not a migration wiped by a force extinction should be refunded in the original patch.
    /// </summary>
    public bool RefundMigrationsInExtinctions { get; }
}

public static class AutoEvoConfigurationHelpers
{
    public static void Check(this IAutoEvoConfiguration configuration)
    {
        if (configuration.MutationsPerSpecies < 0)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", configuration.GetType().Name,
                "Mutations per species must be positive");
        }

        if (configuration.MoveAttemptsPerSpecies < 0)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", configuration.GetType().Name,
                "Move attempts per species must be positive");
        }

        if (configuration.SpeciesSplitByMutationThresholdPopulationFraction is < 0 or > 1)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", configuration.GetType().Name,
                "SpeciesSplitByMutationThresholdPopulationFraction not between 0 and 1");
        }

        if (configuration.SpeciesSplitByMutationThresholdPopulationAmount is < 0)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", configuration.GetType().Name,
                "SpeciesSplitByMutationThresholdPopulationAmount is negative");
        }
    }

    public static AutoEvoConfiguration Clone(this IAutoEvoConfiguration configuration)
    {
        return new AutoEvoConfiguration
        {
            AllowSpeciesToNotMutate = configuration.AllowSpeciesToNotMutate,
            AllowSpeciesToNotMigrate = configuration.AllowSpeciesToNotMigrate,
            BiodiversityAttemptFillChance = configuration.BiodiversityAttemptFillChance,
            BiodiversityFromNeighbourPatchChance = configuration.BiodiversityFromNeighbourPatchChance,
            BiodiversityNearbyPatchIsFreePopulation = configuration.BiodiversityNearbyPatchIsFreePopulation,
            BiodiversitySplitIsMutated = configuration.BiodiversitySplitIsMutated,
            LowBiodiversityLimit = configuration.LowBiodiversityLimit,
            MaximumSpeciesInPatch = configuration.MaximumSpeciesInPatch,
            MoveAttemptsPerSpecies = configuration.MoveAttemptsPerSpecies,
            MutationsPerSpecies = configuration.MutationsPerSpecies,
            NewBiodiversityIncreasingSpeciesPopulation = configuration.NewBiodiversityIncreasingSpeciesPopulation,
            ProtectMigrationsFromSpeciesCap = configuration.ProtectMigrationsFromSpeciesCap,
            ProtectNewCellsFromSpeciesCap = configuration.ProtectNewCellsFromSpeciesCap,
            RefundMigrationsInExtinctions = configuration.RefundMigrationsInExtinctions,
            StrictNicheCompetition = configuration.StrictNicheCompetition,
            SpeciesSplitByMutationThresholdPopulationAmount =
                configuration.SpeciesSplitByMutationThresholdPopulationAmount,
            SpeciesSplitByMutationThresholdPopulationFraction =
                configuration.SpeciesSplitByMutationThresholdPopulationFraction,
            UseBiodiversityForceSplit = configuration.UseBiodiversityForceSplit,
        };
    }
}
