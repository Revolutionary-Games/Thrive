using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;
using ThreadState = System.Threading.ThreadState;

/// <summary>
///   Checks completion, cancellation and continuation through the public run lifecycle.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class AutoEvoRunCompletionTests
{
    private int originalParallelTasks;
    private string? cleanupFailure;

    [Before]
    public void EnsureEnoughWorkers()
    {
        originalParallelTasks = TaskExecutor.Instance.ParallelTasks;
        TaskExecutor.Instance.ParallelTasks = Math.Max(originalParallelTasks, 2);
    }

    [After]
    public void RestoreWorkers()
    {
        // Do not reduce the worker count while callbacks from a failed cleanup may still be running.
        // The failing test already reports this; an After failure would be repeated for every case by GdUnit.
        if (cleanupFailure != null)
            return;

        // This restores the configured count; the executor processes worker exit requests asynchronously.
        TaskExecutor.Instance.ParallelTasks = originalParallelTasks;
    }

    [TestCase]
    public void OneStep_FailureFinishesWithoutResultsOrRepeatingWork()
    {
        RequireCompletedCleanup();
        int calls = 0;
        var workDuration = TimeSpan.Zero;
        var run = new ControlledRun(CreateWorld(), new ActionStep(_ =>
        {
            ++calls;
            workDuration = MeasureControlledWork();
            throw new InvalidOperationException("Expected step failure");
        }));

        run.OneStep();
        var previousDuration = run.RunDuration;
        var observation = Stopwatch.StartNew();
        run.OneStep();
        observation.Stop();

        AssertThat(run.Finished).IsTrue();
        AssertThat(run.Aborted).IsTrue();
        AssertThat(run.Running).IsFalse();
        AssertThat(run.WasSuccessful).IsFalse();
        AssertThat(run.Results).IsNull();
        AssertDurationBounds(run.RunDuration - previousDuration, workDuration, observation.Elapsed);
        var duration = run.RunDuration;
        run.OneStep();
        run.Continue();
        run.Start();
        AssertThat(SpinWait.SpinUntil(() => !run.Running, TimeSpan.FromSeconds(10))).IsTrue();
        AssertThat(calls).IsEqual(1);
        AssertThat(run.RunDuration).IsEqual(duration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OneStep_ObservesCancellationWithoutExecutingAnotherStep(bool afterGathering)
    {
        RequireCompletedCleanup();
        int calls = 0;
        var run = new ControlledRun(CreateWorld(), new ActionStep(_ => ++calls));
        if (afterGathering)
            run.OneStep();

        var completedSteps = run.CompleteSteps;
        run.Abort();
        AssertThat(run.Finished).IsFalse();
        AssertThat(run.IsFinished(false)).IsFalse();
        run.OneStep();

        AssertThat(run.Finished).IsTrue();
        AssertThat(run.Running).IsFalse();
        AssertThat(run.Aborted).IsTrue();
        AssertThat(run.WasSuccessful).IsFalse();
        AssertThat(run.Results).IsNull();
        AssertThat(run.CompleteSteps).IsEqual(completedSteps);
        AssertThat(calls).IsEqual(0);
        AssertThat(run.RunDuration > TimeSpan.Zero).IsTrue();
    }

    [TestCase]
    public void OneStep_CancellationDuringTheStepFinishesAfterItReturns()
    {
        RequireCompletedCleanup();
        bool runningAfterCancellation = false;
        bool finishedAfterCancellation = true;
        var workDuration = TimeSpan.Zero;
        ControlledRun? run = null;
        run = new ControlledRun(CreateWorld(), new ActionStep(_ =>
        {
            workDuration = MeasureControlledWork();

            // The run is assigned before OneStep invokes this callback and is never reassigned afterward.
            // ReSharper disable once AccessToModifiedClosure
            run!.Abort();

            // ReSharper disable once AccessToModifiedClosure
            runningAfterCancellation = run.Running;

            // ReSharper disable once AccessToModifiedClosure
            finishedAfterCancellation = run.Finished;
        }));

        run.OneStep();
        var previousDuration = run.RunDuration;
        var observation = Stopwatch.StartNew();
        run.OneStep();
        observation.Stop();

        AssertThat(runningAfterCancellation).IsTrue();
        AssertThat(finishedAfterCancellation).IsFalse();
        AssertThat(run.Finished).IsTrue();
        AssertThat(run.Running).IsFalse();
        AssertThat(run.Aborted).IsTrue();
        AssertThat(run.WasSuccessful).IsFalse();
        AssertThat(run.Results).IsNull();
        AssertDurationBounds(run.RunDuration - previousDuration, workDuration, observation.Elapsed);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PausedRun_CanFinishWithoutRepeatingCompletedSteps(bool continueInBackground)
    {
        RequireCompletedCleanup();
        int calls = 0;
        var world = CreateWorld();
        var workDuration = TimeSpan.Zero;
        var run = new ControlledRun(world, new ActionStep(results =>
        {
            ++calls;
            results.AddPopulationResultForSpecies(world.PlayerSpecies, world.Map.CurrentPatch!, 123);
        }), new ActionStep(_ =>
        {
            ++calls;
            workDuration = MeasureControlledWork();
        }));
        run.OneStep();
        run.OneStep();
        AssertThat(run.Finished).IsFalse();
        AssertThat(run.Running).IsFalse();
        AssertThat(calls).IsEqual(1);
        var partialDuration = run.RunDuration;
        var observation = Stopwatch.StartNew();

        if (continueInBackground)
        {
            run.Continue();
            WaitForCompletion(run);
        }
        else
        {
            run.OneStep();
            run.OneStep();
            run.OneStep();
        }

        observation.Stop();
        AssertThat(run.WasSuccessful).IsTrue();
        AssertThat(run.Running).IsFalse();
        AssertPopulationResult(run, world);
        AssertThat(run.CompletionFraction).IsEqual(1.0f);
        AssertDurationBounds(run.RunDuration - partialDuration, workDuration, observation.Elapsed);
        AssertThat(calls).IsEqual(2);

        var finalDuration = run.RunDuration;
        run.Start();
        run.OneStep();
        run.Continue();
        AssertThat(SpinWait.SpinUntil(() => !run.Running, TimeSpan.FromSeconds(10))).IsTrue();
        AssertPopulationResult(run, world);
        AssertThat(calls).IsEqual(2);
        AssertThat(run.RunDuration).IsEqual(finalDuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CompletedRun_LifecycleCallsLeaveFinalStateUnchanged(bool background)
    {
        RequireCompletedCleanup();
        var world = CreateWorld();
        int calls = 0;
        var workDuration = TimeSpan.Zero;
        var run = new ControlledRun(world, new ActionStep(results =>
        {
            ++calls;
            workDuration = MeasureControlledWork();
            results.AddPopulationResultForSpecies(world.PlayerSpecies, world.Map.CurrentPatch!, 123);
        }));
        var observation = Stopwatch.StartNew();
        if (background)
        {
            run.Start();
            WaitForCompletion(run);
        }
        else
        {
            run.OneStep();
            run.OneStep();
            run.OneStep();
            run.OneStep();
        }

        observation.Stop();
        AssertThat(run.WasSuccessful).IsTrue();
        AssertPopulationResult(run, world);
        AssertThat(calls).IsEqual(1);
        AssertDurationBounds(run.RunDuration, workDuration, observation.Elapsed);
        var duration = run.RunDuration;
        var completedSteps = run.CompleteSteps;
        var results = run.Results;
        run.Start();
        run.OneStep();
        run.Continue();

        // Also drain an incorrectly restarted run before reporting a regression.
        AssertThat(SpinWait.SpinUntil(() => !run.Running, TimeSpan.FromSeconds(10))).IsTrue();
        AssertThat(run.RunDuration).IsEqual(duration);
        AssertThat(run.CompleteSteps).IsEqual(completedSteps);
        AssertThat(run.Results).IsSame(results);
        AssertPopulationResult(run, world);
        AssertThat(calls).IsEqual(1);
        AssertThat(run.WasSuccessful).IsTrue();
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public void Start_PublishesFinalDataAfterAcceptedWorkStops(bool fail, bool cancel)
    {
        RequireCompletedCleanup();
        var world = CreateWorld();
        var siblingStarted = new ManualResetEventSlim();
        var firstEnded = new ManualResetEventSlim();
        var releaseSibling = new ManualResetEventSlim();

        // These captured flags are intentionally shared; all reads and writes use Volatile.
        int firstExited = 0;
        int siblingExited = 0;
        bool siblingCompleted = false;
        Thread? owner = null;
        ControlledRun? run = null;

        // Captured events are disposed only after both exit markers; ReSharper cannot infer this guarantee.
        run = new ControlledRun(world, new ActionStep(_ =>
        {
            try
            {
                owner = Thread.CurrentThread;

                // ReSharper disable once AccessToDisposedClosure
                if (!siblingStarted.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("Sibling did not start");

                if (cancel)
                {
                    // The run is assigned before Start queues this callback and is never reassigned afterward.
                    // ReSharper disable once AccessToModifiedClosure
                    run!.Abort();
                }

                // ReSharper disable once AccessToDisposedClosure
                firstEnded.Set();
                if (fail)
                    throw new InvalidOperationException("Expected background failure");
            }
            finally
            {
                // No captured event may be accessed after publishing this marker, even on failure.
                // ReSharper disable once AccessToModifiedClosure
                Volatile.Write(ref firstExited, 1);
            }
        }), new ActionStep(results =>
        {
            try
            {
                // ReSharper disable once AccessToDisposedClosure
                siblingStarted.Set();

                // ReSharper disable once AccessToDisposedClosure
                if (!releaseSibling.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("Sibling was not released");

                ++world.PlayerSpecies.Generation;
                results.AddPopulationResultForSpecies(world.PlayerSpecies, world.Map.CurrentPatch!, 123);

                // ReSharper disable once AccessToModifiedClosure
                Volatile.Write(ref siblingCompleted, true);
            }
            finally
            {
                // ReSharper disable once AccessToModifiedClosure
                Volatile.Write(ref siblingExited, 1);
            }
        })) { FullSpeed = true, TrackMemoryInfo = true };

        TimeSpan blockedDuration;
        var observation = Stopwatch.StartNew();
        Exception? observationFailure = null;
        try
        {
            run.Start();
            AssertThat(firstEnded.Wait(TimeSpan.FromSeconds(10))).IsTrue();
            AssertThat(SpinWait.SpinUntil(() => run.Finished ||
                    (owner!.ThreadState & ThreadState.WaitSleepJoin) != 0,
                TimeSpan.FromSeconds(10))).IsTrue();
            AssertThat(Volatile.Read(ref siblingCompleted)).IsFalse();
            AssertThat(run.Finished).IsFalse();
            AssertThat(run.Running).IsTrue();

            // This entire measured interval is inside the accepted sibling step, before we release it.
            blockedDuration = MeasureControlledWork();
        }
        catch (Exception e)
        {
            observationFailure = e;
            throw;
        }
        finally
        {
            releaseSibling.Set();

            // Finished is under test and may be published too early. Wait for the event users independently.
            if (!SpinWait.SpinUntil(() => Volatile.Read(ref firstExited) != 0 &&
                    Volatile.Read(ref siblingExited) != 0, TimeSpan.FromSeconds(10)))
            {
                cleanupFailure = $"First callback exited: {Volatile.Read(ref firstExited) != 0}; " +
                    $"sibling callback exited: {Volatile.Read(ref siblingExited) != 0}. " +
                    "Callback events and worker configuration have been retained";
                var cleanupError = new TimeoutException(cleanupFailure);
                if (observationFailure != null)
                {
                    // GdUnit unwraps nested exceptions to their first cause, losing the other cleanup error.
                    // Report both errors as text so the original assertion and its stack trace remain visible.
                    AssertThat(false).OverrideFailureMessage(
                        $"Observation and callback cleanup failed:\n{observationFailure}\n{cleanupError}").IsTrue();
                }

                // Keep the events alive on timeout: callbacks may still access them.
                throw cleanupError;
            }

            siblingStarted.Dispose();
            firstEnded.Dispose();
            releaseSibling.Dispose();
        }

        WaitForCompletion(run);
        observation.Stop();
        AssertThat(Volatile.Read(ref siblingCompleted)).IsTrue();
        AssertThat(world.PlayerSpecies.Generation).IsEqual(2);
        AssertThat(run.Running).IsFalse();
        AssertThat(run.Aborted).IsEqual(fail || cancel);
        AssertThat(run.WasSuccessful).IsEqual(!fail && !cancel);
        AssertThat(run.Results == null).IsEqual(fail || cancel);
        if (!fail && !cancel)
            AssertPopulationResult(run, world);
        AssertDurationBounds(run.RunDuration, blockedDuration, observation.Elapsed);
        AssertThat(run.PeakMemoryUsage > 0).IsTrue();
    }

    private static void AssertPopulationResult(AutoEvoRun run, GameWorld world)
    {
        AssertThat(run.Results).IsNotNull();
        AssertThat(run.Results!.SpeciesHasResults(world.PlayerSpecies)).IsTrue();
        AssertThat(run.Results.GetPopulationInPatch(world.PlayerSpecies, world.Map.CurrentPatch!)).IsEqual(123L);
    }

    private static void AssertDurationBounds(TimeSpan duration, TimeSpan workDuration, TimeSpan observedDuration)
    {
        // The inner work interval is contained in the production timer, which is contained in our outer observation.
        // Allow for clock conversion precision, not scheduling delays: both bounds use actual elapsed time.
        AssertThat(workDuration.TotalMilliseconds).IsGreaterEqual(20);
        AssertThat(duration.TotalMilliseconds).IsGreaterEqual(workDuration.TotalMilliseconds - 1);
        AssertThat(duration.TotalMilliseconds).IsLessEqual(observedDuration.TotalMilliseconds + 1);
    }

    private static TimeSpan MeasureControlledWork()
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromMilliseconds(20))
            Thread.Sleep(1);

        return timer.Elapsed;
    }

    private static GameWorld CreateWorld()
    {
        return new GameWorld(new WorldGenerationSettings
        {
            Seed = 12345,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        });
    }

    private static void WaitForCompletion(AutoEvoRun run)
    {
        AssertThat(SpinWait.SpinUntil(() => run.Finished, TimeSpan.FromSeconds(10))).IsTrue();
    }

    private void RequireCompletedCleanup()
    {
        // A failed BeforeTest hook does not prevent the test body from running in the current GdUnit runner.
        if (cleanupFailure != null)
            throw new InvalidOperationException($"An earlier test could not clean up its callbacks: {cleanupFailure}");
    }

    private sealed class ControlledRun : AutoEvoRun
    {
        private readonly IRunStep[] steps;

        public ControlledRun(GameWorld world, params IRunStep[] steps) : base(world, world.AutoEvoGlobalCache)
        {
            this.steps = steps;
        }

        protected override void GatherInfo(Queue<IRunStep> destination)
        {
            foreach (var step in steps)
                destination.Enqueue(step);
        }
    }

    private sealed class ActionStep : IRunStep
    {
        private readonly Action<RunResults> action;

        public ActionStep(Action<RunResults> action)
        {
            this.action = action;
        }

        public int TotalSteps => 1;
        public bool CanRunConcurrently => true;

        public bool RunStep(RunResults results)
        {
            action(results);
            return true;
        }
    }
}
