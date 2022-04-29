﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Nito.Collections;

/// <summary>
///   A patch is an instance of a Biome with some species in it
/// </summary>
[JsonObject(IsReference = true)]
[UseThriveSerializer]
public class Patch
{
    [JsonProperty]
    public readonly int ID;

    [JsonProperty]
    public readonly ISet<Patch> Adjacent = new HashSet<Patch>();

    [JsonProperty]
    public readonly Biome BiomeTemplate;

    [JsonProperty]
    public readonly int[] Depth = new int[2] { -1, -1 };

    /// <summary>
    ///   The current snapshot of this patch.
    /// </summary>
    [JsonProperty]
    private readonly PatchSnapshot currentSnapshot;

    [JsonProperty]
    private Deque<PatchSnapshot> history = new();

    public Patch(LocalizedString name, int id, Biome biomeTemplate)
    {
        Name = name;
        ID = id;
        BiomeTemplate = biomeTemplate;
        currentSnapshot = new PatchSnapshot((BiomeConditions)biomeTemplate.Conditions.Clone());
    }

    [JsonProperty]
    public LocalizedString Name { get; private set; }

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
    ///   Adds a new species to this patch
    /// </summary>
    /// <returns>True when added. False if the species was already in this patch</returns>
    public bool AddSpecies(Species species, long population =
        Constants.INITIAL_SPECIES_POPULATION)
    {
        if (currentSnapshot.SpeciesInPatch.ContainsKey(species))
            return false;

        currentSnapshot.SpeciesInPatch[species] = population;
        return true;
    }

    /// <summary>
    ///   Removes a species from this patch
    /// </summary>
    /// <returns>True when a species was removed</returns>
    public bool RemoveSpecies(Species species)
    {
        return currentSnapshot.SpeciesInPatch.Remove(species);
    }

    /// <summary>
    ///   Updates a species population in this patch
    /// </summary>
    /// <returns>True on success</returns>
    public bool UpdateSpeciesPopulation(Species species, long newPopulation)
    {
        if (!currentSnapshot.SpeciesInPatch.ContainsKey(species))
            return false;

        currentSnapshot.SpeciesInPatch[species] = newPopulation;
        return true;
    }

    public long GetSpeciesPopulation(Species species)
    {
        if (!currentSnapshot.SpeciesInPatch.TryGetValue(species, out var population))
            return 0;

        return population;
    }

    public float GetCompoundAmount(string compoundName)
    {
        var compound = SimulationParameters.Instance.GetCompound(compoundName);

        switch (compoundName)
        {
            case "sunlight":
            case "oxygen":
            case "carbondioxide":
            case "nitrogen":
                return Biome.Compounds[compound].Dissolved * 100;
            case "iron":
                return GetTotalChunkCompoundAmount(compound);
            default:
                return Biome.Compounds[compound].Density * Biome.Compounds[compound].Amount +
                    GetTotalChunkCompoundAmount(compound);
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
                return snapshot.Biome.Compounds[compound].Dissolved * 100;
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
