using System;
using System.Collections.Generic;
using Godot;
using SharedBase.Archive;
using Systems;
using Vector3 = Godot.Vector3;

/// <summary>
///   Represents a microbial species with microbe stage specific species things.
/// </summary>
public class MicrobeSpecies : Species, IReadOnlyMicrobeSpecies, ICellDefinition
{
    public const ushort SERIALIZATION_VERSION = 2;

    private readonly Dictionary<BiomeConditions, Dictionary<Compound, (float TimeToFill, float Storage)>>
        cachedFillTimes = new();

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
    /// <param name="workMemory2">More necessary temporary memory</param>
    public MicrobeSpecies(Species cloneOf, ICellDefinition withCellDefinition, List<Hex> workMemory1,
        List<Hex> workMemory2) : this(cloneOf.ID, cloneOf.Genus, cloneOf.Epithet)
    {
        cloneOf.ClonePropertiesTo(this);

        foreach (var organelle in withCellDefinition.ModifiableOrganelles)
        {
            Organelles.AddFast(organelle, workMemory1, workMemory2);
        }

        MembraneType = withCellDefinition.MembraneType;
        MembraneRigidity = withCellDefinition.MembraneRigidity;
        SpeciesColour = withCellDefinition.Colour;
        IsBacteria = withCellDefinition.IsBacteria;
    }

    public bool IsBacteria { get; set; }

    /// <summary>
    ///   Needs to be set before using this class
    /// </summary>
    public MembraneType MembraneType { get; set; } = null!;

    public float MembraneRigidity { get; set; }

    // TODO: switch this primary to be the readonly organelles interface
    /// <summary>
    ///   Organelles of this species. This is saved (almost) last to ensure organelle data that may refer back to this
    ///   species can be loaded (for example, cell-detecting chemoreceptors).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Do not change this once the object is in use as the readonly adapter will not have been updated.
    ///   </para>
    /// </remarks>
    public OrganelleLayout<OrganelleTemplate> Organelles { get; private set; }

    public OrganelleLayout<OrganelleTemplate> ModifiableOrganelles => Organelles;

    public Color Colour
    {
        get => SpeciesColour;
        set => SpeciesColour = value;
    }

    // Even though these properties say "base", it includes the specialized organelle factors.
    // Base refers here to the fact that these are the values when a cell is freshly spawned and has no
    // reproduction progress.
    public float BaseSpeed =>
        MicrobeInternalCalculations.CalculateSpeed(Organelles.Organelles, MembraneType, MembraneRigidity, IsBacteria);

    public float BaseRotationSpeed { get; set; }

    /// <summary>
    ///   This is the base size of this species. Meaning that this is the engulf size of microbes of this species when
    ///   they haven't duplicated any organelles. This is related to <see cref="Components.Engulfer.EngulfingSize"/>
    ///   (as well as the size this takes up as an <see cref="Components.Engulfable"/>) and the math should always
    ///   match between these two.
    /// </summary>
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

    // TODO: precalculate this as it'll help auto-evo quite a bit
    /// <summary>
    ///   Compound capacities members of this species can store in their default configurations
    /// </summary>
    public (float Nominal, Dictionary<Compound, float> Specific) StorageCapacities
    {
        get
        {
            var specific = MicrobeInternalCalculations.GetTotalSpecificCapacity(Organelles, out var nominal);
            return (nominal, specific);
        }
    }

    public bool CanEngulf => !MembraneType.CellWall;

    public ISimulationPhotographable.SimulationType SimulationToPhotograph =>
        ISimulationPhotographable.SimulationType.MicrobeGraphics;

    /// <summary>
    ///   Not used for full microbes
    /// </summary>
    public int MPCost => -1;

    public string CellTypeName => FormattedName;

    /// <summary>
    ///   Microbes are never split from any cell type
    /// </summary>
    public string? SplitFromTypeName => null;

    /// <summary>
    ///   Cached specialization bonus for this species.
    /// </summary>
    public float SpecializationBonus { get; set; }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public override ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.MicrobeSpecies;

    // TODO: sadly I found no way to finagle the interfaces to line up fully, so a very light adapter class is needed
    // here
    IReadOnlyOrganelleLayout<IReadOnlyOrganelleTemplate> IReadOnlyCellDefinition.Organelles =>
        field ??=
            new ReadonlyOrganelleLayoutAdapter<IReadOnlyOrganelleTemplate, OrganelleTemplate>(Organelles);

    public static bool StateHasStabilizedImpl(IWorldSimulation worldSimulation)
    {
        // This is stabilised as long as the default no background operations check passes.
        // If this is changed, CellType also needs changes.
        return true;
    }

    public static MicrobeSpecies ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new MicrobeSpecies(reader.ReadUInt32(),
            reader.ReadString() ?? throw new NullArchiveObjectException(),
            reader.ReadString() ?? throw new NullArchiveObjectException());

        reader.ReportObjectConstructorDone(instance, referenceId);

        instance.ReadNonConstructorBaseProperties(reader, 1);

        instance.IsBacteria = reader.ReadBool();
        instance.MembraneType = reader.ReadObject<MembraneType>();
        instance.MembraneRigidity = reader.ReadFloat();
        instance.Organelles = reader.ReadObject<OrganelleLayout<OrganelleTemplate>>();
        instance.BaseRotationSpeed = reader.ReadFloat();

        if (version > 1)
        {
            instance.SpecializationBonus = reader.ReadFloat();
        }
        else
        {
            // Assume older microbes won't have specialization for now. And the next editor / auto-evo cycle can sort
            // them out.
            instance.SpecializationBonus = 1;
        }

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        WriteBasePropertiesToArchive(writer);

        writer.Write(IsBacteria);
        writer.WriteObject(MembraneType);
        writer.Write(MembraneRigidity);

        writer.WriteObject(Organelles);
        writer.Write(BaseRotationSpeed);
        writer.Write(SpecializationBonus);
    }

    public void UpdateIsBacteria()
    {
        var nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");
        IsBacteria = true;
        var organelles = Organelles.Organelles;
        var count = organelles.Count;

        for (int i = 0; i < count; ++i)
        {
            var organelle = organelles[i];
            if (organelle.Definition == nucleus)
            {
                IsBacteria = false;
                break;
            }
        }
    }

    public override void OnEdited()
    {
        base.OnEdited();

        RepositionToOrigin();
        UpdateInitialCompounds();
        UpdateIsBacteria();

        // Reset endosymbiont status so that they aren't free to move / delete in the next editor cycle
        var count = Organelles.Organelles.Count;
        for (var i = 0; i < count; ++i)
        {
            ModifiableOrganelles.Organelles[i].IsEndosymbiont = false;
        }

        cachedFillTimes.Clear();

        SpecializationBonus =
            MicrobeInternalCalculations.CalculateSpecializationBonus(Organelles,
                new Dictionary<OrganelleDefinition, int>());
    }

    public override bool RepositionToOrigin()
    {
        var changes = Organelles.RepositionToOrigin();
        CalculateRotationSpeed();
        return changes;
    }

    public override void UpdateInitialCompounds()
    {
        // Since the initial compounds are only set once per species, they can't be calculated for each Biome.
        // So, the compound balance calculation uses the default biome.
        // Also, we should not overtly punish photosynthesizers, so we just use the consumption here (instead of
        // balance where the generated glucose would offset things and spawn photosynthesizers with no glucose,
        // which could basically make them die instantly in certain situations)
        var simulationParameters = SimulationParameters.Instance;

        // TODO: improve this depending on a hardcoded patch: https://github.com/Revolutionary-Games/Thrive/issues/5446
        var biomeConditions = simulationParameters.GetBiome("speciesInitialCompoundsBiome").Conditions;

        var compoundBalances = new Dictionary<Compound, CompoundBalance>();

        // TODO: figure out a way to use the real patch environmental tolerances
        var environmentalTolerances = new ResolvedMicrobeTolerances
        {
            HealthModifier = 1,
            OsmoregulationModifier = 1,
            ProcessSpeedModifier = 1,
        };

        // False is passed here until we can make the initial compounds patch specific
        // We don't take specialization into account here, so we overestimate how much stuff is needed
        ProcessSystem.ComputeCompoundBalance(Organelles, biomeConditions, environmentalTolerances, 1,
            CompoundAmountType.Biome, false, compoundBalances);

        bool giveBonusGlucose = Organelles.Count <= Constants.FULL_INITIAL_GLUCOSE_SMALL_SIZE_LIMIT && IsBacteria;

        var cachedCapacities = StorageCapacities;

        InitialCompounds.Clear();

        foreach (var compoundBalance in compoundBalances)
        {
            // Skip compounds we don't want to give as initial compounds
            if (!simulationParameters.GetCompoundDefinition(compoundBalance.Key).CanBeInitialCompound)
                continue;

            // Find the specific capacity, if any, and add it to the nominal
            cachedCapacities.Specific.TryGetValue(compoundBalance.Key, out var compoundCapacity);
            compoundCapacity += cachedCapacities.Nominal;

            if (compoundBalance.Key == Compound.Glucose && giveBonusGlucose)
            {
                InitialCompounds.Add(compoundBalance.Key, compoundCapacity);
                continue;
            }

            var balanceValue = compoundBalance.Value;

            // Skip compounds there's no consumption for (from processes)
            if (balanceValue.Consumption <= MathUtils.EPSILON)
                continue;

            // Initial compounds should suffice for a fixed amount of time of consumption.
            var compoundInitialAmount =
                Math.Abs(balanceValue.Consumption) * Constants.INITIAL_COMPOUND_TIME;

            if (compoundInitialAmount > compoundCapacity)
                compoundInitialAmount = compoundCapacity;

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

        // TODO: can we do caching somehow here?
        var resolvedTolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(
            MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(this, microbeSpawnEnvironment.CurrentBiome));

        // This lock is here to allow multiple microbe spawns to happen in parallel. Lock is not used on clear as no
        // spawns should be allowed to happen while species are being modified
        lock (cachedFillTimes)
        {
            if (!cachedFillTimes.TryGetValue(biome, out compoundTimes))
            {
                // TODO: should moving be false in some cases?
                compoundTimes = MicrobeInternalCalculations.CalculateDayVaryingCompoundsFillTimes(Organelles,
                    MembraneType, true, PlayerSpecies, biome, resolvedTolerances, spawnEnvironment.WorldSettings);
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
            Organelles.AddFast(organelle.Clone(), workMemory1, workMemory2);
        }

        IsBacteria = casted.IsBacteria;
        MembraneType = casted.MembraneType;
        MembraneRigidity = casted.MembraneRigidity;
        SpecializationBonus = casted.SpecializationBonus;

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
        result.SpecializationBonus = SpecializationBonus;

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

        hash ^= PersistentStringHash.GetHash(MembraneType.InternalName) * 5743;
        hash ^= (ulong)MembraneRigidity.GetHashCode() * 5749;
        hash ^= (IsBacteria ? 1UL : 0UL) * 5779UL;
        hash ^= (ulong)count * 131;

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
                BaseHexSize) + "\n" +
            Localization.Translate("TOLERANCE_DETAIL_TEXT").FormatSafe(Tolerances.PreferredTemperature,
                Tolerances.TemperatureTolerance,
                Tolerances.PressureMinimum,
                Tolerances.PressureMinimum + Tolerances.PressureTolerance,
                Math.Round(Tolerances.OxygenResistance * 100, 2),
                Math.Round(Tolerances.UVResistance * 100, 2));
    }

    protected override Dictionary<Compound, float> CalculateTotalReproductionCost()
    {
        var result = base.CalculateTotalReproductionCost();

        int organelleCount = Organelles.Organelles.Count;

        for (int i = 0; i < organelleCount; ++i)
        {
            result.Merge(Organelles.Organelles[i].Definition.InitialComposition);
        }

        return result;
    }

    private void CalculateRotationSpeed()
    {
        BaseRotationSpeed = MicrobeInternalCalculations.CalculateRotationSpeed(Organelles.Organelles);
    }
}
