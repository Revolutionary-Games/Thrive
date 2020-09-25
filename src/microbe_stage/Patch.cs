using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A patch is an instance of a Biome with some species in it
/// </summary>
[JsonObject(IsReference = true)]
[UseThriveSerializer]
public class Patch
{
    /// <summary>
    ///   List of all species and their populations in this patch
    /// </summary>
    [JsonProperty]
    public readonly Dictionary<Species, long> SpeciesInPatch =
        new Dictionary<Species, long>();

    [JsonProperty]
    public readonly int ID;

    [JsonProperty]
    public readonly ISet<Patch> Adjacent = new HashSet<Patch>();

    [JsonProperty]
    public readonly BiomeConditions Biome;

    [JsonProperty]
    public readonly Biome BiomeTemplate;

    [JsonProperty]
    public readonly int[] Depth = new int[2] { -1, -1 };

    public Patch(string name, int id, Biome biomeTemplate)
    {
        Name = name;
        ID = id;
        BiomeTemplate = biomeTemplate;
        Biome = (BiomeConditions)biomeTemplate.Conditions.Clone();
    }

    [JsonProperty]
    public string Name { get; private set; }

    /// <summary>
    ///   Coordinates this patch is to be displayed in the GUI
    /// </summary>
    public Vector2 ScreenCoordinates { get; set; } = new Vector2(0, 0);

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
        foreach (var entry in SpeciesInPatch)
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
        if (SpeciesInPatch.ContainsKey(species))
            return false;

        SpeciesInPatch[species] = population;
        return true;
    }

    /// <summary>
    ///   Removes a species from this patch
    /// </summary>
    /// <returns>True when a species was removed</returns>
    public bool RemoveSpecies(Species species)
    {
        return SpeciesInPatch.Remove(species);
    }

    /// <summary>
    ///   Updates a species population in this patch
    /// </summary>
    /// <returns>True on success</returns>
    public bool UpdateSpeciesPopulation(Species species, long newPopulation)
    {
        if (!SpeciesInPatch.ContainsKey(species))
            return false;

        SpeciesInPatch[species] = newPopulation;
        return true;
    }

    public long GetSpeciesPopulation(Species species)
    {
        if (!SpeciesInPatch.ContainsKey(species))
            return 0;

        return SpeciesInPatch[species];
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

    public override string ToString()
    {
        return $"Patch \"{Name}\"";
    }
}
