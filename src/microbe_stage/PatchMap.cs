using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Whether the fog of war should be disabled,
///   mark patches adjacent to the player as explored,
///   or mark only the patch the player has entered as explored
/// </summary>
public enum FogOfWarMode
{
    Disabled,
    Normal,
    Intense,
}

/// <summary>
///   The visibility of an element in the patch map.
/// </summary>
public enum MapElementVisibility
{
    /// <summary>
    ///   Invisible to the player
    /// </summary>
    Undiscovered,

    /// <summary>
    ///   Visible to the player but details hidden
    /// </summary>
    Unexplored,

    /// <summary>
    ///   Visible to the player and details shown
    /// </summary>
    Explored,
}

/// <summary>
///   A container for patches that are joined together
/// </summary>
[UseThriveSerializer]
public class PatchMap : ISaveLoadable
{
    private Patch? currentPatch;

    /// <summary>
    ///   The list of patches. DO NOT MODIFY THE DICTIONARY FROM OUTSIDE THIS CLASS
    /// </summary>
    [JsonProperty]
    public Dictionary<int, Patch> Patches { get; private set; } = new();

    /// <summary>
    ///   The regions in this map
    /// </summary>
    [JsonProperty]
    public Dictionary<int, PatchRegion> Regions { get; private set; } = new();

    [JsonIgnore]
    public Vector2 Center =>
        Regions.Values.Aggregate(Vector2.Zero, (current, region) => current + region.ScreenCoordinates)
        / Regions.Count;

    /// <summary>
    ///   Currently active patch (the one player is in)
    /// </summary>
    public Patch? CurrentPatch
    {
        get => currentPatch;
        set
        {
            // Allow setting to null to make loading work
            if (value == null)
            {
                currentPatch = null;
                return;
            }

            // New patch must be part of this map
            if (!ContainsPatch(value))
                throw new ArgumentException("cannot set current patch to one not in map");

            currentPatch = value;
        }
    }

    [JsonProperty]
    private List<(int Id1, int Id2)> PatchAdjacencies { get; set; } = new();

    [JsonProperty]
    private List<(int Id1, int Id2)> RegionAdjacencies { get; set; } = new();

    /// <summary>
    ///   Adds a new patch to the map. Throws if can't add
    /// </summary>
    public void AddPatch(Patch patch)
    {
        if (Patches.ContainsKey(patch.ID))
        {
            throw new ArgumentException(
                "patch cannot be added to this map, the ID is already in use: " + patch.ID);
        }

        Patches[patch.ID] = patch;
    }

    /// <summary>
    ///   Adds a new region to the map. Throws if can't add
    /// </summary>
    public void AddRegion(PatchRegion region)
    {
        if (region.ID < 0)
            throw new ArgumentException("id must not be negative");

        if (Regions.ContainsKey(region.ID))
        {
            throw new ArgumentException(
                $"Region {region.Name} cannot be added to this map, the ID is already in use: {region.ID}");
        }

        Regions[region.ID] = region;
    }

    /// <summary>
    ///   Returns true when the map is valid and has no invalid references
    /// </summary>
    /// <returns>True when everything is fine</returns>
    public bool Verify()
    {
        if (CurrentPatch == null)
        {
            GD.PrintErr("CurrentPatch is not set on map");
            return false;
        }

        bool result = true;

        // Link verification caches
        var incomingLinks = new Dictionary<Patch, bool>();
        var seenLinks = new HashSet<Tuple<Patch, Patch>>();

        // Verify all adjacent patches are valid
        foreach (var entry in Patches)
        {
            if (entry.Value == null)
                return false;

            if (!incomingLinks.ContainsKey(entry.Value))
                incomingLinks[entry.Value] = false;

            foreach (var neighbour in entry.Value.Adjacent)
            {
                if (neighbour == null)
                    return false;

                if (!ContainsPatch(neighbour))
                {
                    GD.PrintErr("Patch ", entry.Value.Name, " links to non-existing patch: ",
                        neighbour.Name);
                    result = false;
                }

                incomingLinks[neighbour] = true;

                seenLinks.Add(new Tuple<Patch, Patch>(entry.Value, neighbour));
            }
        }

        // All patches have an incoming link
        foreach (var entry in incomingLinks)
        {
            if (!entry.Value)
            {
                // Allow the initial patch to not have any incoming links as long as
                // it is the only one
                if (Patches.Count == 1 && entry.Key == CurrentPatch)
                    continue;

                GD.PrintErr("no incoming links found for patch: ", entry.Key.Name);
                result = false;
            }
        }

        // All links are two way
        // TODO: do we want always two way links?
        foreach (var entry1 in seenLinks)
        {
            // Find the other way
            bool found = false;

            foreach (var entry2 in seenLinks)
            {
                if (entry1.Item1 == entry2.Item2 && entry1.Item2 == entry2.Item1)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                GD.PrintErr("link ", entry1.Item1.Name, " -> ", entry1.Item2.Name,
                    " is one way. These types of links are currently not wanted");
                result = false;
            }
        }

        // Region links are correct
        foreach (var entry in RegionAdjacencies)
        {
            // TODO: checking for special regions
            if (entry.Id1 < 0 || entry.Id2 < 0)
                continue;

            if (!Regions.ContainsKey(entry.Id1) || !Regions.ContainsKey(entry.Id2))
            {
                GD.PrintErr($"Invalid region link: from {entry.Id1} to {entry.Id2}");
                result = false;
            }
        }

        // Verify unique IDs
        foreach (var region in Regions)
        {
            if (region.Key != region.Value.ID)
                GD.Print($"Region key {region.Key} doesn't match ID in object {region.Value.ID}");
        }

        // TODO: check each region has a link
        // Note: it seems that region links are two-way by default and don't have the ability to only have one way
        // linkage

        return result;
    }

    /// <summary>
    ///   Finds a species in the current patch map with name
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This starts from the current patch and then falls back to
    ///     checking all patches. This is done to improve performance
    ///     as it is likely that species in the current patch are
    ///     looked up.
    ///   </para>
    /// </remarks>
    public Species? FindSpeciesByID(uint id)
    {
        if (CurrentPatch != null)
        {
            var found = CurrentPatch.FindSpeciesByID(id);

            if (found != null)
                return found;
        }

        foreach (var entry in Patches)
        {
            if (entry.Value == CurrentPatch)
                continue;

            var found = entry.Value.FindSpeciesByID(id);

            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    ///   Updates the global population numbers in Species
    /// </summary>
    public void UpdateGlobalPopulations()
    {
        var seenPopulations = new Dictionary<Species, long>();

        foreach (var entry in Patches)
        {
            foreach (var speciesEntry in entry.Value.SpeciesInPatch)
            {
                Species species = speciesEntry.Key;

                if (!seenPopulations.ContainsKey(species))
                    seenPopulations[species] = 0;

                if (speciesEntry.Value > 0)
                    seenPopulations[species] += speciesEntry.Value;
            }
        }

        // Apply the populations after calculating them
        foreach (var entry in seenPopulations)
        {
            entry.Key.SetPopulationFromPatches(entry.Value);
        }
    }

    /// <summary>
    ///   Gets the species population in all patches.
    /// </summary>
    public long GetSpeciesGlobalSimulationPopulation(Species species)
    {
        return Patches.Values.Sum(p => p.GetSpeciesSimulationPopulation(species));
    }

    /// <summary>
    ///   Gets the species gameplay population (<see cref="Patch.GetSpeciesGameplayPopulation"/>) in all patches.
    /// </summary>
    public long GetSpeciesGlobalGameplayPopulation(Species species)
    {
        return Patches.Values.Sum(p => p.GetSpeciesGameplayPopulation(species));
    }

    /// <summary>
    ///   Removes species from patches where their population is &lt;= 0
    /// </summary>
    /// <returns>
    ///   The extinct creatures
    /// </returns>
    public List<Species> RemoveExtinctSpecies(bool playerCantGoExtinct = false)
    {
        var result = new HashSet<Species>();

        List<Species> nonExtinctSpecies = FindAllSpeciesWithPopulation();

        foreach (var patch in Patches)
        {
            // We remove a species as extinct when its population value is not strictly positive,
            // unless it's the player's species and the player can't go extinct.
            var toRemove = patch.Value.SpeciesInPatch.Where(v => v.Value <= 0 &&
                !(playerCantGoExtinct && v.Key.PlayerSpecies)).ToList();

            foreach (var speciesEntry in toRemove)
            {
                patch.Value.RemoveSpecies(speciesEntry.Key);

                if (!nonExtinctSpecies.Contains(speciesEntry.Key))
                {
                    result.Add(speciesEntry.Key);
                }
            }
        }

        return result.ToList();
    }

    /// <summary>
    ///   Returns all species on the map with > 0 population
    /// </summary>
    /// <returns>
    ///   Non-Extinct creatures
    /// </returns>
    public List<Species> FindAllSpeciesWithPopulation()
    {
        var found = new HashSet<Species>();

        foreach (var entry in Patches)
        {
            foreach (var speciesEntry in entry.Value.SpeciesInPatch)
            {
                if (speciesEntry.Value > 0)
                {
                    found.Add(speciesEntry.Key);
                }
            }
        }

        return found.ToList();
    }

    /// <summary>
    ///   Called after auto-evo has applied results. Clears the previous gameplay populations so that next time
    ///   gameplay populations are used they start off with the new simulation computed values.
    /// </summary>
    public void DiscardGameplayPopulations()
    {
        foreach (var entry in Patches)
        {
            entry.Value.DiscardGameplayPopulations();
        }
    }

    /// <summary>
    ///   Updates the time period in all of the patches.
    /// </summary>
    public void UpdateGlobalTimePeriod(double time)
    {
        foreach (var patch in Patches)
        {
            patch.Value.TimePeriod = time;
        }
    }

    public Patch GetPatch(int id)
    {
        return Patches[id];
    }

    public bool ContainsPatch(Patch patch)
    {
        foreach (var entry in Patches)
        {
            if (entry.Value == patch)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///   Replaces a species with a different one. This is done when the class of a species needs to change
    /// </summary>
    /// <param name="old">The old species to remove</param>
    /// <param name="newSpecies">The new species to put in place of the old species</param>
    public void ReplaceSpecies(Species old, Species newSpecies)
    {
        foreach (var patch in Patches.Values)
        {
            patch.ReplaceSpecies(old, newSpecies);
        }
    }

    /// <summary>
    ///   Check if patch link <code>id1->id2</code> or <code>id2->id1</code> exists
    /// </summary>
    /// <returns>True if at least an one-direction link exists</returns>
    public bool ContainsPatchAdjacency(int id1, int id2)
    {
        return PatchAdjacencies.Contains((id1, id2)) || PatchAdjacencies.Contains((id2, id1));
    }

    /// <summary>
    ///   Check if region link <code>id1->id2</code> or <code>id2->id1</code> exists
    /// </summary>
    /// <returns>True if at least an one-direction link exists</returns>
    public bool ContainsRegionAdjacency(int id1, int id2)
    {
        return RegionAdjacencies.Contains((id1, id2)) || RegionAdjacencies.Contains((id2, id1));
    }

    public void CreateAdjacenciesFromPatchData()
    {
        foreach (var entry in Patches)
        {
            foreach (var adjacent in entry.Value.Adjacent)
            {
                if (!ContainsPatchAdjacency(entry.Value.ID, adjacent.ID))
                    PatchAdjacencies.Add((entry.Value.ID, adjacent.ID));
            }
        }

        foreach (var entry in Regions)
        {
            foreach (var adjacent in entry.Value.Adjacent.Keys)
            {
                if (!ContainsRegionAdjacency(entry.Value.ID, adjacent.ID))
                    RegionAdjacencies.Add((entry.Value.ID, adjacent.ID));
            }
        }
    }

    public void FinishLoading(ISaveContext? context)
    {
        RecreateAdjacencies();
    }

    private void RecreateAdjacencies()
    {
        foreach (var (id1, id2) in RegionAdjacencies)
        {
            var region1 = Regions[id1];
            var region2 = Regions[id2];

            region1.AddNeighbour(region2);
            region2.AddNeighbour(region1);
        }

        foreach (var (id1, id2) in PatchAdjacencies)
        {
            var patch1 = Patches[id1];
            var patch2 = Patches[id2];

            patch1.AddNeighbour(patch2);
            patch2.AddNeighbour(patch1);

            var region1 = patch1.Region;
            var region2 = patch2.Region;

            if (region1 != region2)
            {
                region1.SetConnectingPatch(region2, patch2);
                region2.SetConnectingPatch(region1, patch1);
            }
        }
    }
}
