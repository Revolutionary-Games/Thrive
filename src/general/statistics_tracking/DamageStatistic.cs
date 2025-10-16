using System.Collections.Generic;
using SharedBase.Archive;

/// <summary>
///   Tracks damage per source type
/// </summary>
public class DamageStatistic : IStatistic, IArchiveUpdatable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public Dictionary<string, float> DamageByType { get; private set; } = new();

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.DamageStatistic;

    public float GetDamageBySource(string sourceName)
    {
        DamageByType.TryGetValue(sourceName, out var damage);
        return damage;
    }

    public void IncrementDamage(string sourceName, float amount)
    {
        DamageByType.TryGetValue(sourceName, out var damage);
        DamageByType[sourceName] = damage + amount;
    }

    public void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(DamageByType);
    }

    public void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        DamageByType = reader.ReadObjectNotNull<Dictionary<string, float>>();
    }
}
