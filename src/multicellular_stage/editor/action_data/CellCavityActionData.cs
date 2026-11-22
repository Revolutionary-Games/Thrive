using System;
using SharedBase.Archive;

/// <summary>
///   Changes whether a placed cell represents a cavity. Captures its position for replaying facade edits.
/// </summary>
public class CellCavityActionData : EditorCombinableActionData<MulticellularSpecies>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public readonly HexWithData<CellTemplate> Cell;
    public readonly Hex Location;
    public readonly int Orientation;
    public readonly bool OldIsCavity;
    public readonly bool NewIsCavity;

    public CellCavityActionData(HexWithData<CellTemplate> cell, bool oldIsCavity, bool newIsCavity) :
        this(cell, cell.Position, cell.Orientation, oldIsCavity, newIsCavity)
    {
    }

    private CellCavityActionData(HexWithData<CellTemplate> cell, Hex location, int orientation,
        bool oldIsCavity, bool newIsCavity)
    {
        Cell = cell;
        Location = location;
        Orientation = orientation;
        OldIsCavity = oldIsCavity;
        NewIsCavity = newIsCavity;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CellCavityActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.CellCavityActionData)
            throw new NotSupportedException();

        writer.WriteObject((CellCavityActionData)obj);
    }

    public static CellCavityActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new CellCavityActionData(reader.ReadObject<HexWithData<CellTemplate>>(), reader.ReadHex(),
            reader.ReadInt32(), reader.ReadBool(), reader.ReadBool());

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Cell);
        writer.Write(Location);
        writer.Write(Orientation);
        writer.Write(OldIsCavity);
        writer.Write(NewIsCavity);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
