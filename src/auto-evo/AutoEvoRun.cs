using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    protected readonly IAutoEvoConfiguration configuration;
    protected readonly AutoEvoGlobalCache globalCache;

    /// <summary>
    ///   Results are stored here until the simulation is complete and then applied
    /// </summary>
    private readonly RunResults results = new();

    /// <summary>
    ///   Generated steps are stored here until they are executed
    /// </summary>
    private readonly Queue<IRunStep> runSteps = new();

    private readonly List<Task> concurrentStepTasks = new();

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

    public AutoEvoRun(GameWorld world, AutoEvoGlobalCache globalCache)
    {
        Parameters = new RunParameters(world);
        configuration = world.WorldSettings.AutoEvoConfiguration;
        this.globalCache = globalCache;
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
    public List<ExternalEffect> ExternalEffects { get; } = new();

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
    public TimeSpan RunDuration { get; private set; } = TimeSpan.Zero;

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
                return Localization.Translate("ABORTED_DOT");

            if (Finished)
                return Localization.Translate("FINISHED_DOT");

            if (!started)
                return Localization.Translate("NOT_STARTED_DOT");

            int total = totalSteps;

            if (total > 0)
            {
                var percentage = CompletionFraction * 100;

                // {0:F1}% done. {1:n0}/{2:n0} steps. [Paused.]
                return Localization.Translate("AUTO-EVO_STEPS_DONE").FormatSafe(percentage, CompleteSteps, total)
                    + (Running ? string.Empty : " " + Localization.Translate("OPERATION_PAUSED_DOT"));
            }

            return Localization.Translate("STARTING");
        }
    }

    /// <summary>
    ///   Run results after this is finished
    /// </summary>
    public RunResults? Results
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

    protected RunParameters Parameters { get; }

    public static AutoEvoGlobalCache GetGlobalCache(AutoEvoRun? autoEvoRun, WorldGenerationSettings worldSettings)
    {
        if (autoEvoRun != null)
        {
            return autoEvoRun.globalCache;
        }

        return new AutoEvoGlobalCache(worldSettings);
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

    public void OneStep()
    {
        if (Running)
            return;

        started = true;

        var timer = new Stopwatch();
        timer.Start();

        Running = true;

        try
        {
            if (Step())
                Finished = true;
        }
        catch (Exception e)
        {
            Aborted = true;
            GD.PrintErr("Auto-evo failed with an exception: ", e);
        }

        Running = false;

        RunDuration += timer.Elapsed;
    }

    public void Continue()
    {
        if (Running)
            return;

        Running = true;

        var task = new Task(Run);

        TaskExecutor.Instance.AddTask(task);
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

    /// <summary>
    ///   Applies computed auto-evo results to the world.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This has to be called after this run is finished.
    ///     <see cref="CalculateAndApplyFinalExternalEffectSizes"/> must be called first,
    ///     that should be called even before generating the result summaries to make sure they are accurate.
    ///   </para>
    /// </remarks>
    public void ApplyAllResults(bool playerCantGoExtinct)
    {
        if (!Finished || Running)
        {
            throw new InvalidOperationException("Can't apply run results before it is done");
        }

        results.ApplyResults(Parameters.World, false);

        UpdateMap(playerCantGoExtinct);
    }

    /// <summary>
    ///   Calculates the final sizes of external effects. This is a separate method to unify the logic and avoid
    ///   bugs regarding when results are applied and what base populations are used in the external effects.
    ///   Must be called before <see cref="RunResults.MakeSummary"/> is called.
    /// </summary>
    public void CalculateAndApplyFinalExternalEffectSizes()
    {
        if (ExternalEffects.Count < 1)
            return;

        // For subsequent effects to work we need to track the changes we do
        var adjustedPopulations = new Dictionary<(Species, Patch), long>();

        foreach (var effect in ExternalEffects)
        {
            var key = (effect.Species, effect.Patch);

            // If the species is extinct, don't try to calculate the coefficient values as that will cause an error
            if (!results.SpeciesHasResults(effect.Species))
            {
                effect.Coefficient = 1;
                continue;
            }

            if (!adjustedPopulations.TryGetValue(key, out var population))
            {
                population = results.GetPopulationInPatchIfExists(effect.Species, effect.Patch) ?? 0;
            }

            var newPopulation = (long)(population * effect.Coefficient) + effect.Constant;

            var change = newPopulation - population;

            // This *probably* can't overflow, but just in case check for that case
            if (change > int.MaxValue)
            {
                GD.PrintErr("Converting external effect caused a data overflow! We need to change " +
                    "external effects to use longs.");
                change = int.MaxValue;
            }

            effect.Coefficient = 1;
            effect.Constant = (int)change;

            adjustedPopulations[key] = newPopulation;
        }

        ApplyExternalEffects();
    }

    /// <summary>
    ///   Adds an external population affecting event (player dying, reproduction, darwinian evo actions)
    /// </summary>
    /// <param name="species">The affected Species.</param>
    /// <param name="constant">The population change amount (constant part).</param>
    /// <param name="coefficient">The population change amount (coefficient part).</param>
    /// <param name="eventType">The external event type.</param>
    /// <param name="patch">The patch this effect affects.</param>
    public void AddExternalPopulationEffect(Species species, int constant, float coefficient, string eventType,
        Patch patch)
    {
        if (string.IsNullOrEmpty(eventType))
            throw new ArgumentException("external effect type is required", nameof(eventType));

        ExternalEffects.Add(new ExternalEffect(species, constant, coefficient, eventType, patch));
    }

    /// <summary>
    ///   Makes a summary of external effects
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     <see cref="CalculateAndApplyFinalExternalEffectSizes"/> needs to be called before this is called to have
    ///     accurate numbers
    ///   </para>
    /// </remarks>
    /// <returns>The summary of external effects.</returns>
    public LocalizedStringBuilder MakeSummaryOfExternalEffects()
    {
        var combinedExternalEffects = new Dictionary<(Species Species, string Event, Patch Patch), long>();

        foreach (var entry in ExternalEffects)
        {
            var key = (entry.Species, entry.EventType, entry.Patch);

            combinedExternalEffects.TryGetValue(key, out var existingEffectAmount);

            // We can ignore coefficients because we trust that CalculateFinalExternalEffectSizes has been called first
            // and so we also don't need to

            combinedExternalEffects[key] = existingEffectAmount + entry.Constant;
        }

        var builder = new LocalizedStringBuilder(300);

        foreach (var entry in combinedExternalEffects)
        {
            builder.Append(new LocalizedString("AUTO-EVO_POPULATION_CHANGED_2",
                entry.Key.Species.FormattedNameBbCode, entry.Value, entry.Key.Patch.Name, entry.Key.Event));

            builder.Append('\n');
        }

        return builder;
    }

    /// <summary>
    ///   The info gather phase
    /// </summary>
    protected virtual void GatherInfo(Queue<IRunStep> steps)
    {
        var map = Parameters.World.Map;
        var worldSettings = Parameters.World.WorldSettings;

        var autoEvoConfiguration = configuration;

        var allSpecies = new HashSet<Species>();

        var generateMicheCache = new SimulationCache(worldSettings);

        foreach (var entry in map.Patches)
        {
            steps.Enqueue(new GenerateMiche(entry.Value, generateMicheCache, globalCache));

            foreach (var species in entry.Value.SpeciesInPatch)
            {
                allSpecies.Add(species.Key);
            }
        }

        foreach (var entry in map.Patches)
        {
            steps.Enqueue(new ModifyExistingSpecies(entry.Value, new SimulationCache(worldSettings), worldSettings));

            for (int i = 0; i < Constants.AUTO_EVO_MOVE_ATTEMPTS; ++i)
            {
                steps.Enqueue(new MigrateSpecies(entry.Value, new SimulationCache(worldSettings)));
            }
        }

        steps.Enqueue(new RemoveInvalidMigrations(allSpecies));

        // The new populations don't depend on the mutations, this is so that when
        // the player edits their species the other species they are competing
        // against are the same (so we can show some performance predictions in the
        // editor and suggested changes)
        // Concurrent run is false here just to be safe, and as this is a single step this doesn't matter much
        steps.Enqueue(new CalculatePopulation(autoEvoConfiguration, worldSettings, map, null, null, true)
            { CanRunConcurrently = false });

        AddPlayerSpeciesPopulationChangeClampStep(steps, map, Parameters.World.PlayerSpecies);
    }

    /// <summary>
    ///   Adds a step that adjusts the player species population results
    /// </summary>
    /// <param name="steps">The list of steps to add the adjust step to</param>
    /// <param name="map">Used to get list of patches to act on</param>
    /// <param name="playerSpecies">The species the player adjustment is performed on, if null, nothing is done</param>
    /// <param name="previousPopulationFrom">
    ///   This is the species from which the previous populations are read through. If null
    ///   <see cref="playerSpecies"/> is used instead
    /// </param>
    protected void AddPlayerSpeciesPopulationChangeClampStep(Queue<IRunStep> steps, PatchMap map,
        Species? playerSpecies,
        Species? previousPopulationFrom = null)
    {
        if (playerSpecies == null)
            return;

        steps.Enqueue(new LambdaStep(result =>
        {
            if (!result.SpeciesHasResults(playerSpecies))
            {
                GD.Print("Player species has no auto-evo results, creating blank results to avoid problems");
                result.AddPlayerSpeciesBlankResult(playerSpecies, map.Patches.Values);
            }

            foreach (var entry in map.Patches)
            {
                var resultPopulation = result.GetPopulationInPatchIfExists(playerSpecies, entry.Value);

                // Going extinct in patch is not adjusted, because the minimum viable population clamping is
                // performed already so we don't want to undo that
                if (resultPopulation is null or 0)
                    continue;

                // Adjust to the specified fraction of the full population change
                var previousPopulation =
                    entry.Value.GetSpeciesSimulationPopulation(previousPopulationFrom ?? playerSpecies);

                var change = resultPopulation.Value - previousPopulation;

                change = (long)Math.Round(change * Constants.AUTO_EVO_PLAYER_STRENGTH_FRACTION);

                result.AddPopulationResultForSpecies(playerSpecies, entry.Value, previousPopulation + change);
            }
        }));
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

        RunDuration += timer.Elapsed;
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
                GatherInfo(runSteps);

                // +2 is for this step and the result apply step
                totalSteps = runSteps.Sum(s => s.TotalSteps) + 2;

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

                        while (runSteps.TryPeek(out var step) && step.CanRunConcurrently && maxTasksAtOnce > 0)
                        {
                            var step2 = runSteps.Dequeue();

                            if (step != step2)
                                throw new Exception("Dequeued an unexpected item");

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

    private void UpdateMap(bool playerCantGoExtinct)
    {
        Parameters.World.Map.UpdateGlobalTimePeriod(Parameters.World.TotalPassedTime);

        // Update populations before recording conditions - should not affect per-patch population
        Parameters.World.Map.UpdateGlobalPopulations();

        // Needs to be before the remove extinct species call, so that extinct species could still be stored
        // for reference in patch history (e.g. displaying it as zero on the species population chart)
        foreach (var entry in Parameters.World.Map.Patches)
        {
            entry.Value.RecordSnapshot(true);
        }

        var extinct = Parameters.World.Map.RemoveExtinctSpecies(playerCantGoExtinct);

        foreach (var species in extinct)
        {
            Parameters.World.RemoveSpecies(species);
        }
    }

    private void ApplyExternalEffects()
    {
        if (ExternalEffects.Count > 0)
        {
            foreach (var entry in ExternalEffects)
            {
                try
                {
                    // Make sure CalculateFinalExternalEffectSizes has been called
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (entry.Coefficient != 1)
                    {
                        throw new Exception(
                            "CalculateFinalExternalEffectSizes has not been called to finalize external effects");
                    }

                    // It's possible for external effects to be added for extinct species (either completely extinct
                    // or extinct in the patch it applies to)
                    // We ignore this for player to give the player's reproduction bonus the ability to rescue them
                    if (!results.SpeciesHasResults(entry.Species) && !entry.Species.PlayerSpecies)
                    {
                        GD.Print("Extinct species ", entry.Species.FormattedIdentifier,
                            " had an external effect, ignoring the effect");
                        continue;
                    }

                    long currentPopulation = results.GetPopulationInPatchIfExists(entry.Species, entry.Patch) ?? 0;

                    results.AddPopulationResultForSpecies(entry.Species, entry.Patch,
                        currentPopulation + entry.Constant);
                }
                catch (Exception e)
                {
                    GD.PrintErr("External effect can't be applied: ", e);
                }
            }
        }
    }
}
