namespace AutoEvo;

using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Record of Auto-Evo results and species data for a given generation.
/// </summary>
public class GenerationRecord
{
    [JsonConstructor]
    public GenerationRecord(double timeElapsed, Dictionary<uint, SpeciesRecordLite> allSpeciesData)
    {
        TimeElapsed = timeElapsed;
        AllSpeciesData = allSpeciesData;
    }

    /// <summary>
    ///   Total in-game time elapsed since the world's beginning.
    /// </summary>
    [JsonProperty]
    public double TimeElapsed { get; private set; }

    /// <summary>
    ///   Data for all species this generation, along with population and mutation data.
    /// </summary>
    [JsonProperty]
    public Dictionary<uint, SpeciesRecordLite> AllSpeciesData { get; private set; }

    /// <summary>
    ///   Replaces a null species record with the latest non-null record from a previous generation. Used to fill
    ///   missing species data for species who didn't change and thus didn't have their full data saved.
    /// </summary>
    /// <param name="speciesID">ID for the species to update</param>
    /// <param name="currentGeneration">The generation of the species we want to update</param>
    /// <param name="generationHistory">Full generation history for the game</param>
    /// <returns>Species record with full species data</returns>
    public static SpeciesRecordFull GetFullSpeciesRecord(uint speciesID, int currentGeneration,
        Dictionary<int, GenerationRecord> generationHistory)
    {
        var speciesRecord = generationHistory[currentGeneration].AllSpeciesData[speciesID];
        Species? updatedSpecies = null;

        // Loop through previous generations until we find a non-null record and update the species
        for (int i = currentGeneration; i >= 0; i--)
        {
            if (generationHistory[i].AllSpeciesData.TryGetValue(speciesID, out var candidateRecord) &&
                candidateRecord.Species != null)
            {
                updatedSpecies = candidateRecord.Species;
                break;
            }
        }

        if (updatedSpecies == null)
            throw new KeyNotFoundException($"No species with ID {speciesID} found in generation history");

        return new SpeciesRecordFull(updatedSpecies,
            speciesRecord.Population,
            speciesRecord.MutatedPropertiesID,
            speciesRecord.SplitFromID);
    }

    /// <summary>
    ///   Replaces species data for a given species in this generation. Primarily used for updating data for the
    ///   player species once the player has left the editor.
    /// </summary>
    /// <param name="species">Updated species</param>
    public void UpdateSpeciesData(Species species)
    {
        if (AllSpeciesData.TryGetValue(species.ID, out var existing))
        {
            AllSpeciesData[species.ID] = new SpeciesRecordLite((Species)species.Clone(), existing.Population,
                existing.MutatedPropertiesID, existing.SplitFromID);
        }
        else
        {
            GD.PrintErr($"Unable to find species with ID {species.ID} in existing species");
        }
    }
}
