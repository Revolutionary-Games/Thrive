using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   Verifies that queued Auto-Evo runs cannot be submitted a second time through another entry point.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class AutoEvoRunStartupTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void QueuedRun_IsRunningAndExecutesOnlyOnce(bool continueFirst)
    {
        var world = new GameWorld(new WorldGenerationSettings
        {
            Seed = 12345,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        });
        var run = new EmptyRun(world);

        var executor = TaskExecutor.Instance;
        using var parked = new CountdownEvent(executor.ParallelTasks);
        using var releaseOneWorker = new ManualResetEventSlim();
        using var releaseOtherWorkers = new ManualResetEventSlim();
        var parkingTasks = new List<Task>();
        Task? afterRun = null;
        bool runningWhileQueued = false;
        int completedSteps = -1;
        try
        {
            for (int i = 0; i < executor.ParallelTasks; ++i)
            {
                var release = i == 0 ? releaseOneWorker : releaseOtherWorkers;
                var task = new Task(() =>
                {
                    parked.Signal();
                    if (!release.Wait(TimeSpan.FromSeconds(15)))
                        throw new TimeoutException("Worker was not released");
                });
                executor.AddTask(task);
                parkingTasks.Add(task);
            }

            AssertThat(parked.Wait(TimeSpan.FromSeconds(10))).IsTrue();
            if (continueFirst)
            {
                run.Continue();
            }
            else
            {
                run.Start();
            }

            runningWhileQueued = run.Running;
            run.Start();
            run.IsFinished();

            // With only one worker released, this queued observation runs after all earlier submissions finish.
            afterRun = new Task(() => completedSteps = run.CompleteSteps);
            executor.AddTask(afterRun);
            releaseOneWorker.Set();
            AssertThat(afterRun.Wait(TimeSpan.FromSeconds(10))).IsTrue();
        }
        finally
        {
            releaseOneWorker.Set();
            releaseOtherWorkers.Set();
            AssertThat(Task.WaitAll(parkingTasks.ToArray(), TimeSpan.FromSeconds(10))).IsTrue();
            if (afterRun != null)
                AssertThat(afterRun.Wait(TimeSpan.FromSeconds(10))).IsTrue();
        }

        AssertThat(runningWhileQueued).IsTrue();
        AssertThat(run.Finished).IsTrue();
        AssertThat(run.Running).IsFalse();

        // An empty run completes its gathering and ending stages exactly once each.
        AssertThat(completedSteps).IsEqual(2);
    }

    private sealed class EmptyRun : AutoEvoRun
    {
        public EmptyRun(GameWorld world) : base(world, world.AutoEvoGlobalCache)
        {
        }

        protected override void GatherInfo(Queue<IRunStep> destination)
        {
        }
    }
}
