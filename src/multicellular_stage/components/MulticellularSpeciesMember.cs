namespace Components;

using System;
using SharedBase.Archive;

/// <summary>
///   Entity is a multicellular thing. Still exists in the microbial environment.
/// </summary>
[ComponentIsReadByDefault]
public struct MulticellularSpeciesMember : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public MulticellularSpecies Species;

    /// <summary>
    ///   For each part of a multicellular species, the cell type they are must be known
    /// </summary>
    public CellType MulticellularCellType;

    /// <summary>
    ///   Used to keep track of which part of a body plan a non-first cell in a multicellular colony is.
    ///   This is required for regrowing after losing a cell. This is the index of
    ///   <see cref="MulticellularCellType"/> in the <see cref="MulticellularSpecies.Cells"/>
    /// </summary>
    public int MulticellularBodyPlanPartIndex;

    // /// <summary>
    // ///   Set to false if the species is changed
    // /// </summary>
    // public bool SpeciesApplied;

    public MulticellularSpeciesMember(MulticellularSpecies species, CellType cellType,
        int cellBodyPlanIndex)
    {
        if (cellBodyPlanIndex < 0 || cellBodyPlanIndex >= species.Cells.Count)
            throw new ArgumentException("Bad body plan index given");

        Species = species;
        MulticellularCellType = cellType;

        MulticellularBodyPlanPartIndex = cellBodyPlanIndex;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentMulticellularSpeciesMember;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Species);
        writer.WriteObject(MulticellularCellType);
        writer.Write(MulticellularBodyPlanPartIndex);
    }
}

public static class MulticellularSpeciesMemberHelpers
{
    public static MulticellularSpeciesMember ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > MulticellularSpeciesMember.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, MulticellularSpeciesMember.SERIALIZATION_VERSION);

        return new MulticellularSpeciesMember
        {
            Species = reader.ReadObject<MulticellularSpecies>(),
            MulticellularCellType = reader.ReadObject<CellType>(),
            MulticellularBodyPlanPartIndex = reader.ReadInt32(),
        };
    }
}
