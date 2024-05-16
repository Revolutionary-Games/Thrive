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

            if (Endosymbionts != null)
            {
                foreach (var endosymbiont in Endosymbionts)
                {
                    if (endosymbiont.OriginallyFromSpecies.ID == targetSpecies)
                    {
                        GD.PrintErr("Trying to form symbiosis with something that is already an endosymbiont");
                        return false;
                    }
                }
            }

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

    public InProgressEndosymbiosis MarkEndosymbiosisDone(InProgressEndosymbiosis endosymbiosisToComplete)
    {
        if (StartedEndosymbiosis == null)
            throw new InvalidOperationException("No in-progress endosymbiosis");

        if (Endosymbionts != null)
        {
            foreach (var endosymbiont in Endosymbionts)
            {
                if (endosymbiont.OriginallyFromSpecies == endosymbiosisToComplete.Species)
                    throw new ArgumentException("Target endosymbiont species already exists");
            }
        }

        var endosymbiosis = StartedEndosymbiosis;

        if (!endosymbiosis.IsComplete)
            GD.PrintErr("Marking endosymbiosis that is not complete as done");

        if (endosymbiosis != endosymbiosisToComplete)
            GD.PrintErr("Wrong endosymbiosis object passed to marking it as complete");

        StartedEndosymbiosis = null;

        Endosymbionts ??= new List<Endosymbiont>();
        Endosymbionts.Add(new Endosymbiont(endosymbiosisToComplete.TargetOrganelle, endosymbiosisToComplete.Species));

        return endosymbiosis;
    }

    /// <summary>
    ///   Checks if there is an already complete endosymbiosis operation that just needs to be finished
    /// </summary>
    /// <returns>True when there is a complete pending operation</returns>
    public bool HasCompleteEndosymbiosis()
    {
        if (StartedEndosymbiosis == null)
            return false;

        return StartedEndosymbiosis.IsComplete;
    }

    /// <summary>
    ///   Resumes a previous cancelled endosymbiosis progress. Also removes the data from completed endosymbiosis
    ///   operations.
    /// </summary>
    /// <param name="inProgressDataToResume">The progress to resume</param>
    /// <returns>
    ///   The previously active endosymbiosis progress that was replaced, null if there was none or if it matched
    ///   <see cref="inProgressDataToResume"/>
    /// </returns>
    public InProgressEndosymbiosis? ResumeEndosymbiosisProcess(InProgressEndosymbiosis inProgressDataToResume)
    {
        if (StartedEndosymbiosis == inProgressDataToResume)
            return null;

        bool removed = false;

        if (Endosymbionts != null)
        {
            foreach (var endosymbiont in Endosymbionts)
            {
                if (endosymbiont.OriginallyFromSpecies == inProgressDataToResume.Species)
                {
                    removed = Endosymbionts.Remove(endosymbiont);
                    break;
                }
            }
        }

        if (!removed)
        {
            GD.PrintErr("Endosymbiont result should exist to remove on resuming the process");
        }

        var previous = StartedEndosymbiosis;
        StartedEndosymbiosis = inProgressDataToResume;
        return previous;
    }

    /// <summary>
    ///   Checks if a species is already an endosymbiont
    /// </summary>
    /// <param name="speciesToCheck">Species to check</param>
    /// <returns>True when already an endosymbiont</returns>
    public bool IsEndosymbiontAlready(Species speciesToCheck)
    {
        if (Endosymbionts == null)
            return false;

        foreach (var endosymbiont in Endosymbionts)
        {
            if (endosymbiont.OriginallyFromSpecies == speciesToCheck)
                return true;
        }

        return false;
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
