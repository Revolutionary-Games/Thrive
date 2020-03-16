using System;
using Newtonsoft.Json;

/// <summary>
///   Represents a microbial species with microbe stage specific species things.
/// </summary>
public class MicrobeSpecies : Species
{
    public bool IsBacteria = false;
    public MembraneType MembraneType;
    public float MembraneRigidity = 1.0f;

    public MicrobeSpecies(uint id)
        : base(id)
    {
        Organelles = new OrganelleLayout();
    }

    [JsonIgnore]
    public OrganelleLayout Organelles { get; set; }
}
