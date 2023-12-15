using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Represents a late multicellular species that is 3D and composed of placed tissues
/// </summary>
[JsonObject(IsReference = true)]
[TypeConverter(typeof(ThriveTypeConverter))]
[JSONDynamicTypeAllowed]
[UseThriveConverter]
[UseThriveSerializer]
public class LateMulticellularSpecies : Species
{
    public LateMulticellularSpecies(uint id, string genus, string epithet) : base(id, genus, epithet)
    {
    }

    [JsonProperty]
    public MulticellularMetaballLayout BodyLayout { get; private set; } = new();

    [JsonProperty]
    public List<CellType> CellTypes { get; private set; } = new();

    /// <summary>
    ///   The scale in meters of the species
    /// </summary>
    public float Scale { get; set; } = 1.0f;

    [JsonProperty]
    public float BrainPower { get; private set; }

    /// <summary>
    ///   Where this species reproduces, used to control also where individuals of this species spawn and where the
    ///   player spawns
    /// </summary>
    [JsonProperty]
    public ReproductionLocation ReproductionLocation { get; set; }

    [JsonProperty]
    public MulticellularSpeciesType MulticellularType { get; private set; }

    /// <summary>
    ///   All organelles in all of the species' placed metaballs (there can be a lot of duplicates in this list)
    /// </summary>
    [JsonIgnore]
    public IEnumerable<OrganelleTemplate> Organelles =>
        BodyLayout.Select(m => m.CellType).Distinct().SelectMany(c => c.Organelles);

    [JsonIgnore]
    public override string StringCode => ThriveJsonConverter.Instance.SerializeObject(this);

    public static MulticellularSpeciesType CalculateMulticellularTypeFromLayout(
        MetaballLayout<MulticellularMetaball> layout, float scale)
    {
        var brainPower = CalculateBrainPowerFromLayout(layout, scale);

        if (brainPower >= Constants.BRAIN_POWER_REQUIRED_FOR_AWAKENING)
        {
            return MulticellularSpeciesType.Awakened;
        }

        if (brainPower >= Constants.BRAIN_POWER_REQUIRED_FOR_AWARE)
        {
            return MulticellularSpeciesType.Aware;
        }

        return MulticellularSpeciesType.LateMulticellular;
    }

    public override void OnEdited()
    {
        base.OnEdited();

        RepositionToOrigin();
        UpdateInitialCompounds();
        CalculateBrainPower();

        // Note that a few stage transitions are explicit for the player so the editor will override this
        SetTypeFromBrainPower();
    }

    public override bool RepositionToOrigin()
    {
        return BodyLayout.RepositionToGround();
    }

    public override void UpdateInitialCompounds()
    {
        // TODO: change this to be dynamic similar to microbe stage

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

        var casted = (LateMulticellularSpecies)mutation;

        CellTypes.Clear();

        foreach (var cellType in casted.CellTypes)
        {
            CellTypes.Add((CellType)cellType.Clone());
        }

        BodyLayout.Clear();

        var metaballMapping = new Dictionary<Metaball, MulticellularMetaball>();

        // Make sure we process things with parents first
        // TODO: if the tree depth calculation is too expensive here, we'll need to cache the values in the metaball
        // objects
        foreach (var metaball in casted.BodyLayout.OrderBy(m => m.CalculateTreeDepth()))
        {
            BodyLayout.Add(metaball.Clone(metaballMapping));
        }
    }

    /// <summary>
    ///   Explicitly moves the player to awakened status, this is like this to make sure the player wouldn't get stuck
    ///   underwater if they accidentally increased their brain power
    /// </summary>
    public void MovePlayerToAwakenedStatus()
    {
        MulticellularType = MulticellularSpeciesType.Awakened;
    }

    public void KeepPlayerInAwareStage()
    {
        if (MulticellularType == MulticellularSpeciesType.Awakened)
            MulticellularType = MulticellularSpeciesType.Aware;
    }

    public override object Clone()
    {
        var result = new LateMulticellularSpecies(ID, Genus, Epithet);

        ClonePropertiesTo(result);

        foreach (var cellType in CellTypes)
        {
            result.CellTypes.Add((CellType)cellType.Clone());
        }

        var metaballMapping = new Dictionary<Metaball, MulticellularMetaball>();

        foreach (var metaball in BodyLayout)
        {
            result.BodyLayout.Add(metaball.Clone(metaballMapping));
        }

        return result;
    }

    private static float CalculateBrainPowerFromLayout(MetaballLayout<MulticellularMetaball> layout, float scale)
    {
        float result = 0;

        foreach (var metaball in layout)
        {
            if (metaball.CellType.IsBrainTissueType())
            {
                // TODO: check that volume scaling in physically sensible way (using GetVolume) is what we want here
                // Maybe we would actually just want to multiply by the scale number to buff small species' brain?
                result += metaball.GetVolume(scale);
            }
        }

        return result;
    }

    private void SetTypeFromBrainPower()
    {
        MulticellularType = CalculateMulticellularTypeFromLayout(BodyLayout, Scale);
    }

    private void SetInitialCompoundsForDefault()
    {
        InitialCompounds.Clear();

        // TODO: modify these numbers based on the scale and metaball count or something more accurate
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("atp"), 180);
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("glucose"), 90);
    }

    private void SetInitialCompoundsForIron()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("iron"), 90);
    }

    private void SetInitialCompoundsForChemo()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("hydrogensulfide"), 90);
    }

    private void CalculateBrainPower()
    {
        BrainPower = CalculateBrainPowerFromLayout(BodyLayout, Scale);
    }
}
