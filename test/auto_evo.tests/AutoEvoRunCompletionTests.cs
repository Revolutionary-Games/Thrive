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
    [TestCase]
    public void OneStep_FailureFinishesWithoutResultsOrRepeatingWork()
    {
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
        bool runningAfterCancellation = false;
        bool finishedAfterCancellation = true;
        var workDuration = TimeSpan.Zero;
        ControlledRun? run = null;
        run = new ControlledRun(CreateWorld(), new ActionStep(_ =>
        {
            workDuration = MeasureControlledWork();
            run!.Abort();
            runningAfterCancellation = run.Running;
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
        var world = CreateWorld();
        using var siblingStarted = new ManualResetEventSlim();
        using var firstEnded = new ManualResetEventSlim();
        using var releaseSibling = new ManualResetEventSlim();
        using var siblingEnded = new ManualResetEventSlim();
        Thread? owner = null;
        ControlledRun? run = null;
        run = new ControlledRun(world, new ActionStep(_ =>
        {
            owner = Thread.CurrentThread;
            if (!siblingStarted.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Sibling did not start");

            if (cancel)
                run!.Abort();
            firstEnded.Set();
            if (fail)
                throw new InvalidOperationException("Expected background failure");
        }), new ActionStep(results =>
        {
            siblingStarted.Set();
            if (!releaseSibling.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Sibling was not released");

            ++world.PlayerSpecies.Generation;
            results.AddPopulationResultForSpecies(world.PlayerSpecies, world.Map.CurrentPatch!, 123);
            siblingEnded.Set();
        })) { FullSpeed = true, TrackMemoryInfo = true };

        var blockedDuration = TimeSpan.Zero;
        var observation = Stopwatch.StartNew();
        run.Start();
        try
        {
            AssertThat(firstEnded.Wait(TimeSpan.FromSeconds(10))).IsTrue();
            AssertThat(SpinWait.SpinUntil(() => run.Finished ||
                    (owner!.ThreadState & ThreadState.WaitSleepJoin) != 0,
                TimeSpan.FromSeconds(10))).IsTrue();
            AssertThat(siblingEnded.IsSet).IsFalse();
            AssertThat(run.Finished).IsFalse();
            AssertThat(run.Running).IsTrue();

            // This entire measured interval is inside the accepted sibling step, before we release it.
            blockedDuration = MeasureControlledWork();
        }
        finally
        {
            releaseSibling.Set();
            WaitForCompletion(run);
            observation.Stop();
        }

        AssertThat(siblingEnded.IsSet).IsTrue();
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
