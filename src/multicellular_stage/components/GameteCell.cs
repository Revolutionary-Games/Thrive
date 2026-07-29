namespace Components;

using Arch.Core;
using SharedBase.Archive;

/// <summary>
///   Marks an entity as a gamete cell.
/// </summary>
public struct GameteCell : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public GameteType ThisGameteType;

    public Species ForSpecies;

    public Entity LockedOntoTarget;
    public bool HasTarget;

    public bool IsPlayer;

    // Not saved as this is temporary data
    public bool IsSensorCreated;
    public bool IsListeningForCollisions;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentGameteCell;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)ThisGameteType);
        writer.WriteObject(ForSpecies);
        writer.WriteAnyRegisteredValueAsObject(LockedOntoTarget);
        writer.Write(HasTarget);
        writer.Write(IsPlayer);
    }
}

public static class GameteCellHelpers
{
    public static GameteCell ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > GameteCell.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, GameteCell.SERIALIZATION_VERSION);

        var result = new GameteCell
        {
            ThisGameteType = (GameteType)reader.ReadInt32(),
            ForSpecies = reader.ReadObject<Species>(),
            LockedOntoTarget = reader.ReadObject<Entity>(),
            HasTarget = reader.ReadBool(),
            IsPlayer = reader.ReadBool(),
        };

        return result;
    }
}
