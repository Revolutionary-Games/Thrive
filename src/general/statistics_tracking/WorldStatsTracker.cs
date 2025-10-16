using Newtonsoft.Json;
using SharedBase.Archive;
using UnlockConstraints;

/// <summary>
///   Relays statistics about the world and the player to the organelle unlock system (and later achievements)
/// </summary>
[UseThriveSerializer]
public class WorldStatsTracker : IUnlockStateDataSource, IArchiveUpdatable
{
    public const ushort SERIALIZATION_VERSION = 1;

    [JsonProperty]
    public SimpleStatistic TotalEngulfedByPlayer { get; private set; } = new();

    [JsonProperty]
    public SimpleStatistic TotalDigestedByPlayer { get; private set; } = new();

    [JsonProperty]
    public SimpleStatistic TotalPlayerDeaths { get; private set; } = new();

    [JsonProperty]
    public ReproductionStatistic PlayerReproductionStatistic { get; private set; } = new();

    [JsonProperty]
    public DamageStatistic PlayerReceivedDamage { get; private set; } = new();

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.WorldStatsTracker;

    public void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.WriteObjectProperties(TotalEngulfedByPlayer);
        writer.WriteObjectProperties(TotalDigestedByPlayer);
        writer.WriteObjectProperties(TotalPlayerDeaths);
        writer.WriteObjectProperties(PlayerReproductionStatistic);
        writer.WriteObjectProperties(PlayerReceivedDamage);
    }

    public void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        reader.ReadObjectProperties(TotalEngulfedByPlayer);
        reader.ReadObjectProperties(TotalDigestedByPlayer);
        reader.ReadObjectProperties(TotalPlayerDeaths);
        reader.ReadObjectProperties(PlayerReproductionStatistic);
        reader.ReadObjectProperties(PlayerReceivedDamage);
    }
}
