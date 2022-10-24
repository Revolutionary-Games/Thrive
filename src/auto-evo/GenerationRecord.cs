namespace AutoEvo
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    [JsonObject(IsReference = true)]
    [UseThriveSerializer]
    public class GenerationRecord
    {
        [JsonProperty]
        public int Generation;

        [JsonProperty]
        public double TimeElapsed;

        [JsonProperty]
        public RunResults AutoEvoResult = null!;

        [JsonProperty]
        public Dictionary<uint, Species> AllSpecies = null!;
    }
}
