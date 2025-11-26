namespace Components;

using SharedBase.Archive;

/// <summary>
///   Marks entity as the player's controlled character
/// </summary>
[ComponentIsReadByDefault]
public struct PlayerMarker : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentPlayerMarker;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        // No fields to write
    }
}

public static class PlayerMarkerHelpers
{
    public static PlayerMarker ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > PlayerMarker.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, PlayerMarker.SERIALIZATION_VERSION);

        return default(PlayerMarker);
    }
}
