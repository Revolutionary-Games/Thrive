using System;
using System.Collections.Generic;
using SharedBase.Archive;

/// <summary>
///   Stores information for duplicating and deleting cell types.
/// </summary>
public class DuplicateDeleteCellTypeData : EditorCombinableActionData<MulticellularSpecies>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public readonly CellType CellType;

    public DuplicateDeleteCellTypeData(CellType cellType)
    {
        CellType = cellType;
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

        var instance = new DuplicateDeleteCellTypeData(reader.ReadObject<CellType>());

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(CellType);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        return (0, 0);
    }

    protected override double CalculateBaseCostInternal()
    {
        return 0;
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
