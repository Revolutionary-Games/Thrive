using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Nito.Collections;

/// <summary>
///   A patch is an instance of a Biome with some species in it
/// </summary>
[JsonObject(IsReference = true)]
[TypeConverter(typeof(ThriveTypeConverter))]
[JSONAlwaysDynamicType]
[UseThriveConverter]
[UseThriveSerializer]
public class Patch
{
    private readonly Compound sunlight;

    /// <summary>
    ///   The current snapshot of this patch.
    /// </summary>
    [JsonProperty]
    private readonly PatchSnapshot currentSnapshot;

    /// <summary>
    ///   The gameplay adjusted populations (only if set for a species, otherwise missing).
    ///   <see cref="GetSpeciesGameplayPopulation"/>
    /// </summary>
    [JsonProperty]
    private readonly Dictionary<Species, long> gameplayPopulations = new();

    [JsonProperty]
    private Deque<PatchSnapshot> history = new();

    public Patch(LocalizedString name, int id, Biome biomeTemplate, BiomeType biomeType, PatchRegion region)
    {
        Name = name;
        ID = id;
        BiomeTemplate = biomeTemplate;
        BiomeType = biomeType;
        currentSnapshot = new PatchSnapshot((BiomeConditions)biomeTemplate.Conditions.Clone());
        Region = region;

        sunlight = SimulationParameters.Instance.GetCompound("sunlight");
    }

    public Patch(LocalizedString name, int id, Biome biomeTemplate, BiomeType biomeType, PatchSnapshot currentSnapshot)
        : this(name, id, biomeTemplate, currentSnapshot)
    {
        BiomeType = biomeType;
    }

    [JsonConstructor]
    public Patch(LocalizedString name, int id, Biome biomeTemplate, PatchSnapshot currentSnapshot)
    {
        Name = name;
        ID = id;
        BiomeTemplate = biomeTemplate;
        this.currentSnapshot = currentSnapshot;

        sunlight = SimulationParameters.Instance.GetCompound("sunlight");
    }

    [JsonProperty]
    public int ID { get; }

    [JsonIgnore]
    public ISet<Patch> Adjacent { get; } = new HashSet<Patch>();

    [JsonProperty]
    public Biome BiomeTemplate { get; }

    [JsonProperty]
    public LocalizedString Name { get; private set; }

    /// <summary>
    ///   The region this patch belongs to. This has nullability suppression here to solve the circular dependency with
    ///   <see cref="PatchRegion.Patches"/>
    /// </summary>
    [JsonProperty]
    public PatchRegion Region { get; private set; } = null!;

    [JsonProperty]
    public BiomeType BiomeType { get; private set; }

    [JsonProperty]
    public int[] Depth { get; private set; } = { -1, -1 };

    /// <summary>
    ///   Coordinates this patch is to be displayed in the GUI
    /// </summary>
    public Vector2 ScreenCoordinates { get; set; } = new(0, 0);

    /// <summary>
    ///   List of all the recorded snapshot of this patch. Useful for statistics.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<PatchSnapshot> History => history;

    [JsonIgnore]
    public PatchSnapshot CurrentSnapshot => currentSnapshot;

    [JsonIgnore]
    public double TimePeriod
    {
        get => currentSnapshot.TimePeriod;
        set => currentSnapshot.TimePeriod = value;
    }

    /// <summary>
    ///   List of all species and their populations in this patch
    /// </summary>
    [JsonIgnore]
    public Dictionary<Species, long> SpeciesInPatch => currentSnapshot.SpeciesInPatch;

    [JsonIgnore]
    public BiomeConditions Biome => currentSnapshot.Biome;

    /// <summary>
    ///   Logged events that specifically occurred in this patch.
    /// </summary>
    public IReadOnlyList<GameEventDescription> EventsLog => currentSnapshot.EventsLog;

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
    ///   Looks for a species with the specified name in this patch
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

    public float GetCompoundAmount(string compoundName, CompoundAmountType amountType = CompoundAmountType.Current)
    {
        return GetCompoundAmount(SimulationParameters.Instance.GetCompound(compoundName), amountType);
    }

    public float GetCompoundAmount(Compound compound, CompoundAmountType amountType = CompoundAmountType.Current)
    {
        switch (compound.InternalName)
        {
            case "sunlight":
            case "oxygen":
            case "carbondioxide":
            case "nitrogen":
                return GetAmbientCompound(compound, amountType) * 100;
            case "iron":
                return GetTotalChunkCompoundAmount(compound);
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
                    amount = Biome.GetCompound(compound, amountType);
                }

                // TODO: passing amountType to GetTotalChunkCompoundAmount
                return amount.Density * amount.Amount + GetTotalChunkCompoundAmount(compound);
            }
        }
    }

    public float GetCompoundAmountInSnapshot(PatchSnapshot snapshot, string compoundName)
    {
        var compound = SimulationParameters.Instance.GetCompound(compoundName);

        switch (compoundName)
        {
            case "sunlight":
            case "oxygen":
            case "carbondioxide":
            case "nitrogen":
                return GetAmbientCompound(compound, CompoundAmountType.Biome) * 100;
            case "iron":
                return GetTotalChunkCompoundAmount(compound);
            default:
                return snapshot.Biome.Compounds[compound].Density * snapshot.Biome.Compounds[compound].Amount +
                    GetTotalChunkCompoundAmount(compound);
        }
    }

    public float GetTotalChunkCompoundAmount(Compound compound)
    {
        var result = 0.0f;

        foreach (var chunkKey in Biome.Chunks.Keys)
        {
            var chunk = Biome.Chunks[chunkKey];

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
        Biome.AverageCompounds[sunlight] = new BiomeCompoundProperties
        {
            Ambient = Biome.MaximumCompounds[sunlight].Ambient * multiplier,
        };
    }

    public void UpdateCurrentSunlight(float multiplier)
    {
        Biome.CurrentCompoundAmounts[sunlight] = new BiomeCompoundProperties
        {
            Ambient = Biome.MaximumCompounds[sunlight].Ambient * multiplier,
        };
    }

    /// <summary>
    ///   Logs description of an event into the patch's history.
    /// </summary>
    /// <param name="description">The event's description</param>
    /// <param name="highlight">If true, the event will be highlighted in the timeline UI</param>
    /// <param name="iconPath">Resource path to the icon of the event</param>
    public void LogEvent(LocalizedString description, bool highlight = false, string? iconPath = null)
    {
        // Event already logged in timeline
        if (currentSnapshot.EventsLog.Any(entry => entry.Description.Equals(description)))
            return;

        currentSnapshot.EventsLog.Add(new GameEventDescription(description, iconPath, highlight));
    }

    public override string ToString()
    {
        return $"Patch \"{Name}\"";
    }

    private float GetAmbientCompound(Compound compound, CompoundAmountType option)
    {
        switch (option)
        {
            // TODO: minimum?
            case CompoundAmountType.Current:
                return Biome.CurrentCompoundAmounts[compound].Ambient;
            case CompoundAmountType.Maximum:
                return Biome.MaximumCompounds[compound].Ambient;
            case CompoundAmountType.Average:
                return Biome.AverageCompounds[compound].Ambient;
            case CompoundAmountType.Biome:
                return Biome.Compounds[compound].Ambient;
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
public class PatchSnapshot : ICloneable
{
    public double TimePeriod;

    public Dictionary<Species, long> SpeciesInPatch = new();
    public Dictionary<Species, SpeciesInfo> RecordedSpeciesInfo = new();

    public BiomeConditions Biome;

    public List<GameEventDescription> EventsLog = new();

    public PatchSnapshot(BiomeConditions biome)
    {
        Biome = biome;
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
        var result = new PatchSnapshot((BiomeConditions)Biome.Clone())
        {
            TimePeriod = TimePeriod,
            SpeciesInPatch = new Dictionary<Species, long>(SpeciesInPatch),
            RecordedSpeciesInfo = new Dictionary<Species, SpeciesInfo>(RecordedSpeciesInfo),
            EventsLog = new List<GameEventDescription>(EventsLog),
        };

        return result;
    }
}
