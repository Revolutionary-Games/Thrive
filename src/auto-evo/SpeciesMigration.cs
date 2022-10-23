namespace AutoEvo
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    ///   Data for a Species migration between two patches
    /// </summary>
    [JsonObject(IsReference = true)]
    [UseThriveSerializer]
    public class SpeciesMigration
    {
        [JsonProperty]
        public Patch From;

        [JsonProperty]
        public Patch To;

        [JsonProperty]
        public long Population;

        [JsonConstructor]
        public SpeciesMigration(Patch from, Patch to, long population)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
            Population = population;
        }
    }
}
