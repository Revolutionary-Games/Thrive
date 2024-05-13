using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Endosymbiosis data for a single <see cref="Species"/> tracking its relationships to other species
/// </summary>
public class EndosymbiosisData
{
    /// <summary>
    ///   Species that have been engulfed (and how many times) that are candidates for endosymbiosis (or being tracked
    ///   progress towards finalizing endosymbiosis)
    /// </summary>
    [JsonProperty]
    public Dictionary<Species, int> EngulfedSpecies { get; private set; } = new();

    /// <summary>
    ///   Currently ongoing endosymbiosis operation
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: determine if it would be better to have this be a list to allow multiple endosymbiosis operations
    ///     at once
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public InProgressEndosymbiosis? StartedEndosymbiosis { get; private set; }

    /// <summary>
    ///   Completed endosymbiosis operations
    /// </summary>
    [JsonProperty]
    public List<Endosymbiont>? Endosymbionts { get; private set; }

    public EndosymbiosisData Clone()
    {
        var cloned = new EndosymbiosisData
        {
            StartedEndosymbiosis = StartedEndosymbiosis?.Clone(),
        };

        foreach (var entry in EngulfedSpecies)
        {
            cloned.EngulfedSpecies.Add(entry.Key, entry.Value);
        }

        if (Endosymbionts != null)
        {
            cloned.Endosymbionts = new List<Endosymbiont>();

            foreach (var endosymbiont in Endosymbionts)
            {
                cloned.Endosymbionts.Add(endosymbiont.Clone());
            }
        }

        return cloned;
    }

    public class InProgressEndosymbiosis(Species species, int requiredCount, OrganelleDefinition targetOrganelle)
    {
        [JsonProperty]
        public Species Species { get; } = species;

        /// <summary>
        ///   How many times the target species must be engulfed to complete the endosymbiosis
        /// </summary>
        [JsonProperty]
        public int RequiredCount { get; } = requiredCount;

        /// <summary>
        ///   Organelle that will be granted upon finishing the endosymbiosis
        /// </summary>
        [JsonProperty]
        public OrganelleDefinition TargetOrganelle { get; } = targetOrganelle;

        public InProgressEndosymbiosis Clone()
        {
            return new InProgressEndosymbiosis(Species, RequiredCount, TargetOrganelle);
        }
    }
}
