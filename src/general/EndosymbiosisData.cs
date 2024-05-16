using System;
using System.Collections.Generic;
using Godot;
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

    public OrganelleDefinition GetOrganelleTypeForInProgressSymbiosis(Species symbiontSpecies)
    {
        if (StartedEndosymbiosis == null)
            throw new InvalidOperationException("No in-progress endosymbiosis operations, can't find type");

        if (StartedEndosymbiosis.Species != symbiontSpecies)
            throw new ArgumentException("Specified species is not an in-progress endosymbiont");

        return StartedEndosymbiosis.TargetOrganelle;
    }

    /// <summary>
    ///   Increments progress by one for the given species
    /// </summary>
    /// <returns>True on success</returns>
    public bool ReportEndosymbiosisProgress(Species symbiontSpecies)
    {
        if (StartedEndosymbiosis == null)
            return false;

        if (StartedEndosymbiosis.Species != symbiontSpecies)
        {
            GD.PrintErr("Endosymbiont to update progress for doesn't match the in-progress endosymbiosis operation");
            return false;
        }

        StartedEndosymbiosis.CurrentlyAcquiredCount += 1;

        // Clamp to max to for example prevent progress indicators from showing more than full
        if (StartedEndosymbiosis.CurrentlyAcquiredCount > StartedEndosymbiosis.RequiredCount)
            StartedEndosymbiosis.CurrentlyAcquiredCount = StartedEndosymbiosis.RequiredCount;

        return true;
    }

    public bool StartEndosymbiosis(int targetSpecies, OrganelleDefinition organelle, int cost)
    {
        if (StartedEndosymbiosis != null)
        {
            GD.PrintErr("Endosymbiosis is already in progress");
            return false;
        }

        if (cost < 1)
            throw new ArgumentException("Cost should be at least one for endosymbiosis");

        // Need to resolve the species before can start
        foreach (var candidate in EngulfedSpecies)
        {
            if (candidate.Key.ID != targetSpecies || candidate.Value <= 0)
                continue;

            StartedEndosymbiosis = new InProgressEndosymbiosis(candidate.Key, cost, organelle);
            return true;
        }

        return false;
    }

    public bool CancelEndosymbiosisTarget(int targetSpeciesId)
    {
        if (StartedEndosymbiosis == null || StartedEndosymbiosis.Species.ID != targetSpeciesId)
            return false;

        StartedEndosymbiosis = null;
        return true;
    }

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
        ///   The current progress towards <see cref="RequiredCount"/>
        /// </summary>
        [JsonProperty]
        public int CurrentlyAcquiredCount { get; set; }

        /// <summary>
        ///   Organelle that will be granted upon finishing the endosymbiosis
        /// </summary>
        [JsonProperty]
        public OrganelleDefinition TargetOrganelle { get; } = targetOrganelle;

        [JsonIgnore]
        public bool IsComplete => CurrentlyAcquiredCount >= RequiredCount;

        public InProgressEndosymbiosis Clone()
        {
            return new InProgressEndosymbiosis(Species, RequiredCount, TargetOrganelle)
            {
                CurrentlyAcquiredCount = CurrentlyAcquiredCount,
            };
        }
    }
}
