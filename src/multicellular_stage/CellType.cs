using System;
using System.Collections.Generic;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Type of cell in a multicellular species. There can be multiple instances of a cell type placed at once
/// </summary>
public class CellType : ICellDefinition, IReadOnlyCellTypeDefinition, ICloneable, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 3;

    public CellType(OrganelleLayout<OrganelleTemplate> organelles, MembraneType membraneType)
    {
        ModifiableOrganelles = organelles;
        MembraneType = membraneType;
        CanEngulf = membraneType.CanEngulf;
    }

    public CellType(MembraneType membraneType)
    {
        MembraneType = membraneType;
        ModifiableOrganelles = new OrganelleLayout<OrganelleTemplate>();
        CanEngulf = membraneType.CanEngulf;
    }

    /// <summary>
    ///   Creates a cell type from the cell type of microbe species
    /// </summary>
    /// <param name="microbeSpecies">The microbe species to take the cell type parameters from</param>
    /// <param name="workMemory1">Temporary memory needed to copy organelle data</param>
    /// <param name="workMemory2">More temporary memory</param>
    public CellType(MicrobeSpecies microbeSpecies, List<Hex> workMemory1, List<Hex> workMemory2) :
        this(microbeSpecies.MembraneType)
    {
        foreach (var organelle in microbeSpecies.Organelles)
        {
            ModifiableOrganelles.AddFast(organelle.Clone(), workMemory1, workMemory2);
        }

        MembraneRigidity = microbeSpecies.MembraneRigidity;
        Colour = microbeSpecies.SpeciesColour;
        IsBacteria = microbeSpecies.IsBacteria;
        CanEngulf = microbeSpecies.CanEngulf;
        CellTypeName = Localization.Translate("STEM_CELL_NAME");
    }

    // TODO: avoid this adapter object allocation
    public IReadOnlyOrganelleLayout<IReadOnlyOrganelleTemplate> Organelles => field ??=
        new ReadonlyOrganelleLayoutAdapter<IReadOnlyOrganelleTemplate, OrganelleTemplate>(ModifiableOrganelles);

    public OrganelleLayout<OrganelleTemplate> ModifiableOrganelles { get; }

    public string CellTypeName { get; set; } = "error";
    public int MPCost { get; set; } = 15;

    public string? SplitFromTypeName { get; set; }

    /// <summary>
    ///   Cached specialization bonus for this cell type.
    /// </summary>
    public float SpecializationBonus { get; set; }

    public MembraneType MembraneType { get; set; }
    public float MembraneRigidity { get; set; }
    public Color Colour { get; set; }
    public bool IsBacteria { get; set; }
    public float BaseRotationSpeed { get; set; }
    public bool CanEngulf { get; }

    public string FormattedName => CellTypeName;
    public string ReadableName => FormattedName;

    public ISimulationPhotographable.SimulationType SimulationToPhotograph =>
        ISimulationPhotographable.SimulationType.MicrobeGraphics;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.CellType;
    public bool CanBeReferencedInArchive => true;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.CellType)
            throw new NotSupportedException();

        writer.WriteObject((CellType)obj);
    }

    public static CellType ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var result = new CellType(reader.ReadObject<OrganelleLayout<OrganelleTemplate>>(),
            reader.ReadObject<MembraneType>())
        {
            CellTypeName = reader.ReadString() ?? throw new NullArchiveObjectException(),
            MPCost = reader.ReadInt32(),
            MembraneRigidity = reader.ReadFloat(),
            Colour = reader.ReadColor(),
            IsBacteria = reader.ReadBool(),
            BaseRotationSpeed = reader.ReadFloat(),
        };

        if (version > 1)
            result.SplitFromTypeName = reader.ReadString();

        if (version > 2)
        {
            result.SpecializationBonus = reader.ReadFloat();
        }
        else
        {
            // Just like microbes, older cell types will get eventually updated by something to have a valid value
            result.SpecializationBonus = 1;
        }

        return result;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(ModifiableOrganelles);
        writer.WriteObject(MembraneType);

        writer.Write(CellTypeName);
        writer.Write(MPCost);
        writer.Write(MembraneRigidity);
        writer.Write(Colour);
        writer.Write(IsBacteria);
        writer.Write(BaseRotationSpeed);

        writer.Write(SplitFromTypeName);

        writer.Write(SpecializationBonus);
    }

    public bool RepositionToOrigin()
    {
        var changes = ModifiableOrganelles.RepositionToOrigin();
        CalculateRotationSpeed();

        // We don't have another on-edit callback, so we do this update here
        CalculateSpecialization();

        return changes;
    }

    public void UpdateNameIfValid(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName))
            CellTypeName = newName;
    }

    /// <summary>
    ///   Checks if this cell type is a brain tissue type
    /// </summary>
    /// <returns>True when this is a brain tissue type</returns>
    /// <remarks>
    ///   <para>
    ///     TODO: make this check much more comprehensive to make brain tissue type more distinct
    ///   </para>
    /// </remarks>
    public bool IsBrainTissueType()
    {
        foreach (var organelle in Organelles)
        {
            if (organelle.Definition.HasFeatureTag(OrganelleFeatureTag.Axon))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsMuscularTissueType()
    {
        foreach (var organelle in Organelles)
        {
            if (organelle.Definition.HasFeatureTag(OrganelleFeatureTag.Myofibril))
            {
                return true;
            }
        }

        return false;
    }

    public void CalculateSpecialization()
    {
        SpecializationBonus =
            MicrobeInternalCalculations.CalculateSpecializationBonus(ModifiableOrganelles,
                new Dictionary<OrganelleDefinition, int>());
    }

    public void SetupWorldEntities(IWorldSimulation worldSimulation)
    {
        GeneralCellPropertiesHelpers.SetupWorldEntities(this, worldSimulation);
    }

    public Vector3 CalculatePhotographDistance(IWorldSimulation worldSimulation)
    {
        return GeneralCellPropertiesHelpers.CalculatePhotographDistance(worldSimulation);
    }

    public bool StateHasStabilized(IWorldSimulation worldSimulation)
    {
        return MicrobeSpecies.StateHasStabilizedImpl(worldSimulation);
    }

    /// <summary>
    ///   Replaces this type's data with the data from another type.
    /// </summary>
    /// <param name="otherType">Where to copy data from. Note that this does not deep copy the data!</param>
    /// <param name="hexTemporaryMemory">Work memory</param>
    /// <param name="hexTemporaryMemory2">Work memory 2</param>
    /// <param name="shouldUpdatePosition">If true, repositions the organelles after copying to origin</param>
    public void CopyFrom(CellType otherType, List<Hex> hexTemporaryMemory, List<Hex> hexTemporaryMemory2,
        bool shouldUpdatePosition = false)
    {
        // Code very similar to what CellEditorComponent does on applying changes
        ModifiableOrganelles.Clear();

        // Even in a multicellular context, it should always be safe to apply the organelle growth order
        foreach (var organelle in otherType.ModifiableOrganelles)
        {
            var organelleToAdd = organelle.Clone();
            ModifiableOrganelles.AddFast(organelleToAdd, hexTemporaryMemory, hexTemporaryMemory2);
        }

        if (shouldUpdatePosition)
            RepositionToOrigin();

        // Update bacteria status
        IsBacteria = otherType.IsBacteria;

        UpdateNameIfValid(otherType.CellTypeName);

        // Update membrane
        MembraneType = otherType.MembraneType;
        Colour = otherType.Colour;
        MembraneRigidity = otherType.MembraneRigidity;

        SpecializationBonus = otherType.SpecializationBonus;
    }

    public object Clone()
    {
        var result = new CellType(MembraneType)
        {
            CellTypeName = CellTypeName,
            MPCost = MPCost,
            MembraneRigidity = MembraneRigidity,
            Colour = Colour,
            IsBacteria = IsBacteria,
            SpecializationBonus = SpecializationBonus,
        };

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        foreach (var organelle in Organelles)
        {
            result.ModifiableOrganelles.AddFast(organelle.Clone(), workMemory1, workMemory2);
        }

        return result;
    }

    public ulong GetVisualHashCode()
    {
        // This code is copied from MicrobeSpecies
        var count = Organelles.Count;

        var hash = PersistentStringHash.GetHash(MembraneType.InternalName) * 5743;
        hash ^= (ulong)MembraneRigidity.GetHashCode() * 5749;
        hash ^= (IsBacteria ? 1UL : 0UL) * 5779UL;
        hash ^= (ulong)count * 131;

        // Additionally, apply colour hash; this line doesn't appear in MicrobeSpecies, because colour hash
        // is applied by MicrobeSpecies' base class.
        hash ^= Colour.GetVisualHashCode();

        var list = ModifiableOrganelles.Organelles;

        for (int i = 0; i < count; ++i)
        {
            // Organelles in different order don't matter (in terms of visuals), so we don't apply any loop-specific
            // stuff here
            unchecked
            {
                hash += list[i].GetVisualHashCode() * 13;
            }
        }

        return hash ^ Constants.VISUAL_HASH_CELL;
    }

    public override int GetHashCode()
    {
        var count = Organelles.Count;

        int hash = CellTypeName.GetHashCode() ^ MembraneType.InternalName.GetHashCode() * 5743 ^
            MembraneRigidity.GetHashCode() * 5749 ^ (IsBacteria ? 1 : 0) * 5779 ^ count * 131;

        var list = ModifiableOrganelles.Organelles;

        for (int i = 0; i < count; ++i)
        {
            hash ^= list[i].GetHashCode() * 13;
        }

        return hash;
    }

    public override string ToString()
    {
        return $"CellType \"{CellTypeName}\" with {Organelles.Count} organelles";
    }

    private void CalculateRotationSpeed()
    {
        // TODO: switch this to use a read only interface
        BaseRotationSpeed = MicrobeInternalCalculations.CalculateRotationSpeed(ModifiableOrganelles.Organelles);
    }
}
