using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Represents an early multicellular species that is composed of multiple cells
/// </summary>
[JsonObject(IsReference = true)]
[TypeConverter(typeof(ThriveTypeConverter))]
[JSONDynamicTypeAllowedAttribute]
[UseThriveConverter]
public class EarlyMulticellularSpecies : Species
{
    public EarlyMulticellularSpecies(uint id, string genus, string epithet) : base(id, genus, epithet)
    {
        Cells = new CellLayout<CellTemplate>();
    }

    public CellLayout<CellTemplate> Cells { get; set; }

    [JsonProperty]
    public List<CellType> CellTypes { get; private set; } = new();

    /// <summary>
    ///   All organelles in all of the species' placed cells (there can be a lot of duplicates in this list)
    /// </summary>
    [JsonIgnore]
    public IEnumerable<OrganelleTemplate> Organelles => Cells.SelectMany(c => c.Organelles);

    [JsonIgnore]
    public override string StringCode => ThriveJsonConverter.Instance.SerializeObject(this);

    public override void RepositionToOrigin()
    {
        var centerOfMass = Cells.CenterOfMass;

        foreach (var organelle in Cells)
        {
            // This calculation aligns the center of mass with the origin by moving every organelle of the microbe.
            organelle.Position -= centerOfMass;
        }
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

        var casted = (EarlyMulticellularSpecies)mutation;

        Cells.Clear();

        foreach (var cellTemplate in casted.Cells)
        {
            Cells.Add((CellTemplate)cellTemplate.Clone());
        }
    }

    public override object Clone()
    {
        var result = new EarlyMulticellularSpecies(ID, Genus, Epithet);

        ClonePropertiesTo(result);

        foreach (var cellTemplate in Cells)
        {
            result.Cells.Add((CellTemplate)cellTemplate.Clone());
        }

        return result;
    }

    private void SetInitialCompoundsForDefault()
    {
        InitialCompounds.Clear();

        // TODO: modify these numbers based on the cell count or something more accurate
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("atp"), 60);
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("glucose"), 30);
    }

    private void SetInitialCompoundsForIron()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("iron"), 30);
    }

    private void SetInitialCompoundsForChemo()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("hydrogensulfide"), 30);
    }
}
