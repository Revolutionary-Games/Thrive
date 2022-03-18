using Godot;
using Newtonsoft.Json;

/// <summary>
///   Type of a cell in a multicellular species. There can be multiple instances of a cell type placed at once
/// </summary>
public class CellType : ICellProperties
{
    public CellType(OrganelleLayout<OrganelleTemplate> organelles)
    {
        Organelles = organelles;
    }

    public CellType()
    {
        Organelles = new OrganelleLayout<OrganelleTemplate>();
    }

    /// <summary>
    ///   Creates a cell type from the cell type of a microbe species
    /// </summary>
    /// <param name="microbeSpecies">The microbe species to take the cell type parameters from</param>
    public CellType(MicrobeSpecies microbeSpecies) : this()
    {
        foreach (var organelle in microbeSpecies.Organelles)
        {
            Organelles.Add((OrganelleTemplate)organelle.Clone());
        }

        // TODO: copy membrane properties
    }

    [JsonProperty]
    public OrganelleLayout<OrganelleTemplate> Organelles { get; private set; }

    public MembraneType MembraneType { get; set; }
    public float MembraneRigidity { get; set; }
    public Color Colour { get; set; }
    public bool IsBacteria { get; set; }
}
