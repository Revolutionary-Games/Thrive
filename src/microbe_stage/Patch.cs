﻿using System;
using System.Collections.Generic;
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
    public IReadOnlyCollection<PatchSnapshot> History => history;

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
    ///   Adds a connection to patch
    /// </summary>
    /// <returns>True if this was new, false if already added</returns>
    public bool AddNeighbour(Patch patch)
    {
        return Adjacent.Add(patch);
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
        if (!currentSnapshot.SpeciesInPatch.ContainsKey(species))
            return 0;

        return currentSnapshot.SpeciesInPatch[species];
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
    ///   Stores the current state of patch conditions into the patch history.
    /// </summary>
    public void RecordConditions()
    {
        if (history.Count >= Constants.PATCH_HISTORY_RANGE)
            history.RemoveFromBack();

        var conditions = (PatchSnapshot)currentSnapshot.Clone();
        history.AddToFront(conditions);
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

    public BiomeConditions Biome;

    public object Clone()
    {
        var result = new PatchSnapshot
        {
            TimePeriod = TimePeriod,
            SpeciesInPatch = new Dictionary<Species, long>(SpeciesInPatch.Count),
            Biome = (BiomeConditions)Biome.Clone(),
        };

        foreach (var entry in SpeciesInPatch)
        {
            result.SpeciesInPatch.Add(entry.Key, entry.Value);
        }

        return result;
    }
}
