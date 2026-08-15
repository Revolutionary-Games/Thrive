using System;
using SharedBase.Archive;

public class GameteBCellTypeChangeActionData : EditorCombinableActionData<MulticellularSpecies>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public readonly CellType? OldCellType;
    public readonly CellType NewCellType;

    public GameteBCellTypeChangeActionData(CellType? oldCellType, CellType newCellType)
    {
        OldCellType = oldCellType;
        NewCellType = newCellType;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.GameteBCellTypeChangeActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.GameteBCellTypeChangeActionData)
            throw new NotSupportedException();

        writer.WriteObject((GameteBCellTypeChangeActionData)obj);
    }

    public static GameteBCellTypeChangeActionData ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance =
            new GameteBCellTypeChangeActionData(reader.ReadObjectOrNull<CellType>(), reader.ReadObject<CellType>());

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObjectOrNull(OldCellType);
        writer.WriteObject(NewCellType);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
