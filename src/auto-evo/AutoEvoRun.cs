using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoEvo;
using Godot;
using Thread = System.Threading.Thread;

/// <summary>
///   A single run of the auto-evo system happening in a background thread
/// </summary>
public class AutoEvoRun
{
    private readonly RunParameters parameters;

    /// <summary>
    ///   Results are stored here until the simulation is complete and then applied
    /// </summary>
    private readonly RunResults results = new RunResults();

    /// <summary>
    ///   Generated steps are stored here until they are executed
    /// </summary>
    private readonly Queue<IRunStep> runSteps = new Queue<IRunStep>();

    private readonly List<Task> concurrentStepTasks = new List<Task>();

    private volatile RunStage state = RunStage.GatheringInfo;

    private bool started;
    private volatile bool running;
    private volatile bool finished;
    private volatile bool aborted;

    /// <summary>
    ///   -1 means not yet computed
    /// </summary>
    private volatile int totalSteps = -1;

    private int completeSteps;

    public AutoEvoRun(GameWorld world)
    {
        parameters = new RunParameters(world);
    }

    private enum RunStage
    {
        /// <summary>
        ///   On the first step(s) all the data is loaded (if there is a lot then it is split into multiple steps) and
        ///   the total number of steps is calculated
        /// </summary>
        GatheringInfo,

        /// <summary>
        ///   Steps are being executed
        /// </summary>
        Stepping,

        /// <summary>
        ///   All the steps are done and the result is written
        /// </summary>
        Ended,
    }

    /// <summary>
    ///   The Species may not be messed with while running. These are queued changes that will be applied after a run
    /// </summary>
    public List<ExternalEffect> ExternalEffects { get; } = new List<ExternalEffect>();

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

    /// <summary>
    ///   The total duration auto-evo processing took
    /// </summary>
    public TimeSpan? RunDuration { get; private set; }

    public float CompletionFraction
    {
        get
        {
            int total = totalSteps;

            if (total <= 0)
                return 0;

            return (float)CompleteSteps / total;
        }
    }

    public int CompleteSteps => Thread.VolatileRead(ref completeSteps);

    public bool WasSuccessful => Finished && !Aborted;

    /// <summary>
    ///   If true the auto evo uses all available executor threads by running more concurrent concurrentStepTasks
    /// </summary>
    public bool FullSpeed { get; set; }

    /// <summary>
    ///   a string describing the status of the simulation For example "21% done. 21/100 steps."
    /// </summary>
    public string Status
    {
        get
        {
            if (Aborted)
                return TranslationServer.Translate("ABORTED");

            if (Finished)
                return TranslationServer.Translate("FINISHED");

            if (!Running)
                return TranslationServer.Translate("NOT_RUNNING");

            int total = totalSteps;

            if (total > 0)
            {
                var percentage = CompletionFraction * 100;

                // {0:F1}% done. {1:n0}/{2:n0} steps.
                return string.Format(CultureInfo.CurrentCulture,
                    TranslationServer.Translate("AUTO-EVO_STEPS_DONE"),
                    percentage, CompleteSteps, total);
            }

            return TranslationServer.Translate("STARTING");
        }
    }

    /// <summary>
    ///   Run results after this is finished
    /// </summary>
    public RunResults Results
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
    /// <param name="autoStart">If set to <c>true</c> start the run if not already.</param>
    /// <returns>True when the run is complete or aborted</returns>
    public bool IsFinished(bool autoStart = true)
    {
        if (autoStart && !started)
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
        if (ExternalEffects.Count > 0)
        {
            // Effects are applied in the current patch
            var currentPatch = parameters.World.Map.CurrentPatch;

            foreach (var entry in ExternalEffects)
            {
                try
                {
                    long currentPop = results.GetPopulationInPatch(entry.Species, currentPatch);

                    results.AddPopulationResultForSpecies(
                        entry.Species, currentPatch, (int)(currentPop * entry.Coefficient) + entry.Constant);
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
    /// <param name="constant">The population change amount (constant part).</param>
    /// <param name="coefficient">The population change amount (coefficient part).</param>
    /// <param name="eventType">The external event type.</param>
    public void AddExternalPopulationEffect(Species species, int constant, float coefficient, string eventType)
    {
        ExternalEffects.Add(new ExternalEffect(species, constant, coefficient, eventType));
    }

    /// <summary>
    ///   Makes a summary of external effects
    /// </summary>
    /// <returns>The summary of external effects.</returns>
    public string MakeSummaryOfExternalEffects()
    {
        var combinedExternalEffects = new Dictionary<Tuple<Species, string>, long>();

        foreach (var entry in ExternalEffects)
        {
            var key = new Tuple<Species, string>(entry.Species, entry.EventType);

            if (combinedExternalEffects.ContainsKey(key))
            {
                combinedExternalEffects[key] +=
                    entry.Constant + (long)(entry.Species.Population * entry.Coefficient) - entry.Species.Population;
            }
            else
            {
                combinedExternalEffects[key] =
                    entry.Constant + (int)(entry.Species.Population * entry.Coefficient) - entry.Species.Population;
            }
        }

        var builder = new StringBuilder(300);

        foreach (var entry in combinedExternalEffects)
        {
            // entry.Value is the amount, Item2 is the reason string
            builder.Append(string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("AUTO-EVO_POPULATION_CHANGED"),
                entry.Key.Item1.FormattedName, entry.Value, entry.Key.Item2));
            builder.Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    ///   Run this instance. Should only be called in a background thread
    /// </summary>
    private void Run()
    {
        var timer = new Stopwatch();
        timer.Start();

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

        RunDuration = timer.Elapsed;
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
            case RunStage.GatheringInfo:
                GatherInfo();

                // +2 is for this step and the result apply step
                totalSteps = runSteps.Sum(step => step.TotalSteps) + 2;

                Interlocked.Increment(ref completeSteps);
                state = RunStage.Stepping;
                return false;
            case RunStage.Stepping:
                if (runSteps.Count < 1)
                {
                    // All steps complete
                    state = RunStage.Ended;
                }
                else
                {
                    if (FullSpeed)
                    {
                        // Try to use extra threads to speed this up
                        // If we ever want to use background processing in a loading screen to do something time
                        // sensitive while auto-evo runs this value needs to be reduced
                        int maxTasksAtOnce = 1000;

                        while (runSteps.Peek()?.CanRunConcurrently == true && maxTasksAtOnce > 0)
                        {
                            var step = runSteps.Dequeue();

                            concurrentStepTasks.Add(new Task(() => RunSingleStepToCompletion(step)));
                            --maxTasksAtOnce;
                        }

                        if (concurrentStepTasks.Count < 1)
                        {
                            // No steps that can run concurrently, need to run just a normal run
                            NormalRunPartOfNextStep();
                        }
                        else
                        {
                            TaskExecutor.Instance.RunTasks(concurrentStepTasks, true);
                            concurrentStepTasks.Clear();
                        }
                    }
                    else
                    {
                        NormalRunPartOfNextStep();
                    }
                }

                return false;
            case RunStage.Ended:
                // Results are no longer applied here as it's easier to just apply them on the main thread while
                // moving to the editor
                Interlocked.Increment(ref completeSteps);
                return true;
        }

        throw new InvalidOperationException("run stage enum value not handled");
    }

    private void NormalRunPartOfNextStep()
    {
        if (runSteps.Peek().RunStep(results))
            runSteps.Dequeue();

        Interlocked.Increment(ref completeSteps);
    }

    private void RunSingleStepToCompletion(IRunStep step)
    {
        int steps = 0;

        // This condition is here to allow abandoning auto-evo runs quickly
        while (!Aborted)
        {
            ++steps;

            if (step.RunStep(results))
                break;
        }

        // Doing the steps counting this way is slightly faster than an increment after each step
        Interlocked.Add(ref completeSteps, steps);
    }

    /// <summary>
    ///   The info gather phase
    /// </summary>
    private void GatherInfo()
    {
        var random = new Random();

        var alreadyHandledSpecies = new HashSet<Species>();

        var map = parameters.World.Map;

        var autoEvoConfiguration = SimulationParameters.Instance.AutoEvoConfiguration;

        foreach (var entry in map.Patches)
        {
            foreach (var speciesEntry in entry.Value.SpeciesInPatch)
            {
                if (alreadyHandledSpecies.Contains(speciesEntry.Key))
                    continue;

                alreadyHandledSpecies.Add(speciesEntry.Key);

                // The player species doesn't get random mutations. And also doesn't spread automatically
                if (speciesEntry.Key.PlayerSpecies)
                {
                }
                else
                {
                    runSteps.Enqueue(new FindBestMutation(map, speciesEntry.Key,
                        autoEvoConfiguration.MutationsPerSpecies,
                        autoEvoConfiguration.AllowNoMigration,
                        autoEvoConfiguration.SpeciesSplitByMutationThresholdPopulationFraction,
                        autoEvoConfiguration.SpeciesSplitByMutationThresholdPopulationAmount));

                    runSteps.Enqueue(new FindBestMigration(map, speciesEntry.Key, random,
                        autoEvoConfiguration.MoveAttemptsPerSpecies,
                        autoEvoConfiguration.AllowNoMigration));
                }
            }

            if (entry.Value.SpeciesInPatch.Count < autoEvoConfiguration.LowBiodiversityLimit &&
                random.NextDouble() < autoEvoConfiguration.BiodiversityAttemptFillChance)
            {
                runSteps.Enqueue(new IncreaseBiodiversity(map, entry.Value, random, autoEvoConfiguration));
            }
        }

        // The new populations don't depend on the mutations, this is so that when
        // the player edits their species the other species they are competing
        // against are the same (so we can show some performance predictions in the
        // editor and suggested changes)
        // Concurrent run is false here just to be safe, and as this is a single step this doesn't matter much
        runSteps.Enqueue(new CalculatePopulation(map) { CanRunConcurrently = false });

        // Due to species splitting migrations may end up being invalid
        // TODO: should this also adjust / remove migrations that are no longer possible due to updated population
        // numbers
        runSteps.Enqueue(new RemoveInvalidMigrations(alreadyHandledSpecies));

        // Adjust auto-evo results for player species
        runSteps.Enqueue(new LambdaStep(
            result =>
            {
                var species = parameters.World.PlayerSpecies;

                foreach (var entry in map.Patches)
                {
                    if (!entry.Value.SpeciesInPatch.ContainsKey(species))
                        continue;

                    // Going extinct in patch is not adjusted, because the minimum viable population clamping is
                    // performed already so we don't want to undo that
                    var resultPopulation = result.GetPopulationInPatch(species, entry.Value);

                    if (resultPopulation == 0)
                        continue;

                    // Adjust to the specified fraction of the full population change
                    var previousPopulation = entry.Value.GetSpeciesPopulation(species);
                    var change = resultPopulation - previousPopulation;

                    change = (long)Math.Round(change * Constants.AUTO_EVO_PLAYER_STRENGTH_FRACTION);

                    result.AddPopulationResultForSpecies(species, entry.Value, previousPopulation + change);
                }
            }));
    }
}
