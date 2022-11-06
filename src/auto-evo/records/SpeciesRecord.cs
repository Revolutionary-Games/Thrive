namespace AutoEvo
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    ///   Species mutation and population data from a single generation, with or without the full species.
    /// </summary>
    public class SpeciesRecord
    {
        [JsonConstructor]
        public SpeciesRecord(Species? species,
            long population,
            uint? mutatedPropertiesID = null,
            uint? splitFromID = null)
        {
            if (species == null && (mutatedPropertiesID != null || splitFromID != null))
                throw new InvalidOperationException("Species which newly mutated or split off must have species data");

            Species = species;
            Population = population;
            MutatedPropertiesID = mutatedPropertiesID;
            SplitFromID = splitFromID;
        }

        /// <summary>
        ///   Full species data for this species. If null, species is assumed to have full data earlier in the game
        ///   history.
        /// </summary>
        [JsonProperty]
        public Species? Species { get; set; }

        /// <summary>
        ///   Species population for this generation.
        /// </summary>
        [JsonProperty]
        public long Population { get; private set; }

        /// <summary>
        ///   ID of the species this species mutated from. If null, this species did not mutate this generation.
        /// </summary>
        [JsonProperty]
        public uint? MutatedPropertiesID { get; private set; }

        /// <summary>
        ///   ID of the species this species speciated from. If null, this species did not appear this generation.
        /// </summary>
        [JsonProperty]
        public uint? SplitFromID { get; private set; }
    }
}
