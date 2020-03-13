using System;

/// <summary>
///   Represents a microbial species with microbe stage specific species things.
/// </summary>
public class MicrobeSpecies : Species
{
    public bool IsBacteria = false;
    public MembraneType MembraneType;
    public float MembraneRigidity;

    public MicrobeSpecies(uint id)
        : base(id)
    {
    }

    public OrganelleLayout Organelles { get; set; }
}
