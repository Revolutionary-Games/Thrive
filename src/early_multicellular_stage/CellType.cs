using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Type of a cell in a multicellular species. There can be multiple instances of a cell type placed at once
/// </summary>
public class CellType : ICellProperties, IPhotographable, ICloneable
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
    public CellType(MicrobeSpecies microbeSpecies) : this(microbeSpecies.MembraneType)
    {
        foreach (var organelle in microbeSpecies.Organelles)
        {
            Organelles.Add((OrganelleTemplate)organelle.Clone());
        }

        MembraneRigidity = microbeSpecies.MembraneRigidity;
        Colour = microbeSpecies.Colour;
        IsBacteria = microbeSpecies.IsBacteria;
        TypeName = TranslationServer.Translate("STEM_CELL_NAME");
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

    [JsonIgnore]
    public string FormattedName => TypeName;

    [JsonIgnore]
    public string SceneToPhotographPath => "res://src/microbe_stage/Microbe.tscn";

    public void RepositionToOrigin()
    {
        Organelles.RepositionToOrigin();
    }

    public void CalculateRotationSpeed()
    {
        BaseRotationSpeed = MicrobeInternalCalculations.CalculateRotationSpeed(Organelles);
    }

    public void UpdateNameIfValid(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName))
            TypeName = newName;
    }

    public void ApplySceneParameters(Spatial instancedScene)
    {
        var microbe = (Microbe)instancedScene;
        microbe.IsForPreviewOnly = true;

        // We need to call _Ready here as the object may not be attached to the scene yet by the photo studio
        microbe._Ready();

        var tempSpecies = new MicrobeSpecies(new MicrobeSpecies(int.MaxValue, string.Empty, string.Empty), this)
        {
            IsBacteria = false,
        };

        microbe.ApplySpecies(tempSpecies);
    }

    public float CalculatePhotographDistance(Spatial instancedScene)
    {
        return PhotoStudio.CameraDistanceFromRadiusOfObject(((Microbe)instancedScene).Radius *
            Constants.PHOTO_STUDIO_CELL_RADIUS_MULTIPLIER);
    }

    public Dictionary<Compound, float> CalculateTotalComposition()
    {
        var result = new Dictionary<Compound, float>();

        foreach (var organelle in Organelles)
        {
            result.Merge(organelle.Definition.InitialComposition);
        }

        return result;
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

        foreach (var organelle in Organelles)
        {
            result.Organelles.Add((OrganelleTemplate)organelle.Clone());
        }

        return result;
    }

    public override int GetHashCode()
    {
        var hash = (TypeName.GetHashCode() * 131) ^ (MPCost * 2797) ^ (MembraneType.GetHashCode() * 2801) ^
            (MembraneRigidity.GetHashCode() * 2803) ^ (Colour.GetHashCode() * 587) ^ ((IsBacteria ? 1 : 0) * 5171) ^
            (Organelles.Count * 127);

        int counter = 0;

        foreach (var organelle in Organelles)
        {
            hash ^= counter++ * 11 * organelle.GetHashCode();
        }

        return hash;
    }
}
