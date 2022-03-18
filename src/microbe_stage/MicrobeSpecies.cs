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
public class MicrobeSpecies : Species, ICellProperties
{
    public MicrobeSpecies(uint id, string genus, string epithet) : base(id, genus, epithet)
    {
        Organelles = new OrganelleLayout<OrganelleTemplate>();
    }

    /// <summary>
    ///   Creates a wrapper around a cell properties object for use with auto-evo predictions
    /// </summary>
    /// <param name="cloneOf">Grabs the ID and species name from here</param>
    /// <param name="withCellProperties">Properties from here are copied to this (except organelle objects are shared)</param>
    public MicrobeSpecies(Species cloneOf, ICellProperties withCellProperties) : this(cloneOf.ID, cloneOf.Genus,
        cloneOf.Epithet)
    {
        cloneOf.ClonePropertiesTo(this);

        foreach (var organelle in withCellProperties.Organelles)
        {
            Organelles.Add(organelle);
        }

        MembraneType = withCellProperties.MembraneType;
        MembraneRigidity = withCellProperties.MembraneRigidity;
        Colour = withCellProperties.Colour;
        IsBacteria = withCellProperties.IsBacteria;
    }

    public bool IsBacteria { get; set; }

    /// <summary>
    ///   Needs to be set before using this class
    /// </summary>
    public MembraneType MembraneType { get; set; } = null!;

    public float MembraneRigidity { get; set; }

    public OrganelleLayout<OrganelleTemplate> Organelles { get; set; }

    [JsonIgnore]
    public override string StringCode => ThriveJsonConverter.Instance.SerializeObject(this);

    [JsonIgnore]
    public float BaseSpeed => MicrobeInternalCalculations.CalculateSpeed(Organelles, MembraneType, MembraneRigidity);

    /// <summary>
    ///   This is the base size of this species. Meaning that this is the engulf size of microbes of this species when
    ///   they haven't duplicated any organelles. This is related to <see cref="Microbe.EngulfSize"/> and the math
    ///   should always match between these two.
    /// </summary>
    [JsonIgnore]
    public float BaseHexSize => Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount)
        * (IsBacteria ? 0.5f : 1.0f);

    public override void RepositionToOrigin()
    {
        var centerOfMass = Organelles.CenterOfMass;

        foreach (var organelle in Organelles)
        {
            // This calculation aligns the center of mass with the origin by moving every organelle of the microbe.
            organelle.Position -= centerOfMass;
        }
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

    public override void UpdateInitialCompounds()
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
        var result = new MicrobeSpecies(ID, Genus, Epithet);

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
