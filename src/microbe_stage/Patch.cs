using System;
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

    /// <summary>
    ///   The current conditions of this patch.
    /// </summary>
    [JsonProperty]
    public readonly PatchConditions Conditions = new PatchConditions();

    [JsonProperty]
    public readonly ISet<Patch> Adjacent = new HashSet<Patch>();

    [JsonProperty]
    public readonly Biome BiomeTemplate;

    [JsonProperty]
    public readonly int[] Depth = new int[2] { -1, -1 };

    public Patch(string name, int id, Biome biomeTemplate)
    {
        Name = name;
        ID = id;
        BiomeTemplate = biomeTemplate;
        Conditions.Biome = (BiomeConditions)biomeTemplate.Conditions.Clone();
    }

    [JsonProperty]
    public string Name { get; private set; }

    /// <summary>
    ///   Coordinates this patch is to be displayed in the GUI
    /// </summary>
    public Vector2 ScreenCoordinates { get; set; } = new Vector2(0, 0);

    /// <summary>
    ///   List of all the recorded conditions of this patch. Useful for statistics.
    /// </summary>
    [JsonProperty]
    public Deque<PatchConditions> History { get; private set; } = new Deque<PatchConditions>();

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
        foreach (var entry in Conditions.SpeciesInPatch)
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
        if (Conditions.SpeciesInPatch.ContainsKey(species))
            return false;

        Conditions.SpeciesInPatch[species] = population;
        return true;
    }

    /// <summary>
    ///   Removes a species from this patch
    /// </summary>
    /// <returns>True when a species was removed</returns>
    public bool RemoveSpecies(Species species)
    {
        return Conditions.SpeciesInPatch.Remove(species);
    }

    /// <summary>
    ///   Updates a species population in this patch
    /// </summary>
    /// <returns>True on success</returns>
    public bool UpdateSpeciesPopulation(Species species, long newPopulation)
    {
        if (!Conditions.SpeciesInPatch.ContainsKey(species))
            return false;

        Conditions.SpeciesInPatch[species] = newPopulation;
        return true;
    }

    public long GetSpeciesPopulation(Species species)
    {
        if (!Conditions.SpeciesInPatch.ContainsKey(species))
            return 0;

        return Conditions.SpeciesInPatch[species];
    }

    public float GetTotalChunkCompoundAmount(Compound compound)
    {
        var result = 0.0f;

        foreach (var chunkKey in Conditions.Biome.Chunks.Keys)
        {
            var chunk = Conditions.Biome.Chunks[chunkKey];

            if (chunk.Density > 0 && chunk.Compounds.ContainsKey(compound))
            {
                result += chunk.Density * chunk.Compounds[compound].Amount;
            }
        }

        return result;
    }

    public void RecordConditions(double timePeriod)
    {
        if (History.Count >= Constants.MAX_NUM_OF_STORED_PATCH_CONDITIONS)
            History.RemoveFromBack();

        var snapshot = (PatchConditions)Conditions.Clone();
        snapshot.TimePeriod = timePeriod;
        History.AddToFront(snapshot);
    }

    public override string ToString()
    {
        return $"Patch \"{Name}\"";
    }
}

/// <summary>
///   Conditions of a patch at some point in time.
/// </summary>
[UseThriveSerializer]
public class PatchConditions : ICloneable
{
    public double TimePeriod;

    /// <summary>
    ///   List of all species and their populations in this patch
    /// </summary>
    public Dictionary<Species, long> SpeciesInPatch = new Dictionary<Species, long>();

    public BiomeConditions Biome;

    public object Clone()
    {
        var result = new PatchConditions
        {
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
