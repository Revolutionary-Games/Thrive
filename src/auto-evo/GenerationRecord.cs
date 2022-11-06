namespace AutoEvo
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Record of Auto-Evo results and species data for a given generation.
    /// </summary>
    public class GenerationRecord
    {
        [JsonConstructor]
        public GenerationRecord(int generation,
            double timeElapsed,
            Dictionary<Species, RunResults.SpeciesResult> autoEvoResults,
            Dictionary<uint, Species> allSpecies)
        {
            Generation = generation;
            TimeElapsed = timeElapsed;
            AutoEvoResults = autoEvoResults;
            AllSpecies = allSpecies;
        }

        /// <summary>
        ///   Player generation in the game (starting at zero).
        /// </summary>
        [JsonProperty]
        public int Generation { get; private set; }

        /// <summary>
        ///   Total in-game time elapsed since its beginning.
        /// </summary>
        [JsonProperty]
        public double TimeElapsed { get; private set; }

        /// <summary>
        ///   Auto-Evo results for this generation. Species are cloned to preserve their state at this time.
        /// </summary>
        [JsonProperty]
        public Dictionary<Species, RunResults.SpeciesResult> AutoEvoResults { get; private set; }

        /// <summary>
        ///   All species data for this generation. Species are cloned to preserve their state at this time. Note
        ///   this does not include species which went extinct this generation.
        /// </summary>
        [JsonProperty]
        public Dictionary<uint, Species> AllSpecies { get; private set; }

        /// <summary>
        ///   Replaces species data for a given species in this generation. Primarily used for updating data for the
        ///   player species once the player has left the editor.
        /// </summary>
        /// <param name="species">Updated species</param>
        public void UpdateSpeciesData(Species species)
        {
            if (AllSpecies.ContainsKey(species.ID) && AutoEvoResults.Any(r => r.Key.ID == species.ID))
            {
                var speciesClone = (Species)species.Clone();
                AllSpecies[species.ID] = speciesClone;
                AutoEvoResults = AutoEvoResults.ToDictionary(
                    r => r.Key.ID == species.ID ? speciesClone : r.Key,
                    r => r.Key.ID == species.ID ? r.Value.Clone(species) : r.Value);
            }
            else
            {
                GD.PrintErr($"Unable to find species with ID {species.ID} in existing species");
            }
        }
    }
}
