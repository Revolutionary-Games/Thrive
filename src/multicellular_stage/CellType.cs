using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Type of cell in a multicellular species. There can be multiple instances of a cell type placed at once
/// </summary>
[JsonObject(IsReference = true)]
public class CellType : ICellDefinition, ICloneable
{
    [JsonConstructor]
    public CellType(OrganelleLayout<OrganelleTemplate> organelles, MembraneType membraneType)
    {
        Organelles = organelles;
        MembraneType = membraneType;
    }

    public CellType(MembraneType membraneType)
    {
        MembraneType = membraneType;
        Organelles = new OrganelleLayout<OrganelleTemplate>();
    }

    /// <summary>
    ///   Creates a cell type from the cell type of a microbe species
    /// </summary>
    /// <param name="microbeSpecies">The microbe species to take the cell type parameters from</param>
    /// <param name="workMemory1">Temporary memory needed to copy organelle data</param>
    /// <param name="workMemory2">More temporary memory</param>
    public CellType(MicrobeSpecies microbeSpecies, List<Hex> workMemory1, List<Hex> workMemory2) :
        this(microbeSpecies.MembraneType)
    {
        foreach (var organelle in microbeSpecies.Organelles)
        {
            Organelles.AddFast((OrganelleTemplate)organelle.Clone(), workMemory1, workMemory2);
        }

        MembraneRigidity = microbeSpecies.MembraneRigidity;
        Colour = microbeSpecies.Colour;
        IsBacteria = microbeSpecies.IsBacteria;
        CanEngulf = microbeSpecies.CanEngulf;
        TypeName = Localization.Translate("STEM_CELL_NAME");
    }

    [JsonProperty]
    public OrganelleLayout<OrganelleTemplate> Organelles { get; private set; }

    public string TypeName { get; set; } = "error";
    public int MPCost { get; set; } = 15;

    public MembraneType MembraneType { get; set; }
    public float MembraneRigidity { get; set; }
    public Color Colour { get; set; }
    public bool IsBacteria { get; set; }
    public float BaseRotationSpeed { get; set; }
    public bool CanEngulf { get; }

    [JsonIgnore]
    public string FormattedName => TypeName;

    [JsonIgnore]
    public ISimulationPhotographable.SimulationType SimulationToPhotograph =>
        ISimulationPhotographable.SimulationType.MicrobeGraphics;

    public bool RepositionToOrigin()
    {
        var changes = Organelles.RepositionToOrigin();
        CalculateRotationSpeed();
        return changes;
    }

    public void UpdateNameIfValid(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName))
            TypeName = newName;
    }

    /// <summary>
    ///   Checks if this cell type is a brain tissue type
    /// </summary>
    /// <returns>True when this is brain tissue</returns>
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

    public object Clone()
    {
        var result = new CellType(MembraneType)
        {
            TypeName = TypeName,
            MPCost = MPCost,
            MembraneRigidity = MembraneRigidity,
            Colour = Colour,
            IsBacteria = IsBacteria,
        };

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        foreach (var organelle in Organelles)
        {
            result.Organelles.AddFast((OrganelleTemplate)organelle.Clone(), workMemory1, workMemory2);
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

        // Additionally apply colour hash; this line doesn't appear in MicrobeSpecies, because colour hash
        // is applied by MicrobeSpecies' base class.
        hash ^= Colour.GetVisualHashCode();

        var list = Organelles.Organelles;

        for (int i = 0; i < count; ++i)
        {
            // Organelles in different order don't matter (in terms of visuals) so we don't apply any loop specific
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

        int hash = TypeName.GetHashCode() ^ MembraneType.InternalName.GetHashCode() * 5743 ^
            MembraneRigidity.GetHashCode() * 5749 ^ (IsBacteria ? 1 : 0) * 5779 ^ count * 131;

        var list = Organelles.Organelles;

        for (int i = 0; i < count; ++i)
        {
            hash ^= list[i].GetHashCode() * 13;
        }

        return hash;
    }

    private void CalculateRotationSpeed()
    {
        BaseRotationSpeed = MicrobeInternalCalculations.CalculateRotationSpeed(Organelles.Organelles);
    }
}
