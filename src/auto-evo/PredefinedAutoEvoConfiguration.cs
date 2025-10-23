using System;
using Newtonsoft.Json;
using SharedBase.Archive;
using ThriveScriptsShared;

/// <summary>
///   Settings for auto-evo that are loaded from the configuration JSON
/// </summary>
public class PredefinedAutoEvoConfiguration : RegistryType, IAutoEvoConfiguration
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

    [JsonIgnore]
    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.PredefinedAutoEvoConfiguration;

    public static object ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        var name = ReadInternalName(reader, version);
        if (name != SimulationParameters.AUTO_EVO_CONFIGURATION_NAME)
            throw new FormatException($"Auto-evo object had unexpected name: {name}");

        return SimulationParameters.Instance.AutoEvoConfiguration;
    }

    public override void Check(string name)
    {
        this.Check();
    }

    public override void ApplyTranslations()
    {
    }
}
