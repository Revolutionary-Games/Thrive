using System;
using SharedBase.Archive;

public class SporeCellTypeAddActionData : EditorCombinableActionData<MulticellularSpecies>
{
    public const ushort SERIALIZATION_VERSION = 2;

    public readonly CellType SporeCell;

    public bool Delete;

    public SporeCellTypeAddActionData(CellType sporeCell, bool delete)
    {
        SporeCell = sporeCell;
        Delete = delete;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.SporeCellTypeChangeActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.SporeCellTypeChangeActionData)
            throw new NotSupportedException();

        writer.WriteObject((SporeCellTypeAddActionData)obj);
    }

    public static SporeCellTypeAddActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance =
            new SporeCellTypeAddActionData(reader.ReadObject<CellType>(), reader.ReadBool());

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(SporeCell);
        writer.Write(Delete);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
