using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   A single run of the auto-evo system happening in a background thread
/// </summary>
public class AutoEvoRun
{
    // Configuration parameters for auto evo
    // TODO: allow loading these from JSON
    private const int MutationsPerSpecies = 3;
    private const bool AllowNoMutation = true;
    private const int MoveAttemptsPerSpecies = 5;
    private const bool AllowNoMigration = true;

    private readonly AutoEvo.RunParameters parameters;

    private volatile RUN_STAGE state = RUN_STAGE.GATHERING_INFO;

    private bool started = false;
    private volatile bool running = false;
    private volatile bool finished = false;
    private volatile bool aborted = false;

    /// <summary>
    ///   Results are stored here until the simulation is complete and then applied
    /// </summary>
    private AutoEvo.RunResults results = new AutoEvo.RunResults();

    /// <summary>
    ///   -1 means not yet computed
    /// </summary>
    private volatile int totalSteps = -1;
    private volatile int completeSteps = 0;

    /// <summary>
    ///   The Species may not be messed with while running. These are queued changes that will be applied after a run
    /// </summary>
    private List<ExternalEffect> externalEffects = new List<ExternalEffect>();

    /// <summary>
    ///   Generated steps are stored here until they are executed
    /// </summary>
    private Queue<AutoEvo.IRunStep> runSteps = new Queue<AutoEvo.IRunStep>();

    public AutoEvoRun(GameWorld world)
    {
        parameters = new AutoEvo.RunParameters(world);
    }

    private enum RUN_STAGE
    {
        /// <summary>
        ///   On the first step(s) all the data is loaded (if there is a lot then it is split into multiple steps) and
        ///   the total number of steps is calculated
        /// </summary>
        GATHERING_INFO,

        /// <summary>
        ///   Steps are being executed
        /// </summary>
        STEPPING,

        /// <summary>
        ///   All the steps are done and the result is written
        /// </summary>
        ENDED,
    }

    /// <summary>
    ///   True while running
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     While auto-evo is running the patch conditions or species properties (that this run uses) MAY NOT be
    ///     changed!
    ///   </para>
    /// </remarks>
    public bool Running { get => running; private set => running = value; }

    public bool Finished { get => finished; private set => finished = value; }

    public bool Aborted { get => aborted; set => aborted = value; }

    public float CompletionFraction
    {
        get
        {
            int total = totalSteps;

            if (total <= 0)
                return 0;

            return (float)completeSteps / total;
        }
    }

    public bool WasSuccessful
    {
        get
        {
            return Finished && !Aborted;
        }
    }

    /// <summary>
    ///   a string describing the status of the simulation For example "21% done. 21/100 steps."
    /// </summary>
    public string Status
    {
        get
        {
            if (Finished)
                return "Finished.";

            if (!Running)
                return "Not running.";

            int total = totalSteps;

            if (total > 0)
            {
                var percentage = CompletionFraction * 100;

                return string.Format("{0:F1}% done. {1:n0}/{2:n0} steps.", percentage, completeSteps, total);
            }
            else
            {
                return "Starting";
            }
        }
    }

    /// <summary>
    ///   Run results after this is finished
    /// </summary>
    public AutoEvo.RunResults Results
    {
        get
        {
            if (!Finished)
                throw new InvalidOperationException("Can't get run results before finishing");

            // Aborted run gives no results
            if (Aborted)
                return null;

            return results;
        }
    }

    /// <summary>
    ///   Starts this run if not started already
    /// </summary>
    public void Start()
    {
        if (started)
            return;

        var task = new Task(Run);

        TaskExecutor.Instance.AddTask(task);
        started = true;
    }

    public void Abort()
    {
        Aborted = true;
    }

    /// <summary>
    ///   Returns true when this run is finished
    /// </summary>
    /// <param name="autostart">If set to <c>true</c> start the run if not already.</param>
    /// <returns>True when the run is complete or aborted</returns>
    public bool IsFinished(bool autostart = true)
    {
        if (autostart && !started)
            Start();

        return Finished;
    }

    public void ApplyResults()
    {
        if (!Finished || Running)
        {
            throw new InvalidOperationException("Can't apply run results before it is done");
        }

        results.ApplyResults(parameters.World, false);
    }

    /// <summary>
    ///   Applies things added by addExternalPopulationEffect
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This has to be called after this run is finished. This also applies the results so ApplyResults shouldn't be
    ///     called when using external effects.
    ///   </para>
    /// </remarks>
    public void ApplyExternalEffects()
    {
        if (externalEffects.Count > 0)
        {
            // Effects are applied in the current patch
            var currentPatch = parameters.World.Map.CurrentPatch;

            foreach (var entry in externalEffects)
            {
                try
                {
                    int currentPop = results.GetPopulationInPatch(entry.Species, currentPatch);

                    results.AddPopulationResultForSpecies(
                        entry.Species, currentPatch, currentPop + entry.Amount);
                }
                catch (Exception e)
                {
                    GD.PrintErr("External effect can't be applied: ", e);
                }
            }
        }

        results.ApplyResults(parameters.World, false);
    }

    /// <summary>
    ///   Adds an external population affecting event (player dying, reproduction, darwinian evo actions)
    /// </summary>
    /// <param name="species">The affected Species.</param>
    /// <param name="amount">The population change amount.</param>
    /// <param name="eventType">The external event type.</param>
    public void AddExternalPopulationEffect(Species species, int amount, string eventType)
    {
        externalEffects.Add(new ExternalEffect(species, amount, eventType));
    }

    /// <summary>
    ///   Makes a summary of external effects
    /// </summary>
    /// <returns>The summary of external effects.</returns>
    public string MakeSummaryOfExternalEffects()
    {
        var combinedExternalEffects = new Dictionary<Tuple<Species, string>, int>();

        foreach (var entry in externalEffects)
        {
            var key = new Tuple<Species, string>(entry.Species, entry.EventType);

            if (combinedExternalEffects.ContainsKey(key))
            {
                combinedExternalEffects[key] += entry.Amount;
            }
            else
            {
                combinedExternalEffects[key] = entry.Amount;
            }
        }

        var builder = new StringBuilder(300);

        foreach (var entry in combinedExternalEffects)
        {
            builder.Append(entry.Key.Item1.FormattedName);
            builder.Append(" population changed by ");
            builder.Append(entry.Value);
            builder.Append(" because of: ");
            builder.Append(entry.Key.Item2);
        }

        return builder.ToString();
    }

    /// <summary>
    ///   Run this instance. Should only be called in a background thread
    /// </summary>
    private void Run()
    {
        Running = true;

        bool complete = false;

        while (!Aborted && !complete)
        {
            try
            {
                complete = Step();
            }
            catch (Exception e)
            {
                Aborted = true;
                GD.PrintErr("Auto-evo failed with an exception: ", e);
            }
        }

        Running = false;
        Finished = true;
    }

    /// <summary>
    ///   Performs a single calculation step. This should be quite fast (5-20 milliseconds) in order to make aborting
    ///   work fast.
    /// </summary>
    /// <returns>True when finished</returns>
    private bool Step()
    {
        switch (state)
        {
            case RUN_STAGE.GATHERING_INFO:
                GatherInfo();

                // +2 is for this step and the result apply step
                totalSteps = runSteps.Sum((step) => step.TotalSteps) + 2;

                ++completeSteps;
                state = RUN_STAGE.STEPPING;
                return false;
            case RUN_STAGE.STEPPING:
                if (runSteps.Count < 1)
                {
                    // All steps complete
                    state = RUN_STAGE.ENDED;
                }
                else
                {
                    if (runSteps.Peek().Step(results))
                        runSteps.Dequeue();

                    ++completeSteps;
                }

                return false;
            case RUN_STAGE.ENDED:
                // Results are no longer applied here as it's easier to just apply them on the main thread while
                // moving to the editor
                ++completeSteps;
                return true;
        }

        throw new InvalidOperationException("run stage enum value not handled");
    }

    /// <summary>
    ///   The info gather phase
    /// </summary>
    private void GatherInfo()
    {
        int totalSpecies = 0;

        var alreadyHandledSpecies = new HashSet<Species>();

        var map = parameters.World.Map;

        foreach (var entry in map.Patches)
        {
            foreach (var speciesEntry in entry.Value.SpeciesInPatch)
            {
                if (alreadyHandledSpecies.Contains(speciesEntry.Key))
                    continue;

                ++totalSpecies;
                alreadyHandledSpecies.Add(speciesEntry.Key);

                // The player species doesn't get random mutations. And also doesn't
                // spread automatically
                if (speciesEntry.Key.PlayerSpecies)
                {
                }
                else
                {
                    runSteps.Enqueue(new AutoEvo.FindBestMutation(map, speciesEntry.Key, MutationsPerSpecies,
                            AllowNoMutation));
                    runSteps.Enqueue(new AutoEvo.FindBestMigration(map, speciesEntry.Key, MoveAttemptsPerSpecies,
                            AllowNoMigration));
                }
            }
        }

        // The new populations don't depend on the mutations, this is so that when
        // the player edits their species the other species they are competing
        // against are the same (so we can show some performance predictions in the
        // editor and suggested changes)
        runSteps.Enqueue(new AutoEvo.CalculatePopulation(map));

        // Adjust auto-evo results for player species
        // NOTE: currently the population change is random so it is canceled out for
        // the player
        runSteps.Enqueue(new AutoEvo.LambdaStep(
                (result) =>
                {
                    var species = parameters.World.PlayerSpecies;

                    foreach (var entry in map.Patches)
                    {
                        if (!entry.Value.SpeciesInPatch.ContainsKey(species))
                            continue;

                        result.AddPopulationResultForSpecies(species, entry.Value,
                            entry.Value.GetSpeciesPopulation(species));
                    }
                }));
    }
}
