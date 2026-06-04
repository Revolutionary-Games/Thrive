using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Godot;
using SharedBase.Archive;
using Systems;

/// <summary>
///   Represents a multicellular species composed of multiple cells
/// </summary>
public class MulticellularSpecies : Species, IReadOnlyMulticellularSpecies, ISimulationPhotographable
{
    public const ushort SERIALIZATION_VERSION = 3;

    private readonly Dictionary<BiomeConditions, Dictionary<Compound, (float TimeToFill, float Storage)>>
        cachedFillTimes = new();

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

    public MulticellularReproductionMethod ReproductionMethod { get; set; }

    public CellType? ModifiableSporeCellType { get; set; }

    public IReadOnlyCellTypeDefinition? SporeCellType => ModifiableSporeCellType;

    public ISimulationPhotographable.SimulationType SimulationToPhotograph =>
        ISimulationPhotographable.SimulationType.MicrobeGraphics;

    public override Stage StageForDisplay => Stage.MulticellularStage;

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

        if (version < 2)
        {
            // Need to fix editor layout data inconsistency
            if (instance.modifiableEditorCells != null)
            {
                foreach (var hexWithData in instance.modifiableEditorCells.AsModifiable())
                {
                    if (hexWithData.Data == null)
                    {
                        GD.PrintErr("Unexpectedly multicellular species editor cell has no data");
                    }
                    else
                    {
                        hexWithData.Data.Position = hexWithData.Position;
                        hexWithData.Data.Orientation = hexWithData.Orientation;
                    }
                }
            }
        }

        if (version >= 3)
        {
            instance.ReproductionMethod = (MulticellularReproductionMethod)reader.ReadInt32();
            instance.ModifiableSporeCellType = reader.ReadObjectOrNull<CellType>();
        }

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        WriteBasePropertiesToArchive(writer);

        writer.WriteObject(ModifiableGameplayCells);
        writer.WriteObject(ModifiableEditorCells);
        writer.WriteObject(ModifiableCellTypes);

        writer.Write((int)ReproductionMethod);
        writer.WriteObjectOrNull(ModifiableSporeCellType);
    }

    public override void OnEdited()
    {
        base.OnEdited();

        // TODO: do we need to reposition for auto-evo?
        RepositionToOrigin();

        bool sporeCellTypeInList = false;

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

            if (!sporeCellTypeInList && cellType == ModifiableSporeCellType)
                sporeCellTypeInList = true;
        }

        if (!sporeCellTypeInList && ModifiableSporeCellType != null)
            throw new Exception($"Spore cell type isn't present in the cell type list: {ModifiableSporeCellType}");

        if (ReproductionMethod == MulticellularReproductionMethod.Sporulation && ModifiableSporeCellType == null)
            throw new Exception("Sporulation reproduction method requires a spore cell type to be set");

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

#if DEBUG
        ModifiableGameplayCells.ThrowIfCellsOverlap();
#endif

        foreach (var modifiableGameplayCell in ModifiableGameplayCells)
        {
            bool typeExists = false;

            foreach (var typeDefinition in ModifiableCellTypes)
            {
                if (typeDefinition == modifiableGameplayCell.ModifiableCellType)
                {
                    typeExists = true;
                    break;
                }
            }

            if (!typeExists)
            {
#if DEBUG
                throw new Exception($"Gameplay cell type {modifiableGameplayCell.ModifiableCellType} does not exist " +
                    $"in species {FormattedIdentifier}");
#else
                GD.PrintErr($"Gameplay cell type {modifiableGameplayCell.ModifiableCellType} does not exist " +
                    $"in species {FormattedIdentifier}");
#endif
            }
        }
    }

    public override void OnAttemptedInAutoEvo(bool refreshCache)
    {
        base.OnAttemptedInAutoEvo(refreshCache);

        // TODO: in the future this will need to refresh specialization calculations for cell types

        UpdateInitialCompounds();

        cachedFillTimes.Clear();
    }

    public override bool RepositionToOrigin()
    {
        // TODO: should this actually reposition things as the cell at index 0 is always the colony leader so if it
        // isn't centered, that'll cause issues?
        // var centerOfMass = ModifiableCells.CenterOfMass;

        bool changes = RepositionGameplayCells();
        changes |= RepositionEditorCells();

        return changes;
    }

    public override void UpdateInitialCompounds()
    {
        var simulationParameters = SimulationParameters.Instance;

        // Since the initial compounds are only set once per species they can't be calculated for each Biome.
        // So, the compound balance calculation uses a special biome.
        // TODO: see the TODOS in MicrobeSpecies as well as: https://github.com/Revolutionary-Games/Thrive/issues/5446
        var biomeConditions = simulationParameters.GetBiome("speciesInitialCompoundsBiome").Conditions;

        var compoundBalances = new Dictionary<Compound, CompoundBalance>();

        // TODO: figure out a way to use the real patch environmental tolerances
        var environmentalTolerances = new ResolvedMicrobeTolerances
        {
            HealthModifier = 1,
            OsmoregulationModifier = 1,
            ProcessSpeedModifier = 1,
        };

        // We don't take specialization into account here, so we overestimate how much stuff is needed
        ProcessSystem.ComputeCompoundBalance(ModifiableGameplayCells[0].ModifiableOrganelles,
            biomeConditions, environmentalTolerances, 1, CompoundAmountType.Biome, false, compoundBalances);
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

        var biome = microbeSpawnEnvironment.CurrentBiome;

        // TODO: this would be excellent to match the actual cell type being used for spawning
        var cellType = ModifiableGameplayCells[0].ModifiableCellType;

        // TODO: can we do caching somehow here?
        var resolvedTolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(
            MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(this, microbeSpawnEnvironment.CurrentBiome));

        Dictionary<Compound, (float TimeToFill, float Storage)>? compoundTimes;

        // This lock is here to allow multiple microbe spawns to happen in parallel. Lock is not used on clear as no
        // spawns should be allowed to happen while species are being modified
        lock (cachedFillTimes)
        {
            if (!cachedFillTimes.TryGetValue(biome, out compoundTimes))
            {
                // This does not use the real specialization bonus to avoid underestimating how much single cells in
                // unusual spawn situations (such as spores) need to survive the night
                var totalSpecializationBonus = 1;

                // TODO: should moving be false in some cases?
                compoundTimes = MicrobeInternalCalculations.CalculateDayVaryingCompoundsFillTimes(
                    cellType.ModifiableOrganelles, cellType.MembraneType, true, PlayerSpecies,
                    totalSpecializationBonus, microbeSpawnEnvironment.CurrentBiome, resolvedTolerances,
                    microbeSpawnEnvironment.WorldSettings);
                cachedFillTimes[biome] = compoundTimes;
            }
        }

        MicrobeInternalCalculations.GiveNearNightInitialCompoundBuff(targetStorage, compoundTimes,
            spawnEnvironment.DaylightInfo);
    }

    public override void ApplyMutation(Species mutation)
    {
        base.ApplyMutation(mutation);

        var casted = (MulticellularSpecies)mutation;

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        // We need to ensure each cell type is cloned just once so that references work
        var typeMapping = new Dictionary<CellType, CellType>();

        ModifiableCellTypes.Clear();

        foreach (var cellType in casted.ModifiableCellTypes)
        {
            var clonedType = (CellType)cellType.Clone();
            ModifiableCellTypes.Add(clonedType);
            typeMapping[cellType] = clonedType;

            if (cellType == casted.ModifiableSporeCellType)
                ModifiableSporeCellType = clonedType;
        }

        ModifiableGameplayCells.Clear();

        foreach (var cellTemplate in casted.ModifiableGameplayCells)
        {
            var oldType = cellTemplate.ModifiableCellType;

            if (!typeMapping.TryGetValue(oldType, out var newType))
                throw new Exception("Cell type not found in species");

            ModifiableGameplayCells.AddFast(new CellTemplate(newType, cellTemplate.Position, cellTemplate.Orientation),
                workMemory1, workMemory2);
        }

        if (casted.ModifiableSporeCellType != null && ModifiableSporeCellType == null)
        {
            if (!typeMapping.TryGetValue(casted.ModifiableSporeCellType, out var newSporeType))
                throw new Exception("Spore cell type not found in species");

            ModifiableSporeCellType = newSporeType;
        }
        else if (casted.ModifiableSporeCellType == null)
        {
            ModifiableSporeCellType = null;
        }

        ReproductionMethod = casted.ReproductionMethod;

        // Recalculate editor cells if they exist as they are now out of date
        modifiableEditorCells = null;
        readonlyIndividualLayoutAdapter = null;

        cachedFillTimes.Clear();
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

    public float CalculateAverageSpecialization()
    {
        float score = 0;
        int count = ModifiableGameplayCells.Count;

        if (count < 1)
            return 1;

        for (int i = 0; i < count; ++i)
        {
            var cell = ModifiableGameplayCells[i];
            score += cell.CellType.CellTypeSpecializationBonus * GetAdjacencySpecializationBonus(i);
        }

        return score / count;
    }

    /// <summary>
    ///   Calculates the adjacency bonus of efficiency for a cell in the body plan of this species. This is meant to
    ///   be applied by multiplying into the base specialization bonus.
    /// </summary>
    /// <param name="cellIndexInBodyPlan">Index of the cell in the body plan we want the bonus for</param>
    /// <returns>The calculated bonus (or 1, if it can't be calculated)</returns>
    public float GetAdjacencySpecializationBonus(int cellIndexInBodyPlan)
    {
        // We theoretically don't have to access the modifiable things here, however, the wrapper doesn't provide
        // index access, so we use the modifiable property here.
        var modifiable = ModifiableEditorCells;

        if (modifiable.Count < cellIndexInBodyPlan)
        {
            GD.PrintErr("Cell index out of bounds, using first cell for specialization");
            cellIndexInBodyPlan = 0;
        }

        var cell = modifiable[cellIndexInBodyPlan];

        return CellBodyPlanInternalCalculations
            .GetAdjacencySpecializationBonusFromBodyPlan(cell.Data!, EditorCells);
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

    /// <summary>
    ///   The cell that gets produced by a fully grown colony that grows into its own organism.
    ///   Might be the same as <see cref="ColonyRootCellType"/>, but might be a spore or a gamete
    /// </summary>
    public CellType FirstCellTypeToSpawn()
    {
        if (ReproductionMethod == MulticellularReproductionMethod.Budding)
        {
            return ModifiableGameplayCells[0].ModifiableCellType;
        }

        if (ReproductionMethod == MulticellularReproductionMethod.Sporulation)
        {
            if (ModifiableSporeCellType == null)
            {
                throw new Exception("A spore-reproducing species' spore cell type is unset:" +
                    $"{FormattedName}");
            }

            return ModifiableSporeCellType;
        }

        throw new NotImplementedException($"Reproduction type not implemented: {ReproductionMethod}");
    }

    /// <summary>
    ///   Returns the cell type of the cell that can directly grow into a full colony. In advanced reproduction
    ///   methods, this cell can be created through spore germination or gamete fusion.
    /// </summary>
    public CellType ColonyRootCellType()
    {
        return ModifiableGameplayCells[0].ModifiableCellType;
    }

    public override object Clone()
    {
        var result = new MulticellularSpecies(ID, Genus, Epithet);

        ClonePropertiesTo(result);

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        // We need to ensure each cell type is cloned just once so that references work
        var typeMapping = new Dictionary<CellType, CellType>();

        foreach (var cellType in ModifiableCellTypes)
        {
            var clonedType = (CellType)cellType.Clone();
            result.ModifiableCellTypes.Add(clonedType);
            typeMapping[cellType] = clonedType;

            if (cellType == ModifiableSporeCellType)
                result.ModifiableSporeCellType = clonedType;
        }

        foreach (var cellTemplate in ModifiableGameplayCells)
        {
            var oldType = cellTemplate.ModifiableCellType;

            if (!typeMapping.TryGetValue(oldType, out var newType))
                throw new Exception("Cell type not found in species");

            result.ModifiableGameplayCells.AddFast(
                new CellTemplate(newType, cellTemplate.Position, cellTemplate.Orientation),
                workMemory1, workMemory2);
        }

        if (result.modifiableEditorCells == null)
        {
            result.modifiableEditorCells = new IndividualHexLayout<CellTemplate>();
        }
        else
        {
            result.modifiableEditorCells.Clear();
        }

        foreach (var hexWithData in (HexLayout<HexWithData<CellTemplate>>)ModifiableEditorCells)
        {
            var oldTemplate = hexWithData.Data;
            CellTemplate? newTemplate = null;

            if (oldTemplate != null)
            {
                var oldType = oldTemplate.ModifiableCellType;

                if (!typeMapping.TryGetValue(oldType, out var newType))
                    throw new Exception("Cell type not found in species");

                newTemplate = new CellTemplate(newType, oldTemplate.Position, oldTemplate.Orientation);
            }

            result.modifiableEditorCells.AddFast(
                new HexWithData<CellTemplate>(newTemplate, hexWithData.Position, hexWithData.Orientation),
                workMemory1, workMemory2);
        }

        if (ModifiableSporeCellType != null && result.ModifiableSporeCellType == null)
        {
            throw new Exception($"Cell type {ModifiableSporeCellType.ReadableName} not found while cloning" +
                $"multicellular species: {ReadableName}");
        }

        result.ReproductionMethod = ReproductionMethod;

        return result;
    }

    public override ulong GetVisualHashCode()
    {
        ulong hash = 1099511628211;

        foreach (var cell in ModifiableGameplayCells)
        {
            hash += cell.GetVisualHashCode() ^ (ulong)cell.Position.GetHashCode();
        }

        // Reproduction mode doesn't affect the visual hash code

        return hash;
    }

    public override string GetDetailString()
    {
        var reproductionType =
            Localization.Translate(ReproductionMethod.GetAttribute<DescriptionAttribute>().Description);

        return base.GetDetailString() + "\n" +
            Localization.Translate("MULTICELLULAR_SPECIES_DETAIL_TEXT").FormatSafe(ModifiableGameplayCells.Count,
                CellTypes.Count,
                reproductionType) + "\n" +
            Localization.Translate("TOLERANCE_DETAIL_TEXT").FormatSafe(Tolerances.PreferredTemperature,
                Tolerances.TemperatureTolerance,
                Tolerances.PressureMinimum,
                Tolerances.PressureMinimum + Tolerances.PressureTolerance,
                Math.Round(Tolerances.OxygenResistance * 100, 2),
                Math.Round(Tolerances.UVResistance * 100, 2));
    }

    protected override Dictionary<Compound, float> CalculateBaseReproductionCost()
    {
        var baseReproductionCost = base.CalculateBaseReproductionCost();

        // Apply the multiplier to the costs for being multicellular
        var result = new Dictionary<Compound, float>();

        var fromCellsMultiplier = ModifiableGameplayCells.Count *
            Constants.MULTICELLULAR_BASE_REPRODUCTION_COST_MULTIPLIER_PER_CELL;

        foreach (var entry in baseReproductionCost)
        {
            var multicellCost = entry.Value * Constants.MULTICELLULAR_BASE_REPRODUCTION_COST_MULTIPLIER;

            var cellExtra = fromCellsMultiplier * multicellCost;
            result[entry.Key] = multicellCost + cellExtra;
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

    private bool RepositionGameplayCells()
    {
        var centerOfMass = ModifiableGameplayCells[0].Position;

        if (centerOfMass.Q == 0 && centerOfMass.R == 0)
            return false;

        foreach (var cell in ModifiableGameplayCells)
        {
            // This calculation aligns the center of mass with the origin by moving every cell of the colony.
            cell.Position -= centerOfMass;
        }

        return true;
    }

    private bool RepositionEditorCells()
    {
        var editorLeaderPosition = ModifiableEditorCells[0].Position;

        if (editorLeaderPosition.Q == 0 && editorLeaderPosition.R == 0)
            return false;

        foreach (var cell in ModifiableEditorCells.AsModifiable())
        {
            cell.Position -= editorLeaderPosition;
            cell.Data!.Position -= editorLeaderPosition;
        }

        return true;
    }
}
