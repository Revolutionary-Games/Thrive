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
        Organelles = new OrganelleLayout<OrganelleTemplate>();
    }

    public OrganelleLayout<OrganelleTemplate> Organelles { get; set; }

    [JsonIgnore]
    public override string StringCode
    {
        get
        {
            return ToString();
        }
        set
        {
            // TODO: allow replacing Organelles from value
            throw new NotImplementedException();
        }
    }

    public override string ToString()
    {
        // TODO: custom serializer to store the membrane type by name
        return JsonConvert.SerializeObject(this);
    }
}
