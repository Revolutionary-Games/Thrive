using System;
using System.Linq;
using Godot;
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

    public void SetInitialCompoundsForDefault()
    {
        InitialCompounds.Clear();
        InitialCompounds.Add("atp", 30);
        InitialCompounds.Add("glucose", 10);
    }

    public void SetInitialCompoundsForIron()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add("iron", 10);
    }

    public void SetInitialCompoundsForChemo()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add("hydrogensulfide", 10);
    }

    public void SetUpdatedCompounds()
    {
        var simulation = SimulationParameters.Instance;

        // If you have iron (f is the symbol for rusticyanin)
        var rusticyanin = simulation.GetOrganelleType("rusticyanin");
        var chemo = simulation.GetOrganelleType("chemoplast");
        var chemoProtein = simulation.GetOrganelleType("chemoSynthesizingProteins");

        if (Organelles.Any(o => o.Definition == rusticyanin))
        {
            SetInitialCompoundsForIron();
        }
        else if (Organelles.Any(o => o.Definition == chemo ||
            o.Definition == chemoProtein))
        {
            SetInitialCompoundsForChemo();
        }
        else
        {
            SetInitialCompoundsForDefault();
        }
    }

    public override void ApplyMutation(Species mutation)
    {
        base.ApplyMutation(mutation);

        var casted = (MicrobeSpecies)mutation;

        Organelles.RemoveAll();

        foreach (var organelle in casted.Organelles)
        {
            Organelles.Add((OrganelleTemplate)organelle.Clone());
        }

        IsBacteria = casted.IsBacteria;
        MembraneType = casted.MembraneType;
        MembraneRigidity = casted.MembraneRigidity;
    }

    public override string ToString()
    {
        // TODO: custom serializer to store the membrane type by name
        return JsonConvert.SerializeObject(this);
    }

    public override object Clone()
    {
        var result = new MicrobeSpecies(ID);

        ClonePropertiesTo(result);

        result.IsBacteria = IsBacteria;
        result.MembraneType = MembraneType;
        result.MembraneRigidity = MembraneRigidity;

        foreach (var organelle in Organelles)
        {
            result.Organelles.Add((OrganelleTemplate)organelle.Clone());
        }

        return result;
    }
}
