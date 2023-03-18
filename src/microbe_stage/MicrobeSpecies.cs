using System;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Represents a microbial species with microbe stage specific species things.
/// </summary>
[JsonObject(IsReference = true)]
[TypeConverter(typeof(ThriveTypeConverter))]
[JSONDynamicTypeAllowed]
[UseThriveConverter]
[UseThriveSerializer]
public class MicrobeSpecies : Species, ICellProperties, IPhotographable
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

    public OrganelleLayout<OrganelleTemplate> Organelles { get; set; }

    [JsonIgnore]
    public override string StringCode => ThriveJsonConverter.Instance.SerializeObject(this);

    // Even though these properties say "base" it includes the specialized organelle factors. Base refers here to
    // the fact that these are the values when a cell is freshly spawned and has no reproduction progress.
    [JsonIgnore]
    public float BaseSpeed => MicrobeInternalCalculations.CalculateSpeed(Organelles, MembraneType, MembraneRigidity);

    [JsonProperty]
    public float BaseRotationSpeed { get; set; } = Constants.CELL_BASE_ROTATION;

    /// <summary>
    ///   This is the base size of this species. Meaning that this is the engulf size of microbes of this species when
    ///   they haven't duplicated any organelles. This is related to <see cref="Microbe.EngulfSize"/> and the math
    ///   should always match between these two.
    /// </summary>
    [JsonIgnore]
    public float BaseHexSize => Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount)
        * (IsBacteria ? 0.5f : 1.0f);

    [JsonIgnore]
    public float StorageCapacity => MicrobeInternalCalculations.CalculateCapacity(Organelles);

    [JsonIgnore]
    public bool CanEngulf => !MembraneType.CellWall;

    [JsonIgnore]
    public string SceneToPhotographPath => "res://src/microbe_stage/Microbe.tscn";

    public override void OnEdited()
    {
        base.OnEdited();

        RepositionToOrigin();
        UpdateInitialCompounds();
        CalculateRotationSpeed();
    }

    public override void RepositionToOrigin()
    {
        Organelles.RepositionToOrigin();
    }

    public void CalculateRotationSpeed()
    {
        BaseRotationSpeed = MicrobeInternalCalculations.CalculateRotationSpeed(Organelles);
    }

    public override void UpdateInitialCompounds()
    {
        // Since the initial compounds are only set once per species they can't be calculated for each Biome.
        // So, the compound balance calculation uses the default biome.
        var biomeConditions = SimulationParameters.Instance.GetBiome("default").Conditions;
        var compoundBalances = ProcessSystem.ComputeCompoundBalance(Organelles,
            biomeConditions, CompoundAmountType.Current);

        InitialCompounds.Clear();

        foreach (var compoundBalance in compoundBalances)
        {
            if (compoundBalance.Key == SimulationParameters.Instance.GetCompound("glucose") &&
                Organelles.Count <= 3 && IsBacteria)
            {
                InitialCompounds.Add(compoundBalance.Key, StorageCapacity);
                continue;
            }

            if (compoundBalance.Value.Balance >= 0)
                continue;

            // Initial compounds should suffice for a fixed amount of time.
            var compoundInitialAmount = Math.Abs(compoundBalance.Value.Balance) * Constants.INITIAL_COMPOUND_TIME;
            if (compoundInitialAmount > StorageCapacity)
                compoundInitialAmount = StorageCapacity;
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

    public void ApplySceneParameters(Spatial instancedScene)
    {
        var microbe = (Microbe)instancedScene;
        microbe.IsForPreviewOnly = true;

        // We need to call _Ready here as the object may not be attached to the scene yet by the photo studio
        microbe._Ready();

        microbe.ApplySpecies(this);
    }

    public float CalculatePhotographDistance(Spatial instancedScene)
    {
        return PhotoStudio.CameraDistanceFromRadiusOfObject(((Microbe)instancedScene).Radius *
            Constants.PHOTO_STUDIO_CELL_RADIUS_MULTIPLIER);
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
}
