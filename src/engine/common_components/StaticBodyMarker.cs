namespace Components;

using SharedBase.Archive;

/// <summary>
///   Marks that an entity cannot be moved by physics, thus excluding it from reading physics position data.
///   Makes static bodies much more efficient.
/// </summary>
public struct StaticBodyMarker : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentStaticBodyMarker;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        // No fields
    }
}

public static class StaticBodyMarkerHelpers
{
    public static StaticBodyMarker ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > StaticBodyMarker.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, StaticBodyMarker.SERIALIZATION_VERSION);

        return default(StaticBodyMarker);
    }
}
