﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Saving.Serializers;
using Systems;
using Vector3 = Godot.Vector3;

/// <summary>
///   Represents a microbial species with microbe stage specific species things.
/// </summary>
[JsonObject(IsReference = true)]
[TypeConverter($"Saving.Serializers.{nameof(ThriveTypeConverter)}")]
[JSONDynamicTypeAllowed]
[UseThriveConverter]
[UseThriveSerializer]
public class MicrobeSpecies : Species, ICellDefinition
{
    private readonly Dictionary<BiomeConditions, Dictionary<Compound, (float TimeToFill, float Storage)>>
        cachedFillTimes = new();

    [JsonConstructor]
    public MicrobeSpecies(uint id, string genus, string epithet) : base(id, genus, epithet)
    {
        Organelles = new OrganelleLayout<OrganelleTemplate>();
    }

    /// <summary>
    ///   Creates a wrapper around a cell properties object for use with auto-evo predictions
    /// </summary>
    /// <param name="cloneOf">Grabs the ID and species name from here</param>
    /// <param name="withCellDefinition">
    ///   Properties from here are copied to this (except organelle objects are shared)
    /// </param>
    /// <param name="workMemory1">Temporary memory needed to copy the organelles</param>
    /// <param name="workMemory2">More needed temporary memory</param>
    public MicrobeSpecies(Species cloneOf, ICellDefinition withCellDefinition, List<Hex> workMemory1,
        List<Hex> workMemory2) : this(cloneOf.ID, cloneOf.Genus, cloneOf.Epithet)
    {
        cloneOf.ClonePropertiesTo(this);

        foreach (var organelle in withCellDefinition.Organelles)
        {
            Organelles.AddFast(organelle, workMemory1, workMemory2);
        }

        MembraneType = withCellDefinition.MembraneType;
        MembraneRigidity = withCellDefinition.MembraneRigidity;
        Colour = withCellDefinition.Colour;
        IsBacteria = withCellDefinition.IsBacteria;
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
    public float BaseHexSize
    {
        get
        {
            var raw = 0.0f;

            // Need to do the calculation this way to avoid extra memory allocations
            var organelles = Organelles.Organelles;
            int count = organelles.Count;
            for (int i = 0; i < count; ++i)
            {
                raw += organelles[i].Definition.HexCount;
            }

            if (IsBacteria)
                return raw * 0.5f;

            return raw;
        }
    }

    /// <summary>
    ///   TODO: this should be removed as this is not accurate (only accurate if specialized storage vacuoles aren't
    ///   used)
    /// </summary>
    [JsonIgnore]
    public float StorageCapacity => MicrobeInternalCalculations.CalculateCapacity(Organelles);

    /// <summary>
    ///   Compound capacities members of this species can store in their default configurations
    /// </summary>
    [JsonIgnore]
    public (float Nominal, Dictionary<Compound, float> Specific) StorageCapacities
    {
        get
        {
            var specific = MicrobeInternalCalculations.GetTotalSpecificCapacity(Organelles, out var nominal);
            return (nominal, specific);
        }
    }

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

        cachedFillTimes.Clear();
    }

    public override bool RepositionToOrigin()
    {
        var changes = Organelles.RepositionToOrigin();
        CalculateRotationSpeed();
        return changes;
    }

    public override void UpdateInitialCompounds()
    {
        // Since the initial compounds are only set once per species they can't be calculated for each Biome.
        // So, the compound balance calculation uses the default biome.
        // Also, we should not overtly punish photosynthesizers, so we just use the consumption here (instead of
        // balance where the generated glucose would offset things and spawn photosynthesizers with no glucose,
        // which could basically make them die instantly in certain situations)
        var simulationParameters = SimulationParameters.Instance;

        // TODO: improve this depending on a hardcoded patch: https://github.com/Revolutionary-Games/Thrive/issues/5446
        var biomeConditions = simulationParameters.GetBiome("speciesInitialCompoundsBiome").Conditions;

        // False is passed here until we can make the initial compounds patch specific
        var compoundBalances = ProcessSystem.ComputeCompoundBalance(Organelles,
            biomeConditions, CompoundAmountType.Biome, false);

        bool giveBonusGlucose = Organelles.Count <= Constants.FULL_INITIAL_GLUCOSE_SMALL_SIZE_LIMIT && IsBacteria;

        var cachedCapacity = StorageCapacity;

        InitialCompounds.Clear();

        foreach (var compoundBalance in compoundBalances)
        {
            // Skip ATP as we don't want to give any initial ATP
            if (compoundBalance.Key == Compound.ATP)
                continue;

            if (compoundBalance.Key == Compound.Glucose && giveBonusGlucose)
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

    public override void HandleNightSpawnCompounds(CompoundBag targetStorage, ISpawnEnvironmentInfo spawnEnvironment)
    {
        if (spawnEnvironment is not IMicrobeSpawnEnvironment microbeSpawnEnvironment)
            throw new ArgumentException("Microbes must have microbe spawn environment info");

        // TODO: cache the data
        var biome = microbeSpawnEnvironment.CurrentBiome;

        Dictionary<Compound, (float TimeToFill, float Storage)>? compoundTimes;

        // This lock is here to allow multiple microbe spawns to happen in parallel. Lock is not used on clear as no
        // spawns should be allowed to happen while species are being modified
        lock (cachedFillTimes)
        {
            if (!cachedFillTimes.TryGetValue(biome, out compoundTimes))
            {
                // TODO: should moving be false in some cases?
                compoundTimes = MicrobeInternalCalculations.CalculateDayVaryingCompoundsFillTimes(Organelles,
                    MembraneType, true, PlayerSpecies, biome, spawnEnvironment.WorldSettings);
                cachedFillTimes[biome] = compoundTimes;
            }
        }

        MicrobeInternalCalculations.GiveNearNightInitialCompoundBuff(targetStorage, compoundTimes,
            spawnEnvironment.DaylightInfo);
    }

    public override void ApplyMutation(Species mutation)
    {
        base.ApplyMutation(mutation);

        var casted = (MicrobeSpecies)mutation;

        Organelles.Clear();

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        foreach (var organelle in casted.Organelles)
        {
            Organelles.AddFast((OrganelleTemplate)organelle.Clone(), workMemory1, workMemory2);
        }

        IsBacteria = casted.IsBacteria;
        MembraneType = casted.MembraneType;
        MembraneRigidity = casted.MembraneRigidity;

        cachedFillTimes.Clear();
    }

    public override float GetPredationTargetSizeFactor()
    {
        return Organelles.Count;
    }

    public Vector3 CalculatePhotographDistance(IWorldSimulation worldSimulation)
    {
        return GeneralCellPropertiesHelpers.CalculatePhotographDistance(worldSimulation);
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
        return Clone(true);
    }

    public MicrobeSpecies Clone(bool cloneOrganelles)
    {
        var result = new MicrobeSpecies(ID, Genus, Epithet);

        ClonePropertiesTo(result);

        result.IsBacteria = IsBacteria;
        result.MembraneType = MembraneType;
        result.MembraneRigidity = MembraneRigidity;

        if (cloneOrganelles)
        {
            result.Organelles = Organelles.Clone();
        }

        return result;
    }

    public override ulong GetVisualHashCode()
    {
        var hash = base.GetVisualHashCode();

        // This code also exists in CellType visual calculation
        var count = Organelles.Count;

        hash ^= PersistentStringHash.GetHash(MembraneType.InternalName) * 5743 ^
            (ulong)MembraneRigidity.GetHashCode() * 5749 ^
            (IsBacteria ? 1UL : 0UL) * 5779UL ^ (ulong)count * 131;

        var list = Organelles.Organelles;

        for (int i = 0; i < count; ++i)
        {
            // Organelles in different order don't matter (in terms of visuals) so we don't apply any loop specific
            // stuff here
            unchecked
            {
                hash += list[i].GetVisualHashCode() * 13;
            }
        }

        return hash ^ Constants.VISUAL_HASH_CELL;
    }

    public override string GetDetailString()
    {
        return base.GetDetailString() + "\n" +
            Localization.Translate("MICROBE_SPECIES_DETAIL_TEXT").FormatSafe(MembraneType.Name,
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
