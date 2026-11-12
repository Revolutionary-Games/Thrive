using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   Checks that stopping an editor prediction releases its inputs only after accepted work has finished.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class EditorPredictionLifetimeTests
{
    /// <summary>
    ///   Holds a prediction step while stopping it, then changes its original input immediately after Stop returns.
    ///   The step must read the unchanged input, and the editor must discard the run only after completion.
    /// </summary>
    [TestCase]
    public void StopPredictionWaitsForTheCurrentStepBeforeReturningItsInputs()
    {
        var timeout = TimeSpan.FromSeconds(10);
        var world = new GameWorld(new WorldGenerationSettings
        {
            Seed = 3171,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        });
        var originalGeneration = world.PlayerSpecies.Generation;
        var entered = new ManualResetEventSlim();
        var release = new ManualResetEventSlim();
        int generationReadByStep = -1;
        var run = new GatedEditorRun(world, new LambdaStep(_ =>
        {
            entered.Set();
            if (!release.Wait(timeout))
                throw new TimeoutException("Prediction step was not released");

            generationReadByStep = world.PlayerSpecies.Generation;
        }));
        var editor = new CellEditorComponent();
        var pendingField = typeof(CellEditorComponent).GetField("waitingForPrediction",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var pending = Activator.CreateInstance(pendingField.FieldType, run, world.PlayerSpecies,
            run.ModifiedProperties);
        pendingField.SetValue(editor, pending);

        int stopReturned = 0;
        bool returnedWhileBlocked = true;
        bool finishedWhileBlocked = true;
        bool pendingWhileBlocked = false;
        int generationWhileBlocked = -1;
        Exception? workerFailure = null;
        Exception? releaserFailure = null;
        var stoppingThread = Thread.CurrentThread;
        var worker = new Thread(() =>
        {
            try
            {
                while (!run.Finished)
                    run.OneStep();
            }
            catch (Exception e)
            {
                workerFailure = e;
            }
        }) { IsBackground = true };
        var releaser = new Thread(() =>
        {
            try
            {
                // Observe either a real wait or an incorrect early return; no arbitrary delay decides the result.
                if (!SpinWait.SpinUntil(() => run.Aborted && (Volatile.Read(ref stopReturned) != 0 ||
                        (stoppingThread.ThreadState & ThreadState.WaitSleepJoin) != 0), timeout))
                {
                    throw new TimeoutException("Stop neither waited for the prediction nor returned");
                }

                returnedWhileBlocked = Volatile.Read(ref stopReturned) != 0;
                finishedWhileBlocked = run.Finished;
                pendingWhileBlocked = ReferenceEquals(pendingField.GetValue(editor), pending);
                generationWhileBlocked = world.PlayerSpecies.Generation;
            }
            catch (Exception e)
            {
                releaserFailure = e;
            }
            finally
            {
                release.Set();
            }
        }) { IsBackground = true };

        bool workerStarted = false;
        bool releaserStarted = false;
        try
        {
            worker.Start();
            workerStarted = true;
            AssertThat(entered.Wait(timeout)).IsTrue();
            releaser.Start();
            releaserStarted = true;

            editor.StopAutoEvoPrediction();
            var finishedAtReturn = run.Finished;
            ++world.PlayerSpecies.Generation;
            Volatile.Write(ref stopReturned, 1);

            AssertThat(releaser.Join(timeout)).IsTrue();
            AssertThat(worker.Join(timeout)).IsTrue();
            AssertThat(releaserFailure).IsNull();
            AssertThat(workerFailure).IsNull();
            AssertThat(returnedWhileBlocked).IsFalse();
            AssertThat(finishedWhileBlocked).IsFalse();
            AssertThat(pendingWhileBlocked).IsTrue();
            AssertThat(generationWhileBlocked).IsEqual(originalGeneration);
            AssertThat(generationReadByStep).IsEqual(originalGeneration);
            AssertThat(finishedAtReturn).IsTrue();
            AssertThat(run.Aborted).IsTrue();
            AssertThat(run.Running).IsFalse();
            AssertThat(pendingField.GetValue(editor)).IsNull();
        }
        finally
        {
            run.Abort();
            release.Set();
            var releaserStopped = !releaserStarted || releaser.Join(timeout);
            var workerStopped = !workerStarted || worker.Join(timeout);
            if (!releaserStopped || !workerStopped)
            {
                // Retain the events and node if a worker can still use them; contain this failure in the test process.
                throw new TimeoutException("Prediction lifetime test could not join its threads; resources retained");
            }

            entered.Dispose();
            release.Dispose();
            editor.Free();
        }
    }

    /// <summary>
    ///   Supplies one controlled step through the real EditorAutoEvoRun lifecycle without loading an editor scene.
    /// </summary>
    private sealed class GatedEditorRun(GameWorld world, IRunStep step) :
        EditorAutoEvoRun(world, world.AutoEvoGlobalCache, world.PlayerSpecies,
            (Species)world.PlayerSpecies.Clone(), world.Map.CurrentPatch)
    {
        protected override void GatherInfo(Queue<IRunStep> steps, long seed)
        {
            steps.Enqueue(step);
        }
    }
}
