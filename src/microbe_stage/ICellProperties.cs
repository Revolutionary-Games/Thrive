using Godot;

/// <summary>
///   Generic interface to allow working with microbe species and also multicellular species' individual cell types
/// </summary>
public interface ICellProperties
{
    public OrganelleLayout<OrganelleTemplate> Organelles { get; }
    public MembraneType MembraneType { get; set; }
    public float MembraneRigidity { get; set; }
    public Color Colour { get; set; }
    public bool IsBacteria { get; set; }

    public string FormattedName { get; }

    public void RepositionToOrigin();
    public void UpdateNameIfValid(string newName);
}
