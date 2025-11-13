/// <summary>
///   Readonly species with microbe properties on top
/// </summary>
public interface IReadOnlyMicrobeSpecies : IReadOnlySpecies
{
    public bool IsBacteria { get; }

    public MembraneType MembraneType { get; }

    public float MembraneRigidity { get; }

    public OrganelleLayout<OrganelleTemplate> Organelles { get; }
}
