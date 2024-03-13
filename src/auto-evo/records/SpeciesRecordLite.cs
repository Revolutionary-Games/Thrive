namespace AutoEvo;

using System;
using Newtonsoft.Json;

/// <summary>
///   Species mutation and population data from a single generation, with or without the full species.
/// </summary>
public class SpeciesRecordLite : SpeciesRecord
{
    [JsonConstructor]
    public SpeciesRecordLite(Species? species, long population, uint? mutatedPropertiesID = null,
        uint? splitFromID = null) : base(population, mutatedPropertiesID, splitFromID)
    {
        if (species == null && (mutatedPropertiesID != null || splitFromID != null))
            throw new InvalidOperationException("Species which newly mutated or split off must have species data");

        Species = species;
    }

    /// <summary>
    ///   Full species data for this species. If null, species is assumed to have full data earlier in the game
    ///   history.
    /// </summary>
    [JsonProperty]
    public Species? Species { get; private set; }
}
