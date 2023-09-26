using System;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Type of a cell in a multicellular species. There can be multiple instances of a cell type placed at once
/// </summary>
[JsonObject(IsReference = true)]
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
        CanEngulf = microbeSpecies.CanEngulf;
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
    public bool CanEngulf { get; }

    /// <summary>
    ///   Total mass of all the organelles in this cell type
    /// </summary>
    [JsonIgnore]
    public float TotalMass => Organelles.Sum(o => o.Definition.Mass);

    [JsonIgnore]
    public string FormattedName => TypeName;

    [JsonIgnore]
    public string SceneToPhotographPath => "res://src/microbe_stage/Microbe.tscn";

    public void UpdatePositionalProperties()
    {
        Organelles.RepositionToOrigin();
        BaseRotationSpeed = MicrobeInternalCalculations.CalculateRotationSpeed(Organelles);
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
            if (organelle.Definition.HasComponentFactory<AxonComponentFactory>())
            {
                return true;
            }
        }

        return false;
    }

    public void ApplySceneParameters(Spatial instancedScene)
    {
        new MicrobeSpecies(new MicrobeSpecies(int.MaxValue, string.Empty, string.Empty), this)
            .ApplySceneParameters(instancedScene);
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

    private void RepositionToOrigin()
    {
        Organelles.RepositionToOrigin();
    }

    private void CalculateRotationSpeed()
    {
        BaseRotationSpeed = MicrobeInternalCalculations.CalculateRotationSpeed(Organelles);
    }
}
