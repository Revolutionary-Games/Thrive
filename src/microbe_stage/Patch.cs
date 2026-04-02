using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Nito.Collections;
using SharedBase.Archive;

/// <summary>
///   A patch is an instance of a Biome with some species in it
/// </summary>
public class Patch : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 2;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString UnknownPatchName = new LocalizedString("UNKNOWN_PATCH");
    private static readonly LocalizedString HiddenPatchName = new LocalizedString("UNDISCOVERED_PATCH");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    /// <summary>
    ///   The current snapshot of this patch.
    /// </summary>
    private readonly PatchSnapshot currentSnapshot;

    /// <summary>
    ///   The gameplay adjusted populations (only if set for a species, otherwise missing).
    ///   <see cref="GetSpeciesGameplayPopulation"/>
    /// </summary>
    private readonly Dictionary<Species, long> gameplayPopulations = new();

    private Deque<PatchSnapshot> history = new();

    public Patch(LocalizedString name, int id, Biome biomeTemplate, BiomeType biomeType, PatchRegion region,
        long additionalDataSeed)
    {
        Name = name;
        ID = id;
        BiomeTemplate = biomeTemplate;
        BiomeType = biomeType;
        currentSnapshot =
            new PatchSnapshot((BiomeConditions)biomeTemplate.Conditions.Clone(), biomeTemplate.Background);
        Region = region;

        DynamicDataSeed = additionalDataSeed;
    }

    public Patch(LocalizedString name, int id, Biome biomeTemplate, BiomeType biomeType, PatchSnapshot currentSnapshot,
        long additionalDataSeed)
    {
        Name = name;
        ID = id;
        BiomeTemplate = biomeTemplate;
        this.currentSnapshot = currentSnapshot;
        BiomeType = biomeType;
        DynamicDataSeed = additionalDataSeed;
    }

    private Patch(LocalizedString name, int id, Biome biomeTemplate, PatchSnapshot currentSnapshot,
        Dictionary<Species, long> gameplayPopulations)
    {
        Name = name;
        ID = id;
        BiomeTemplate = biomeTemplate;
        this.currentSnapshot = currentSnapshot;
        this.gameplayPopulations = gameplayPopulations;
    }

    public int ID { get; }

    public ISet<Patch> Adjacent { get; private set; } = new HashSet<Patch>();

    public Biome BiomeTemplate { get; }

    public LocalizedString Name { get; private set; }

    /// <summary>
    ///   The region this patch belongs to. This has nullability suppression here to solve the circular dependency with
    ///   <see cref="PatchRegion.Patches"/>
    /// </summary>
    public PatchRegion Region { get; private set; } = null!;

    public BiomeType BiomeType { get; private set; }

    public int[] Depth { get; private set; } = { -1, -1 };

    /// <summary>
    ///   The visibility of this patch on the map
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Generally, this should not be set directly, instead <see cref="ApplyVisibility"/> should be used to
    ///     perform additional checks and apply visibility to the region
    ///   </para>
    /// </remarks>
    public MapElementVisibility Visibility { get; set; } = MapElementVisibility.Hidden;

    /// <summary>
    ///   Coordinates this patch is to be displayed in the GUI
    /// </summary>
    public Vector2 ScreenCoordinates { get; set; } = new(0, 0);

    /// <summary>
    ///   List of all the recorded snapshot of this patch. Useful for statistics.
    /// </summary>
    public IReadOnlyList<PatchSnapshot> History => history;

    public PatchSnapshot CurrentSnapshot => currentSnapshot;

    public double TimePeriod
    {
        get => currentSnapshot.TimePeriod;
        set => currentSnapshot.TimePeriod = value;
    }

    /// <summary>
    ///   List of all species and their populations in this patch
    /// </summary>
    public Dictionary<Species, long> SpeciesInPatch => currentSnapshot.SpeciesInPatch;

    public BiomeConditions Biome => currentSnapshot.Biome;

    public string Background => currentSnapshot.Background ?? BiomeTemplate.Background;

    /// <summary>
    ///   Logged events that specifically occurred in this patch.
    /// </summary>
    public IReadOnlyList<GameEventDescription> EventsLog => currentSnapshot.EventsLog;

    /// <summary>
    ///   Current patch events affecting this patch with their properties.
    /// </summary>
    public IReadOnlyDictionary<PatchEventTypes, PatchEventProperties> ActivePatchEvents =>
        currentSnapshot.ActivePatchEvents;

    /// <summary>
    ///   The name of the patch the player should see; this accounts for fog of war and <see cref="Visibility"/>
    /// </summary>
    public LocalizedString VisibleName
    {
        get
        {
            switch (Visibility)
            {
                case MapElementVisibility.Shown:
                    return Name;
                case MapElementVisibility.Unknown:
                    return UnknownPatchName;
                case MapElementVisibility.Hidden:
                    return HiddenPatchName;
                default:
                    throw new InvalidOperationException("Invalid Patch Visibility");
            }
        }
    }

    /// <summary>
    ///   True when this patch has compounds that vary during the day / night cycle
    /// </summary>
    public bool HasDayAndNight => Biome.HasCompoundsThatVary();

    /// <summary>
    ///   Seed for generating dynamic runtime data for this patch (for example, terrain)
    /// </summary>
    public long DynamicDataSeed { get; private set; }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.Patch;
    public bool CanBeReferencedInArchive => true;

    /// <summary>
    ///   Adds all neighbors recursively to the provided <see cref="HashSet{T}"/>
    /// </summary>
    /// <param name="patch">The <see cref="Patch"/> to start from</param>
    /// <param name="set">The <see cref="HashSet{T}"/> to add to</param>
    public static void CollectNeighbours(Patch patch, HashSet<Patch> set)
    {
        foreach (var neighbour in patch.Adjacent)
        {
            if (set.Add(neighbour))
            {
                CollectNeighbours(neighbour, set);
            }
        }
    }

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.Patch)
            throw new NotSupportedException();

        writer.WriteObject((Patch)obj);
    }

    public static Patch ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Read all required fields from archive before Patch initialization
        var name = reader.ReadObject<LocalizedString>();
        var id = reader.ReadInt32();
        var biomeTemplate = reader.ReadObject<Biome>();
        var currentSnapshot = reader.ReadObject<PatchSnapshot>();
        var gameplayPopulations = reader.ReadObject<Dictionary<Species, long>>();

        if (version <= 1)
        {
            var patchEventTypes = reader.ReadObject<List<PatchEventTypes>>();
            foreach (var eventType in patchEventTypes)
            {
                currentSnapshot.ActivePatchEvents.Add(eventType, new PatchEventProperties());
            }

            // Starting sunlight and temperature are set here instead of in biome conditions because they
            // are not present there
            var biomeReference = SimulationParameters.Instance.GetBiome(biomeTemplate.InternalName);
            currentSnapshot.Biome.StartingSunlightValue = biomeReference.Conditions.Compounds
                .GetValueOrDefault(Compound.Sunlight, default(BiomeCompoundProperties)).Ambient;
            currentSnapshot.Biome.StartingTemperatureValue = biomeReference.Conditions.Compounds
                .GetValueOrDefault(Compound.Temperature, default(BiomeCompoundProperties)).Ambient;
        }

        var biomeType = (BiomeType)reader.ReadInt32();
        var depth = reader.ReadObject<int[]>();
        var visibility = (MapElementVisibility)reader.ReadInt32();
        var screenCoordinates = reader.ReadVector2();
        var dynamicDataSeed = reader.ReadInt64();

        var instance = new Patch(name, id, biomeTemplate, currentSnapshot, gameplayPopulations)
        {
            BiomeType = biomeType,
            Depth = depth,
            Visibility = visibility,
            ScreenCoordinates = screenCoordinates,
            DynamicDataSeed = dynamicDataSeed,
        };

        reader.ReportObjectConstructorDone(instance, referenceId);

        instance.Adjacent = reader.ReadObject<HashSet<Patch>>();

        instance.Region = reader.ReadObject<PatchRegion>();

        // Sadly need to make a temporary list here to copy the data as we don't have a separate deque deserializer
        // which would be just duplicated code from the list reader
        instance.history = new Deque<PatchSnapshot>(reader.ReadObject<List<PatchSnapshot>>());

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Name);
        writer.Write(ID);
        writer.WriteObject(BiomeTemplate);
        writer.WriteObject(currentSnapshot);
        writer.WriteObject(gameplayPopulations);
        writer.Write((int)BiomeType);
        writer.WriteObject(Depth);
        writer.Write((int)Visibility);
        writer.Write(ScreenCoordinates);
        writer.Write(DynamicDataSeed);

        writer.WriteObject(Adjacent);
        writer.WriteObject(Region);

        writer.WriteObject(history);
    }

    /// <summary>
    ///   Adds a connection to patch
    /// </summary>
    /// <returns>True if this was new, false if already added</returns>
    public bool AddNeighbour(Patch patch)
    {
        return Adjacent.Add(patch);
    }

    /// <summary>
    ///   Checks all neighbours recursively to find all connected patch nodes
    /// </summary>
    /// <returns>A <see cref="HashSet{T}"/> of <see cref="Patch"/> connected to this node by some means</returns>
    public HashSet<Patch> GetAllConnectedPatches()
    {
        var resultSet = new HashSet<Patch>();
        CollectNeighbours(this, resultSet);

        return resultSet;
    }

    /// <summary>
    ///   Checks closest neighbours using breadth-first search (BFS) with the given maximum visits.
    /// </summary>
    /// <param name="visits">The maximum number of patches to visit/add</param>
    /// <returns>A <see cref="HashSet{T}"/> of closest Patches connected to this node by some means</returns>
    public HashSet<Patch> GetClosestConnectedPatches(int visits = 20)
    {
        var queue = new Queue<Patch>();
        var visited = new HashSet<Patch>();

        queue.Enqueue(this);
        visited.Add(this);

        var maxReached = false;

        while (queue.Count > 0 && !maxReached)
        {
            var vertex = queue.Dequeue();

            foreach (var patch in vertex.Adjacent)
            {
                if (visited.Add(patch))
                {
                    queue.Enqueue(patch);

                    if (--visits <= 0)
                    {
                        maxReached = true;
                        break;
                    }
                }
            }
        }

        return visited;
    }

    /// <summary>
    ///   Looks for a species with the specified id in this patch
    /// </summary>
    public Species? FindSpeciesByID(uint id)
    {
        foreach (var entry in currentSnapshot.SpeciesInPatch)
        {
            if (entry.Key.ID == id)
                return entry.Key;
        }

        return null;
    }

    /// <summary>
    ///   Adds a new species to this patch. May only be called after auto-evo has ran.
    /// </summary>
    /// <returns>True when added. False if the species was already in this patch</returns>
    public bool AddSpecies(Species species, long population)
    {
        if (currentSnapshot.SpeciesInPatch.ContainsKey(species))
            return false;

        currentSnapshot.SpeciesInPatch[species] = population;
        return true;
    }

    /// <summary>
    ///   Removes a species from this patch. May only be called after auto-evo has ran.
    /// </summary>
    /// <returns>True when a species was removed</returns>
    public bool RemoveSpecies(Species species)
    {
        return currentSnapshot.SpeciesInPatch.Remove(species);
    }

    public int GetSpeciesCount()
    {
        int result = 0;

        foreach (var entry in SpeciesInPatch)
        {
            if (entry.Value > 0)
                ++result;
        }

        return result;
    }

    /// <summary>
    ///   Updates a species population in this patch. Should only be called by auto-evo applying the results.
    /// </summary>
    /// <returns>True on success</returns>
    public bool UpdateSpeciesSimulationPopulation(Species species, long newPopulation)
    {
        if (!currentSnapshot.SpeciesInPatch.ContainsKey(species))
            return false;

        currentSnapshot.SpeciesInPatch[species] = newPopulation;
        return true;
    }

    /// <summary>
    ///   Returns the auto-evo simulation confirmed population numbers
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The simulation population is different from the gameplay population in that it may not be modified by
    ///     anything else except auto-evo results applying. Auto-evo also relies on this population number not changing
    ///     at all while it is running. Gameplay population is an additional layer on top of the last simulation
    ///     population to record immediate external effects. The gameplay populations are not authoritative and will be
    ///     overridden the next time simulation populations are updated
    ///   </para>
    /// </remarks>
    /// <param name="species">The species to get the population for</param>
    /// <returns>The population amount</returns>
    public long GetSpeciesSimulationPopulation(Species species)
    {
        if (!currentSnapshot.SpeciesInPatch.TryGetValue(species, out var population))
            return 0;

        return population;
    }

    /// <summary>
    ///   Gets the population that's potentially adjusted during the current swimming around cycle (before auto-evo
    ///   results are applied)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     See the remarks on <see cref="GetSpeciesSimulationPopulation"/> for more info
    ///   </para>
    /// </remarks>
    /// <param name="species">The species to get the population for</param>
    /// <returns>The gameplay population, or if not set the simulation population</returns>
    public long GetSpeciesGameplayPopulation(Species species)
    {
        if (gameplayPopulations.TryGetValue(species, out var population))
            return population;

        return GetSpeciesSimulationPopulation(species);
    }

    /// <summary>
    ///   Updates a species gameplay population in this patch. This maybe called even when auto-evo is running. Once
    ///   this is called <see cref="GetSpeciesGameplayPopulation"/> starts returning the set value instead of the
    ///   simulation population.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that gameplay results disappear when auto-evo results are applied, so the same change needs to also
    ///     be saved as an external effect.
    ///   </para>
    /// </remarks>
    /// <returns>True on success</returns>
    public bool UpdateSpeciesGameplayPopulation(Species species, long newPopulation)
    {
        if (!currentSnapshot.SpeciesInPatch.ContainsKey(species))
            return false;

        gameplayPopulations[species] = newPopulation;
        return true;
    }

    /// <summary>
    ///   Should only be called by auto-evo results after applying themselves to clear out the gameplay populations.
    ///   <see cref="PatchMap.DiscardGameplayPopulations"/>
    /// </summary>
    public void DiscardGameplayPopulations()
    {
        gameplayPopulations.Clear();
    }

    public bool IsSurfacePatch()
    {
        return Depth[0] == 0 && BiomeType != BiomeType.Cave;
    }

    public bool IsOceanicPatch()
    {
        return BiomeType is BiomeType.Epipelagic or BiomeType.IceShelf or BiomeType.Mesopelagic
            or BiomeType.Bathypelagic or BiomeType.Abyssopelagic or BiomeType.Seafloor;
    }

    public bool IsContinentalPatch()
    {
        return BiomeType is BiomeType.Banana or BiomeType.Coastal or BiomeType.Estuary or BiomeType.Tidepool;
    }

    public float GetCompoundAmountForDisplay(Compound compound,
        CompoundAmountType amountType = CompoundAmountType.Current)
    {
        return GetCompoundAmountInSnapshotForDisplay(currentSnapshot, compound, amountType);
    }

    public float GetCompoundAmountInSnapshotForDisplay(PatchSnapshot snapshot, Compound compound,
        CompoundAmountType amountType = CompoundAmountType.Current)
    {
        switch (compound)
        {
            case Compound.Sunlight:
            case Compound.Temperature:
            case Compound.Oxygen:
            case Compound.Carbondioxide:
            case Compound.Nitrogen:
                return GetAmbientCompoundInSnapshot(snapshot, compound, CompoundAmountType.Biome) * 100;
            case Compound.Radiation:
            case Compound.Iron:
                return GetTotalChunkCompoundAmountInSnapshot(snapshot, compound);
            default:
            {
                BiomeCompoundProperties amount;
                if (amountType == CompoundAmountType.Template)
                {
                    // TODO: chunk handling?
                    amount = BiomeTemplate.Conditions.GetCompound(compound, CompoundAmountType.Biome);
                }
                else
                {
                    amount = snapshot.Biome.GetCompound(compound, amountType);
                }

                // TODO: passing amountType to GetTotalChunkCompoundAmount
                return amount.Density * amount.Amount + GetTotalChunkCompoundAmountInSnapshot(snapshot, compound);
            }
        }
    }

    public float GetTotalChunkCompoundAmount(Compound compound)
    {
        return GetTotalChunkCompoundAmountInSnapshot(currentSnapshot, compound);
    }

    public float GetTotalChunkCompoundAmountInSnapshot(PatchSnapshot snapshot, Compound compound)
    {
        var result = 0.0f;

        foreach (var chunkKey in snapshot.Biome.Chunks.Keys)
        {
            var chunk = snapshot.Biome.Chunks[chunkKey];

            if (chunk.Compounds == null)
                continue;

            if (chunk.Density > 0 && chunk.Compounds.TryGetValue(compound, out var chunkCompound))
            {
                result += chunk.Density * chunkCompound.Amount;
            }
        }

        return result;
    }

    /// <summary>
    ///   Stores the current state of the patch into the patch history.
    /// </summary>
    public void RecordSnapshot(bool clearLoggedEvents)
    {
        if (history.Count >= Constants.PATCH_HISTORY_RANGE)
            history.RemoveFromBack();

        foreach (var species in currentSnapshot.SpeciesInPatch.Keys)
        {
            currentSnapshot.RecordedSpeciesInfo[species] = species.RecordSpeciesInfo();
        }

        var snapshot = (PatchSnapshot)currentSnapshot.Clone();
        history.AddToFront(snapshot);

        if (clearLoggedEvents)
            currentSnapshot.EventsLog.Clear();
    }

    public void ReplaceSpecies(Species old, Species newSpecies, bool replaceInHistory = true)
    {
        currentSnapshot.ReplaceSpecies(old, newSpecies);

        if (!replaceInHistory)
            return;

        foreach (var snapshot in History)
        {
            snapshot.ReplaceSpecies(old, newSpecies);
        }

        // TODO: can we do something about the game log here?
    }

    public void UpdateAverageSunlight(float multiplier)
    {
        Biome.AverageCompounds[Compound.Sunlight] = new BiomeCompoundProperties
        {
            Ambient = Biome.Compounds[Compound.Sunlight].Ambient * multiplier,
        };
    }

    public void UpdateCurrentSunlight(float multiplier)
    {
        Biome.CurrentCompoundAmounts[Compound.Sunlight] = new BiomeCompoundProperties
        {
            Ambient = Biome.Compounds[Compound.Sunlight].Ambient * multiplier,
        };
    }

    /// <summary>
    ///   Generates a set of tolerances for a microbe that starts living in this patch. The tolerances aren't exactly
    ///   perfectly tailored for this patch (as that would make initial migrations harder), but are always without any
    ///   negative effects. To balance out organelle tolerance debuffs, needs to be given the current organelles.
    /// </summary>
    /// <returns>Set of tolerances that can survive well in the current patch</returns>
    public EnvironmentalTolerances GenerateTolerancesForMicrobe(IReadOnlyList<OrganelleTemplate> organelles)
    {
        // To guarantee perfect tolerance, we need to apply reverse of the organelle effects so that when the organelle
        // effects are applied, the final tolerances are well adapted
        var organelleEffects = default(MicrobeEnvironmentalToleranceCalculations.ToleranceValues);

        MicrobeEnvironmentalToleranceCalculations.ApplyOrganelleEffectsOnTolerances(organelles, ref organelleEffects);

        var result = GenerateOptimalTolerances(organelleEffects);

#if DEBUG
        result.SanityCheck();

        var optimalTest =
            MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(result, organelles, currentSnapshot.Biome);

        if (optimalTest.OverallScore is < 1 or > 1 + MathUtils.EPSILON)
        {
            GD.PrintErr("Optimal tolerance creation failed, score: " + optimalTest.OverallScore);

            if (Debugger.IsAttached)
                Debugger.Break();
        }
#endif

        return result;
    }

    /// <summary>
    ///   Base variant of good tolerance value calculations. Can be used for species types that don't have an overload
    ///   of this operation.
    /// </summary>
    /// <param name="externalModifiers">External modifiers that this balances the optimal tolerances against</param>
    /// <returns>Optimal tolerances to live in this patch</returns>
    public EnvironmentalTolerances GenerateOptimalTolerances(
        MicrobeEnvironmentalToleranceCalculations.ToleranceValues externalModifiers)
    {
        var result = new EnvironmentalTolerances
        {
            OxygenResistance = GetAmbientCompound(Compound.Oxygen, CompoundAmountType.Biome),
            UVResistance = GetAmbientCompound(Compound.Sunlight, CompoundAmountType.Biome),
            PressureMinimum = Math.Max(Biome.Pressure - Constants.TOLERANCE_INITIAL_PRESSURE_RANGE * 0.5f, 0),
            PressureTolerance = Constants.TOLERANCE_INITIAL_PRESSURE_RANGE,
            PreferredTemperature = GetAmbientCompound(Compound.Temperature, CompoundAmountType.Biome) -
                externalModifiers.PreferredTemperature * 1.01f,
            TemperatureTolerance = Constants.TOLERANCE_INITIAL_TEMPERATURE_RANGE,
        };

        // Apply the reverse of the negative effects to balance things out (and slightly exaggerate to not run into
        // rounding issues)
        if (externalModifiers.OxygenResistance < 0)
            result.OxygenResistance -= externalModifiers.OxygenResistance * 1.01f;

        if (externalModifiers.UVResistance < 0)
            result.UVResistance -= externalModifiers.UVResistance * 1.01f;

        if (externalModifiers.PressureTolerance < 0)
            result.PressureTolerance -= externalModifiers.PressureTolerance * 1.01f;

        return result;
    }

    public EnvironmentalTolerances GenerateTolerancesForMicrobe(IndividualHexLayout<CellTemplate> cells)
    {
        var organelleEffects = default(MicrobeEnvironmentalToleranceCalculations.ToleranceValues);

        MicrobeEnvironmentalToleranceCalculations.ApplyCellEffectsOnTolerances(cells, ref organelleEffects);

        var result = GenerateOptimalTolerances(organelleEffects);

#if DEBUG
        result.SanityCheck();

        var optimalTest =
            MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(result, cells, currentSnapshot.Biome);

        if (optimalTest.OverallScore is < 1 or > 1 + MathUtils.EPSILON)
        {
            GD.PrintErr("Optimal tolerance creation failed for multicellular, score: " + optimalTest.OverallScore);

            if (Debugger.IsAttached)
                Debugger.Break();
        }
#endif

        return result;
    }

    /// <summary>
    ///   Logs description of an event into the patch's history.
    /// </summary>
    /// <param name="description">The event's description</param>
    /// <param name="highlight">If true, the event will be highlighted in the timeline UI</param>
    /// <param name="showInReport">If true, the event will be shown on the report tab main page</param>
    /// <param name="iconPath">Resource path to the icon of the event</param>
    public void LogEvent(LocalizedString description, bool highlight = false,
        bool showInReport = false, string? iconPath = null)
    {
        // Event already logged in timeline
        foreach (var gameEvent in currentSnapshot.EventsLog)
        {
            if (gameEvent.Description.Equals(description))
                return;
        }

        currentSnapshot.EventsLog.Add(new GameEventDescription(description, iconPath, highlight, showInReport));
    }

    /// <summary>
    ///   Runs <see cref="ApplyVisibility"/> on all the patches neighbours
    /// </summary>
    /// <param name="visibility">The visibility to be set</param>
    public void ApplyVisibilityToNeighbours(MapElementVisibility visibility)
    {
        foreach (var patch in Adjacent)
        {
            patch.ApplyVisibility(visibility);
        }
    }

    /// <summary>
    ///   Sets <see cref="Visibility"/> and the visibility of the region if more visible than current
    /// </summary>
    /// <param name="visibility">The visibility to be set</param>
    public void ApplyVisibility(MapElementVisibility visibility)
    {
        // Only update visibility if the new visibility is more visible than the current one
        if ((int)Visibility >= (int)visibility)
            Visibility = visibility;

        if ((int)Region.Visibility >= (int)visibility)
            Region.Visibility = visibility;
    }

    public void ApplyPatchEventVisuals(PatchMapNode node,
        IReadOnlyDictionary<PatchEventTypes, PatchEventProperties>? events = null)
    {
        // If the events are not specified, use the ones from the current generation
        events ??= ActivePatchEvents;

        if (Visibility == MapElementVisibility.Shown)
            node.ShowEventVisuals(events);
    }

    public override string ToString()
    {
        return $"Patch \"{Name}\"";
    }

    private float GetAmbientCompound(Compound compound, CompoundAmountType option)
    {
        return GetAmbientCompoundInSnapshot(currentSnapshot, compound, option);
    }

    private float GetAmbientCompoundInSnapshot(PatchSnapshot snapshot, Compound compound, CompoundAmountType option)
    {
        switch (option)
        {
            case CompoundAmountType.Current:
                return snapshot.Biome.CurrentCompoundAmounts[compound].Ambient;
            case CompoundAmountType.Maximum:
                return snapshot.Biome.MaximumCompounds[compound].Ambient;
            case CompoundAmountType.Minimum:
                return snapshot.Biome.MinimumCompounds[compound].Ambient;
            case CompoundAmountType.Average:
                return snapshot.Biome.AverageCompounds[compound].Ambient;
            case CompoundAmountType.Biome:
                return snapshot.Biome.Compounds[compound].Ambient;
            case CompoundAmountType.Template:
                return BiomeTemplate.Conditions.Compounds[compound].Ambient;
            default:
                throw new ArgumentOutOfRangeException(nameof(option), option, null);
        }
    }
}

/// <summary>
///   Snapshot of a patch at some point in time.
/// </summary>
public class PatchSnapshot : ICloneable, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 2;

    public double TimePeriod;

    public Dictionary<Species, long> SpeciesInPatch = new();
    public Dictionary<Species, SpeciesInfo> RecordedSpeciesInfo = new();

    public BiomeConditions Biome;
    public string? Background;

    public List<GameEventDescription> EventsLog = new();

    public Dictionary<PatchEventTypes, PatchEventProperties> ActivePatchEvents = new();

    public PatchSnapshot(BiomeConditions biome, string? background)
    {
        Biome = biome;
        Background = background;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.PatchSnapshot;
    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.PatchSnapshot)
            throw new NotSupportedException();

        writer.WriteObject((PatchSnapshot)obj);
    }

    public static PatchSnapshot ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new PatchSnapshot(reader.ReadObject<BiomeConditions>(), reader.ReadString())
        {
            TimePeriod = reader.ReadDouble(),
            SpeciesInPatch = reader.ReadObject<Dictionary<Species, long>>(),
            RecordedSpeciesInfo = reader.ReadObject<Dictionary<Species, SpeciesInfo>>(),
            EventsLog = reader.ReadObject<List<GameEventDescription>>(),
            ActivePatchEvents = version <= 1 ?
                new Dictionary<PatchEventTypes, PatchEventProperties>() :
                reader.ReadObject<Dictionary<PatchEventTypes, PatchEventProperties>>(),
        };
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Biome);
        writer.Write(Background);
        writer.Write(TimePeriod);
        writer.WriteObject(SpeciesInPatch);
        writer.WriteObject(RecordedSpeciesInfo);
        writer.WriteObject(EventsLog);
        writer.WriteObject(ActivePatchEvents);
    }

    public void ReplaceSpecies(Species old, Species newSpecies)
    {
        if (SpeciesInPatch.TryGetValue(old, out var population))
        {
            SpeciesInPatch.Remove(old);
            SpeciesInPatch.Add(newSpecies, population);
        }

        if (RecordedSpeciesInfo.TryGetValue(old, out var info))
        {
            RecordedSpeciesInfo.Remove(old);
            RecordedSpeciesInfo.Add(newSpecies, info);
        }

        // TODO: can we handle EventsLog here?
    }

    public object Clone()
    {
        // We only do a shallow copy of RecordedSpeciesInfo here as SpeciesInfo objects are never modified.
        var result = new PatchSnapshot((BiomeConditions)Biome.Clone(), Background)
        {
            TimePeriod = TimePeriod,
            SpeciesInPatch = new Dictionary<Species, long>(SpeciesInPatch),
            RecordedSpeciesInfo = new Dictionary<Species, SpeciesInfo>(RecordedSpeciesInfo),
            EventsLog = new List<GameEventDescription>(EventsLog),
            ActivePatchEvents = new Dictionary<PatchEventTypes, PatchEventProperties>(ActivePatchEvents),
        };

        return result;
    }
}
