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
    public int MoveAttemptsPerSpecies { get; private set; }

    [JsonProperty]
    public bool StrictNicheCompetition { get; private set; }

    [JsonIgnore]
    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.PredefinedAutoEvoConfiguration;

    public static object ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
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
