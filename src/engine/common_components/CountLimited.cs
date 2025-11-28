namespace Components;

using SharedBase.Archive;

/// <summary>
///   Limit for how many entities can exist in the configured group. Requires <see cref="WorldPosition"/> as
///   despawning is done far away from the player.
/// </summary>
public struct CountLimited : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public LimitGroup Group;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentCountLimited;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)Group);
    }
}

public static class CountLimitedHelpers
{
    public static CountLimited ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > CountLimited.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, CountLimited.SERIALIZATION_VERSION);

        return new CountLimited
        {
            Group = (LimitGroup)reader.ReadInt32(),
        };
    }
}
