using System;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   Represents a microbial species with microbe stage specific species things.
/// </summary>
[JsonObject(IsReference = true)]
[TypeConverter(typeof(ThriveTypeConverter))]
[JSONDynamicTypeAllowed]
[UseThriveConverter]
[UseThriveSerializer]
public class MicrobeSpecies : Species, ICellProperties
{
    [JsonConstructor]
    public MicrobeSpecies(uint id, string genus, string epithet) : base(id, genus, epithet)
    {
        Organelles = new OrganelleLayout<OrganelleTemplate>();
    }

    /// <summary>
    ///   Creates a wrapper around a cell properties object for use with auto-evo predictions
    /// </summary>
    /// <param name="cloneOf">Grabs the ID and species name from here</param>
    /// <param name="withCellProperties">
    ///   Properties from here are copied to this (except organelle objects are shared)
    /// </param>
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

    /// <summary>
    ///   Organelles this species consist of. This is saved last to ensure organelle data that may refer back to this
    ///   species can be loaded (for example cell-detecting chemoreceptors).
    /// </summary>
    [JsonProperty(Order = 1)]
    public OrganelleLayout<OrganelleTemplate> Organelles { get; set; }

    [JsonIgnore]
    public override string StringCode => ThriveJsonConverter.Instance.SerializeObject(this);

    // Even though these properties say "base" it includes the specialized organelle factors. Base refers here to
    // the fact that these are the values when a cell is freshly spawned and has no reproduction progress.
    [JsonIgnore]
    public float BaseSpeed =>
        MicrobeInternalCalculations.CalculateSpeed(Organelles.Organelles, MembraneType, MembraneRigidity, IsBacteria);

    [JsonProperty]
    public float BaseRotationSpeed { get; set; }

    /// <summary>
    ///   This is the base size of this species. Meaning that this is the engulf size of microbes of this species when
    ///   they haven't duplicated any organelles. This is related to <see cref="Components.Engulfer.EngulfingSize"/>
    ///   (as well as the size this takes up as an <see cref="Components.Engulfable"/>) and the math should always
    ///   match between these two.
    /// </summary>
    [JsonIgnore]
    public float BaseHexSize => Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount)
        * (IsBacteria ? 0.5f : 1.0f);

    [JsonIgnore]
    public float StorageCapacity => MicrobeInternalCalculations.CalculateCapacity(Organelles);

    [JsonIgnore]
    public bool CanEngulf => !MembraneType.CellWall;

    [JsonIgnore]
    public ISimulationPhotographable.SimulationType SimulationToPhotograph =>
        ISimulationPhotographable.SimulationType.MicrobeGraphics;

    public static bool StateHasStabilizedImpl(IWorldSimulation worldSimulation)
    {
        // This is stabilized as long as the default no background operations check passes
        // If this is changed CellType also needs changes
        return true;
    }

    public override void OnEdited()
    {
        base.OnEdited();

        RepositionToOrigin();
        UpdateInitialCompounds();
    }

    public override void RepositionToOrigin()
    {
        Organelles.RepositionToOrigin();
        CalculateRotationSpeed();
    }

    public override void UpdateInitialCompounds()
    {
        // Since the initial compounds are only set once per species they can't be calculated for each Biome.
        // So, the compound balance calculation uses the default biome.
        // Also we should not overtly punish photosynthesizers so we just use the consumption here (instead of
        // balance where the generated glucose would offset things and spawn photosynthesizers with no glucose,
        // which could basically make them die instantly in certain situations)
        var biomeConditions = SimulationParameters.Instance.GetBiome("default").Conditions;
        var compoundBalances = ProcessSystem.ComputeCompoundBalance(Organelles,
            biomeConditions, CompoundAmountType.Biome);

        var glucose = SimulationParameters.Instance.GetCompound("glucose");
        var atp = SimulationParameters.Instance.GetCompound("atp");
        bool giveBonusGlucose = Organelles.Count <= Constants.FULL_INITIAL_GLUCOSE_SMALL_SIZE_LIMIT && IsBacteria;

        var cachedCapacity = StorageCapacity;

        InitialCompounds.Clear();

        foreach (var compoundBalance in compoundBalances)
        {
            // Skip ATP as it we don't want to give any initial ATP
            if (compoundBalance.Key == atp)
                continue;

            if (compoundBalance.Key == glucose && giveBonusGlucose)
            {
                InitialCompounds.Add(compoundBalance.Key, cachedCapacity);
                continue;
            }

            var balanceValue = compoundBalance.Value;

            // Skip compounds there's no consumption for (from processes)
            if (balanceValue.Consumption.Count < 1)
                continue;

            // Initial compounds should suffice for a fixed amount of time of consumption.
            var compoundInitialAmount =
                Math.Abs(balanceValue.Consumption.SumValues()) * Constants.INITIAL_COMPOUND_TIME;

            if (compoundInitialAmount > cachedCapacity)
                compoundInitialAmount = cachedCapacity;

            InitialCompounds.Add(compoundBalance.Key, compoundInitialAmount);
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

    public Vector3 CalculatePhotographDistance(IWorldSimulation worldSimulation)
    {
        return CellPropertiesHelpers.CalculatePhotographDistance(worldSimulation);
    }

    public void SetupWorldEntities(IWorldSimulation worldSimulation)
    {
        ((MicrobeVisualOnlySimulation)worldSimulation).CreateVisualisationMicrobe(this);
    }

    public bool StateHasStabilized(IWorldSimulation worldSimulation)
    {
        return StateHasStabilizedImpl(worldSimulation);
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

    public override int GetVisualHashCode()
    {
        var hash = base.GetVisualHashCode();

        hash ^= (MembraneType.GetHashCode() * 5743) ^ (MembraneRigidity.GetHashCode() * 5749) ^
            ((IsBacteria ? 1 : 0) * 5779) ^
            (Organelles.Count * 131);

        int counter = 0;

        foreach (var organelle in Organelles)
        {
            hash ^= counter++ * 13 * organelle.GetHashCode();
        }

        return hash;
    }

    public override string GetDetailString()
    {
        return base.GetDetailString() + "\n" +
            TranslationServer.Translate("MICROBE_SPECIES_DETAIL_TEXT").FormatSafe(
                MembraneType.Name,
                MembraneRigidity,
                BaseSpeed,
                BaseRotationSpeed,
                BaseHexSize);
    }

    private void CalculateRotationSpeed()
    {
        BaseRotationSpeed = MicrobeInternalCalculations.CalculateRotationSpeed(Organelles.Organelles);
    }
}
