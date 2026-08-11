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

    public Entity EmittedBy;
    public Entity LockedOntoTarget;
    public Entity MergingWith;

    // TODO: use to increase the force
    public float MergingTimePassed;

    public bool HasTarget;
    public bool IsMerging;

    /// <summary>
    ///   Once set to true, this gamete cell will not do anything again.
    /// </summary>
    public bool IsUsed;

    public bool IsPlayer;

    // Not saved as this is temporary data
    public bool IsSensorCreated;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentGameteCell;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)ThisGameteType);
        writer.WriteObject(ForSpecies);
        writer.WriteAnyRegisteredValueAsObject(EmittedBy);
        writer.WriteAnyRegisteredValueAsObject(LockedOntoTarget);
        writer.WriteAnyRegisteredValueAsObject(MergingWith);
        writer.Write(MergingTimePassed);
        writer.Write(HasTarget);
        writer.Write(IsMerging);
        writer.Write(IsPlayer);
        writer.Write(IsUsed);
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
            EmittedBy = reader.ReadObject<Entity>(),
            LockedOntoTarget = reader.ReadObject<Entity>(),
            MergingWith = reader.ReadObject<Entity>(),
            MergingTimePassed = reader.ReadFloat(),
            HasTarget = reader.ReadBool(),
            IsMerging = reader.ReadBool(),
            IsPlayer = reader.ReadBool(),
            IsUsed = reader.ReadBool(),
        };

        return result;
    }
}
