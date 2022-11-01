namespace AutoEvo
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    ///   Record of Auto-Evo results and species data for a given generation.
    /// </summary>
    public class GenerationRecord
    {
        [JsonConstructor]
        public GenerationRecord(int generation, double timeElapsed, RunResults autoEvoResult,
            Dictionary<uint, Species> allSpecies)
        {
            Generation = generation;
            TimeElapsed = timeElapsed;
            AutoEvoResults = autoEvoResult;
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
        ///   Auto-Evo results for this generation.
        /// </summary>
        [JsonProperty]
        public RunResults AutoEvoResults { get; private set; }

        /// <summary>
        ///   All species data for this generation. Species are cloned to preserve their state at this time.
        /// </summary>
        [JsonProperty]
        public Dictionary<uint, Species> AllSpecies { get; private set; }
    }
}
