using System;
using SharedBase.Archive;

/// <summary>
///   Stores information for duplicating and deleting cell types. Note that while this uses the multicellular species
///   type, this also applies for macroscopic species.
/// </summary>
public class DuplicateDeleteCellTypeData : EditorCombinableActionData<MulticellularSpecies>
{
    public const ushort SERIALIZATION_VERSION = 2;

    public readonly CellType CellType;
    public readonly bool Delete;

    public DuplicateDeleteCellTypeData(CellType cellType, bool delete)
    {
        CellType = cellType;
        Delete = delete;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.DuplicateDeleteCellTypeData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.DuplicateDeleteCellTypeData)
            throw new NotSupportedException();

        writer.WriteObject((DuplicateDeleteCellTypeData)obj);
    }

    public static DuplicateDeleteCellTypeData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        DuplicateDeleteCellTypeData instance;
        if (version < 2)
        {
            instance = new DuplicateDeleteCellTypeData(reader.ReadObject<CellType>(), false);
        }
        else
        {
            instance = new DuplicateDeleteCellTypeData(reader.ReadObject<CellType>(), reader.ReadBool());
        }

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(CellType);
        writer.Write(Delete);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
