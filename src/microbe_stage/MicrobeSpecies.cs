﻿using System;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Represents a microbial species with microbe stage specific species things.
/// </summary>
[JsonObject(IsReference = true)]
[TypeConverter(typeof(ThriveTypeConverter))]
[JSONDynamicTypeAllowedAttribute]
[UseThriveConverter]
public class MicrobeSpecies : Species
{
    public bool IsBacteria;
    public MembraneType MembraneType;
    public float MembraneRigidity;

    public MicrobeSpecies(uint id)
        : base(id)
    {
        Organelles = new OrganelleLayout<OrganelleTemplate>();
    }

    public OrganelleLayout<OrganelleTemplate> Organelles { get; set; }

    [JsonIgnore]
    public override string StringCode
    {
        get => ThriveJsonConverter.Instance.SerializeObject(this);

        // TODO: allow replacing Organelles from value
        set => throw new NotImplementedException();
    }

    public void SetInitialCompoundsForDefault()
    {
        InitialCompounds.Clear();
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("atp"), 30);
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("glucose"), 10);
    }

    public void SetInitialCompoundsForIron()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("iron"), 10);
    }

    public void SetInitialCompoundsForChemo()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("hydrogensulfide"), 10);
    }

    public void UpdateInitialCompounds()
    {
        var simulation = SimulationParameters.Instance;

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

        Organelles.Clear();

        foreach (var organelle in casted.Organelles)
        {
            Organelles.Add((OrganelleTemplate)organelle.Clone());
        }

        IsBacteria = casted.IsBacteria;
        MembraneType = casted.MembraneType;
        MembraneRigidity = casted.MembraneRigidity;
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
