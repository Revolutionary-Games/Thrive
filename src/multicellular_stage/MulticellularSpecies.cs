using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using SharedBase.Archive;
using Systems;

/// <summary>
///   Represents a multicellular species composed of multiple cells
/// </summary>
public class MulticellularSpecies : Species, IReadOnlyMulticellularSpecies, ISimulationPhotographable
{
    public const ushort SERIALIZATION_VERSION = 1;

    private ReadonlyCellLayoutAdapter<IReadOnlyCellTemplate, CellTemplate>? readonlyCellLayoutAdapter;
    private ReadonlyIndividualLayoutAdapter<CellTemplate, IReadOnlyCellTemplate>? readonlyIndividualLayoutAdapter;

    private IndividualHexLayout<CellTemplate>? modifiableEditorCells;

    public MulticellularSpecies(uint id, string genus, string epithet) : base(id, genus, epithet)
    {
    }

    /// <summary>
    ///   The cells that make up this species' body plan. The first index is the cell of the bud type, and the cells
    ///   grow in order.
    /// </summary>
    public CellLayout<CellTemplate> ModifiableGameplayCells { get; private set; } = new();

    // TODO: find a way around this adapter class
    public IReadOnlyCellLayout<IReadOnlyCellTemplate> GameplayCells => readonlyCellLayoutAdapter ??=
        new ReadonlyCellLayoutAdapter<IReadOnlyCellTemplate, CellTemplate>(ModifiableGameplayCells);

    /// <summary>
    ///   The 'original' colony layout, from which the simulated one (<see cref="GameplayCells"/>) is generated.
    /// </summary>
    public IndividualHexLayout<CellTemplate> ModifiableEditorCells
    {
        get
        {
            if (modifiableEditorCells != null)
            {
#if DEBUG
                if (modifiableEditorCells.Count < 1)
                {
                    Debugger.Break();
                    GD.PrintErr("Editor cells are missing from species!");
                }
#endif
                return modifiableEditorCells;
            }

            // Recalculate from the gameplay cells if the editor layout is missing
            GD.Print($"Creating missing editor layout from gameplay cells for species: {FormattedIdentifier}");

            var result = new IndividualHexLayout<CellTemplate>();

            // A bit inefficient to need to allocate temporary memory here, but this is anyway pretty expensive to need
            // to calculate this for a species
            MulticellularLayoutHelpers.GenerateEditorLayoutFromGameplayLayout(result, ModifiableGameplayCells,
                new List<Hex>(), new List<Hex>());

            modifiableEditorCells = result;
            return result;
        }
        set => modifiableEditorCells = value;
    }

    // TODO: find a away around this adapter class
    public IReadOnlyIndividualLayout<IReadOnlyCellTemplate> EditorCells => readonlyIndividualLayoutAdapter ??=
        new ReadonlyIndividualLayoutAdapter<CellTemplate, IReadOnlyCellTemplate>(ModifiableEditorCells);

    public List<CellType> ModifiableCellTypes { get; private set; } = new();

    public IReadOnlyList<IReadOnlyCellTypeDefinition> CellTypes => ModifiableCellTypes;

    public ISimulationPhotographable.SimulationType SimulationToPhotograph =>
        ISimulationPhotographable.SimulationType.MicrobeGraphics;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MulticellularSpecies;

    public static MulticellularSpecies ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new MulticellularSpecies(reader.ReadUInt32(),
            reader.ReadString() ?? throw new NullArchiveObjectException(),
            reader.ReadString() ?? throw new NullArchiveObjectException());

        reader.ReportObjectConstructorDone(instance, referenceId);

        instance.ReadNonConstructorBaseProperties(reader, 1);

        instance.ModifiableGameplayCells = reader.ReadObject<CellLayout<CellTemplate>>();
        instance.modifiableEditorCells = reader.ReadObjectOrNull<IndividualHexLayout<CellTemplate>>();
        instance.ModifiableCellTypes = reader.ReadObject<List<CellType>>();

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        WriteBasePropertiesToArchive(writer);

        writer.WriteObject(ModifiableGameplayCells);
        writer.WriteObject(ModifiableEditorCells);
        writer.WriteObject(ModifiableCellTypes);
    }

    public override void OnEdited()
    {
        base.OnEdited();

        RepositionToOrigin();
        UpdateInitialCompounds();

        // Make certain these are all up to date
        foreach (var cellType in ModifiableCellTypes)
        {
            // See the comment in CellBodyPlanEditorComponent.OnFinishEditing
            if (cellType.RepositionToOrigin())
            {
                GD.Print("Repositioned a multicellular species' cell type. This might break / crash the " +
                    "body plan layout.");
            }

            // Reset endosymbiont status so that they aren't free to move / delete in the next editor cycle
            var count = cellType.ModifiableOrganelles.Count;
            for (var i = 0; i < count; ++i)
            {
                cellType.ModifiableOrganelles.Organelles[i].IsEndosymbiont = false;
            }
        }

        if (modifiableEditorCells != null)
        {
            // TODO: should this just automatically remove it?
            if (modifiableEditorCells.Count != ModifiableGameplayCells.Count)
                throw new Exception("Editor cells have not been updated after species edit");

#if DEBUG
            foreach (var hexWithData in modifiableEditorCells.AsModifiable())
            {
                if (hexWithData.Data == null)
                    throw new Exception("Editor cells have no data");

                if (hexWithData.Data.Position != hexWithData.Position ||
                    hexWithData.Data.Orientation != hexWithData.Orientation)
                {
                    throw new Exception(
                        "Editor cells have not been updated after species edit to match their position");
                }
            }
#endif
        }
    }

    public override bool RepositionToOrigin()
    {
        // TODO: should this actually reposition things as the cell at index 0 is always the colony leader so if it
        // isn't centered, that'll cause issues?
        // var centerOfMass = ModifiableCells.CenterOfMass;

        var centerOfMass = ModifiableGameplayCells[0].Position;

        if (centerOfMass.Q == 0 && centerOfMass.R == 0)
            return false;

        foreach (var cell in ModifiableGameplayCells)
        {
            // This calculation aligns the center of mass with the origin by moving every organelle of the microbe.
            cell.Position -= centerOfMass;
        }

        return true;
    }

    public override void UpdateInitialCompounds()
    {
        var simulationParameters = SimulationParameters.Instance;

        // Since the initial compounds are only set once per species they can't be calculated for each Biome.
        // So, the compound balance calculation uses a special biome.
        // TODO: see the TODOS in MicrobeSpecies as well as: https://github.com/Revolutionary-Games/Thrive/issues/5446
        var biomeConditions = simulationParameters.GetBiome("speciesInitialCompoundsBiome").Conditions;

        var compoundBalances = new Dictionary<Compound, CompoundBalance>();

        // TODO: environmental tolerances for multicellular
        var environmentalTolerances = new ResolvedMicrobeTolerances
        {
            HealthModifier = 1,
            OsmoregulationModifier = 1,
            ProcessSpeedModifier = 1,
        };

        ProcessSystem.ComputeCompoundBalance(ModifiableGameplayCells[0].ModifiableOrganelles,
            biomeConditions, environmentalTolerances, CompoundAmountType.Biome, false, compoundBalances);
        var storageCapacity =
            MicrobeInternalCalculations.CalculateCapacity(ModifiableGameplayCells[0].ModifiableOrganelles);

        InitialCompounds.Clear();

        foreach (var compoundBalance in compoundBalances)
        {
            if (compoundBalance.Value.Balance >= 0)
                continue;

            // Skip compounds we don't want to give as initial compounds
            if (!simulationParameters.GetCompoundDefinition(compoundBalance.Key).CanBeInitialCompound)
                continue;

            // Initial compounds should suffice for a fixed amount of time.
            // Some extra is given to accommodate multicellular growth
            var compoundInitialAmount = Math.Abs(compoundBalance.Value.Balance) *
                Constants.INITIAL_COMPOUND_TIME * Constants.MULTICELLULAR_INITIAL_COMPOUND_MULTIPLIER;

            if (compoundInitialAmount > storageCapacity)
                compoundInitialAmount = storageCapacity;

            InitialCompounds.Add(compoundBalance.Key, compoundInitialAmount);
        }
    }

    public override void HandleNightSpawnCompounds(CompoundBag targetStorage, ISpawnEnvironmentInfo spawnEnvironment)
    {
        if (spawnEnvironment is not IMicrobeSpawnEnvironment microbeSpawnEnvironment)
            throw new ArgumentException("Multicellular species must have microbe spawn environment info");

        // TODO: this would be excellent to match the actual cell type being used for spawning
        var cellType = ModifiableGameplayCells[0].ModifiableCellType;

        // TODO: environmental tolerances for multicellular
        var environmentalTolerances = new ResolvedMicrobeTolerances
        {
            HealthModifier = 1,
            OsmoregulationModifier = 1,
            ProcessSpeedModifier = 1,
        };

        // TODO: CACHING IS MISSING from here (but microbe has it)
        // TODO: should moving be false in some cases?
        var compoundTimes = MicrobeInternalCalculations.CalculateDayVaryingCompoundsFillTimes(
            cellType.ModifiableOrganelles, cellType.MembraneType, true, PlayerSpecies,
            microbeSpawnEnvironment.CurrentBiome, environmentalTolerances,
            microbeSpawnEnvironment.WorldSettings);

        MicrobeInternalCalculations.GiveNearNightInitialCompoundBuff(targetStorage, compoundTimes,
            spawnEnvironment.DaylightInfo);
    }

    public override void ApplyMutation(Species mutation)
    {
        base.ApplyMutation(mutation);

        var casted = (MulticellularSpecies)mutation;

        ModifiableGameplayCells.Clear();

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        foreach (var cellTemplate in casted.ModifiableGameplayCells)
        {
            ModifiableGameplayCells.AddFast((CellTemplate)cellTemplate.Clone(), workMemory1, workMemory2);
        }

        ModifiableCellTypes.Clear();

        foreach (var cellType in casted.ModifiableCellTypes)
        {
            ModifiableCellTypes.Add((CellType)cellType.Clone());
        }
    }

    public override float GetPredationTargetSizeFactor()
    {
        var totalOrganelles = 0;

        int count = ModifiableGameplayCells.Count;
        for (int i = 0; i < count; ++i)
        {
            totalOrganelles += ModifiableGameplayCells[i].Organelles.Count;
        }

        return totalOrganelles;
    }

    public void SetupWorldEntities(IWorldSimulation worldSimulation)
    {
        ((MicrobeVisualOnlySimulation)worldSimulation).CreateVisualisationColony(this);
    }

    public bool StateHasStabilized(IWorldSimulation worldSimulation)
    {
        return true;
    }

    public Vector3 CalculatePhotographDistance(IWorldSimulation worldSimulation)
    {
        return ((MicrobeVisualOnlySimulation)worldSimulation).CalculateColonyPhotographDistance();
    }

    public override object Clone()
    {
        var result = new MulticellularSpecies(ID, Genus, Epithet);

        ClonePropertiesTo(result);

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        foreach (var cellTemplate in ModifiableGameplayCells)
        {
            result.ModifiableGameplayCells.AddFast((CellTemplate)cellTemplate.Clone(), workMemory1, workMemory2);
        }

        if (result.modifiableEditorCells == null)
        {
            result.modifiableEditorCells = new IndividualHexLayout<CellTemplate>();
        }
        else
        {
            result.modifiableEditorCells.Clear();
        }

        foreach (var cellTemplate in (HexLayout<HexWithData<CellTemplate>>)ModifiableEditorCells)
        {
            result.modifiableEditorCells.AddFast(cellTemplate.Clone(), workMemory1, workMemory2);
        }

        foreach (var cellType in ModifiableCellTypes)
        {
            result.ModifiableCellTypes.Add((CellType)cellType.Clone());
        }

        return result;
    }

    public override ulong GetVisualHashCode()
    {
        ulong hash = 1099511628211;

        foreach (var cell in ModifiableGameplayCells)
        {
            hash += cell.GetVisualHashCode() ^ (ulong)cell.Position.GetHashCode();
        }

        return hash;
    }

    protected override Dictionary<Compound, float> CalculateBaseReproductionCost()
    {
        var baseReproductionCost = base.CalculateBaseReproductionCost();

        // Apply the multiplier to the costs for being multicellular
        var result = new Dictionary<Compound, float>();

        foreach (var entry in baseReproductionCost)
        {
            result[entry.Key] = entry.Value * Constants.MULTICELLULAR_BASE_REPRODUCTION_COST_MULTIPLIER;
        }

        return result;
    }

    protected override Dictionary<Compound, float> CalculateTotalReproductionCost()
    {
        var result = base.CalculateTotalReproductionCost();

        int count = ModifiableGameplayCells.Count;
        for (int i = 0; i < count; ++i)
        {
            result.Merge(ModifiableGameplayCells[i].CalculateTotalComposition());
        }

        return result;
    }
}
