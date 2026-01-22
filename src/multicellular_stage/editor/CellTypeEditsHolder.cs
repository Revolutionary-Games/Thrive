using System.Collections.Generic;
using SharedBase.Archive;

/// <summary>
///   For the new MP system to work, cell type edits in multicellular need to be buffered until the end of the editor
///   session. This class holds those edits.
/// </summary>
public class CellTypeEditsHolder : IArchiveUpdatable
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Mapping from original cell types to edited cell types. Don't use a dictionary as the hash code changes on
    ///   name changes, so we need to use a plain list, which should be efficient enough as there aren't that many
    ///   items.
    /// </summary>
    private readonly List<(CellType Original, CellType NewType)> cellTypeMapping = new();

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.CellTypeEditsHolder;

    public void Reset()
    {
        cellTypeMapping.Clear();
    }

    /// <summary>
    ///   Gets the original *or* a holder if an edit has been started for this type
    /// </summary>
    /// <param name="cellType">Cell type to process</param>
    /// <returns>The original type or an edit holder if edit has been started</returns>
    public CellType GetCellType(CellType cellType)
    {
        foreach (var tuple in cellTypeMapping)
        {
            if (tuple.Original == cellType)
                return tuple.NewType;
        }

        return cellType;
    }

    /// <summary>
    ///   Ensure that an edit has been started for a type
    /// </summary>
    /// <returns>The edit holder for the type</returns>
    public CellType GetEditedCellType(CellType cellType)
    {
        foreach (var tuple in cellTypeMapping)
        {
            if (tuple.Original == cellType)
                return tuple.NewType;
        }

        throw new KeyNotFoundException("Given cell type has not been marked as edited yet");
    }

    public CellType BeginOrContinueEdit(CellType cellType)
    {
        foreach (var tuple in cellTypeMapping)
        {
            if (tuple.Original == cellType)
                return tuple.NewType;
        }

        var type = (CellType)cellType.Clone();
        cellTypeMapping.Add((cellType, type));
        return type;
    }

    /// <summary>
    ///   Reverse lookup for cell type edits
    /// </summary>
    public CellType GetOriginalType(CellType cellType)
    {
        foreach (var pair in cellTypeMapping)
        {
            if (pair.NewType == cellType)
                return pair.Original;
        }

        return cellType;
    }

    public void ApplyChanges()
    {
        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        foreach (var pair in cellTypeMapping)
        {
            pair.Original.CopyFrom(pair.NewType, workMemory1, workMemory2);
        }

        Reset();
    }

    public void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(cellTypeMapping);
    }

    public void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        cellTypeMapping.Clear();
        foreach (var cellType in reader.ReadObject<List<(CellType Old, CellType New)>>())
        {
            cellTypeMapping.Add(cellType);
        }
    }
}
