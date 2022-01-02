using System;
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
    private readonly PatchSnapshot currentSnapshot = new PatchSnapshot();

    [JsonProperty]
    private Deque<PatchSnapshot> history = new Deque<PatchSnapshot>();

    public Patch(string name, int id, Biome biomeTemplate)
    {
        Name = name;
        ID = id;
        BiomeTemplate = biomeTemplate;
        currentSnapshot.Biome = (BiomeConditions)biomeTemplate.Conditions.Clone();
    }

    [JsonProperty]
    public string Name { get; private set; }

    /// <summary>
    ///   Coordinates this patch is to be displayed in the GUI
    /// </summary>
    public Vector2 ScreenCoordinates { get; set; } = new Vector2(0, 0);

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
    ///   All logged events occured in this patch.
    /// </summary>
    public IReadOnlyList<PatchSnapshot.EventDescription> Events => currentSnapshot.Events;

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
    ///   Looks for a species with the specified name in this patch
    /// </summary>
    public Species FindSpeciesByID(uint id)
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

    public float GetTotalChunkCompoundAmount(Compound compound)
    {
        var result = 0.0f;

        foreach (var chunkKey in Biome.Chunks.Keys)
        {
            var chunk = Biome.Chunks[chunkKey];

            if (chunk.Density > 0 && chunk.Compounds.ContainsKey(compound))
            {
                result += chunk.Density * chunk.Compounds[compound].Amount;
            }
        }

        return result;
    }

    /// <summary>
    ///   Stores the current state of the patch into the patch history.
    /// </summary>
    public void RecordSnapshot()
    {
        if (history.Count >= Constants.PATCH_HISTORY_RANGE)
            history.RemoveFromBack();

        foreach (var species in currentSnapshot.SpeciesInPatch.Keys)
        {
            currentSnapshot.RecordedSpeciesInfo[species] = species.RecordSpeciesInfo();
        }

        var snapshot = (PatchSnapshot)currentSnapshot.Clone();
        history.AddToFront(snapshot);
    }

    public void LogEvent(LocalizedString description, bool highlight = false, string iconPath = null)
    {
        // Event already logged in timeline
        if (currentSnapshot.Events.Any(entry => entry.Description.ToString() == description.ToString()))
            return;

        currentSnapshot.Events.Add(new PatchSnapshot.EventDescription
        {
            Description = description,
            IconPath = iconPath,
            Highlighted = highlight,
        });
    }

    public void ClearLoggedEvents()
    {
        currentSnapshot.Events.Clear();
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

    public Dictionary<Species, long> SpeciesInPatch = new Dictionary<Species, long>();
    public Dictionary<Species, SpeciesInfo> RecordedSpeciesInfo = new Dictionary<Species, SpeciesInfo>();

    public BiomeConditions Biome;

    public List<EventDescription> Events = new();

    public object Clone()
    {
        // We only do a shallow copy of RecordedSpeciesInfo here as SpeciesInfo objects are never modified.
        var result = new PatchSnapshot
        {
            TimePeriod = TimePeriod,
            SpeciesInPatch = new Dictionary<Species, long>(SpeciesInPatch),
            RecordedSpeciesInfo = new Dictionary<Species, SpeciesInfo>(RecordedSpeciesInfo),
            Biome = (BiomeConditions)Biome.Clone(),
            Events = new List<EventDescription>(Events),
        };

        return result;
    }

    /// <summary>
    ///   A text-based description of what has happened in a patch to be added to the timeline. Decorated with an icon
    ///   if there's any.
    /// </summary>
    public class EventDescription
    {
        public LocalizedString Description { get; set; }
        public string IconPath { get; set; }
        public bool Highlighted { get; set; }
    }
}
