using System;
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
    }

    [JsonProperty]
    public OrganelleLayout<OrganelleTemplate> Organelles { get; private set; }

    public string TypeName { get; set; } = "Stem";
    public int MPCost { get; set; } = 25;

    public MembraneType MembraneType { get; set; }
    public float MembraneRigidity { get; set; }
    public Color Colour { get; set; }
    public bool IsBacteria { get; set; }

    [JsonIgnore]
    public string SceneToPhotographPath => "res://src/microbe_stage/Microbe.tscn";

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
}
