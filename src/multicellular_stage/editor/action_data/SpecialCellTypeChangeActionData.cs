using System;
using SharedBase.Archive;

public class SpecialCellTypeChangeActionData : EditorCombinableActionData<MulticellularSpecies>
{
    public const ushort SERIALIZATION_VERSION = 2;

    public readonly CellType? OldCellType;
    public readonly CellType? NewCellType;

    public readonly SpecialCellArchetype CellArchetype;

    public SpecialCellTypeChangeActionData(CellType? oldCellType, CellType? newCellType,
        SpecialCellArchetype cellArchetype)
    {
        OldCellType = oldCellType;
        NewCellType = newCellType;
        CellArchetype = cellArchetype;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.SpecialCellTypeChangeActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.SpecialCellTypeChangeActionData)
            throw new NotSupportedException();

        writer.WriteObject((SpecialCellTypeChangeActionData)obj);
    }

    public static SpecialCellTypeChangeActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var oldCellType = reader.ReadObjectOrNull<CellType>();
        var newCellType = reader.ReadObjectOrNull<CellType>();

        // This was previously a class specifically for changing spore cell type
        var cellArchetype = SERIALIZATION_VERSION > 1 ? (SpecialCellArchetype)reader.ReadInt32()
            : SpecialCellArchetype.Spore;

        var instance = new SpecialCellTypeChangeActionData(oldCellType, newCellType, cellArchetype);

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObjectOrNull(OldCellType);
        writer.WriteObjectOrNull(NewCellType);

        writer.Write((int)CellArchetype);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
