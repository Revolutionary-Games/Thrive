using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

/// <summary>
///   Checks PhotoStudio scheduling and recovery from invalid image requests.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class PhotoStudioTests
{
    [TestCase]
    public void InvalidSimulationRequest_DoesNotBlockFollowingImage()
    {
        var photoStudio = PhotoStudio.Instance;
        var invalidPhotographable = new TestSimulationPhotographable(true,
            unchecked((ulong)Guid.NewGuid().GetHashCode()));
        var plainExceptionPhotographable = new TestSimulationPhotographable(true,
            unchecked((ulong)Guid.NewGuid().GetHashCode()))
        {
            ThrowPlainException = true,
        };
        var validPhotographable = new TestSimulationPhotographable(false,
            unchecked((ulong)Guid.NewGuid().GetHashCode()));

        var invalidTask = photoStudio.GenerateImage(invalidPhotographable, int.MinValue, 16);
        var plainExceptionTask = photoStudio.GenerateImage(plainExceptionPhotographable, int.MinValue + 1, 16);
        var validTask = photoStudio.GenerateImage(validPhotographable, int.MinValue + 2, 16);

        for (int i = 0; i < 32 && !validTask.Finished && !validTask.Failed; ++i)
        {
            // This loop does not yield to Godot's frame loop, so render pending viewport updates explicitly.
            RenderingServer.ForceDraw(false);
            photoStudio._Process(1.0 / 60);
        }

        AssertThat(invalidTask.Failed).IsTrue();
        AssertThat(invalidTask.Finished).IsFalse();
        AssertThat(invalidPhotographable.SetupCount).IsEqual(1);
        AssertThat(plainExceptionTask.Failed).IsTrue();
        AssertThat(plainExceptionTask.Finished).IsFalse();
        AssertThat(plainExceptionPhotographable.SetupCount).IsEqual(1);
        AssertThat(validPhotographable.SetupCount).IsEqual(1);

        // Headless Godot may not provide viewport images, but the request must still reach a terminal state.
        AssertThat(validTask.Finished || validTask.Failed).IsTrue();

        // A corrected request with the same appearance must replace the failed cached task.
        var correctedPhotographable = new TestSimulationPhotographable(false,
            invalidPhotographable.GetVisualHashCode());
        var correctedTask = photoStudio.GenerateImage(correctedPhotographable, int.MinValue, 16);

        for (int i = 0; i < 32 && !correctedTask.Finished && !correctedTask.Failed; ++i)
        {
            // This loop does not yield to Godot's frame loop, so render pending viewport updates explicitly.
            RenderingServer.ForceDraw(false);
            photoStudio._Process(1.0 / 60);
        }

        AssertThat(correctedPhotographable.SetupCount).IsEqual(1);
        AssertThat(correctedTask.Finished || correctedTask.Failed).IsTrue();

        // Complete the image task's cleanup step before leaving the singleton in use by other tests.
        photoStudio._Process(1.0 / 60);
    }

    [TestCase]
    public void SceneStageFailures_DoNotBlockFollowingRequest()
    {
        var photoStudio = PhotoStudio.Instance;
        var oldBackgroundLoad = photoStudio.UseBackgroundSceneLoad;
        var oldBackgroundInstance = photoStudio.UseBackgroundSceneInstance;

        try
        {
            foreach (var background in new[] { false, true })
            {
                photoStudio.UseBackgroundSceneLoad = background;
                photoStudio.UseBackgroundSceneInstance = background;

                for (int failureStage = 0; failureStage < 3; ++failureStage)
                {
                    var broken = new TestScenePhotographable(failureStage);
                    var following = new TestSimulationPhotographable(false,
                        unchecked((ulong)Guid.NewGuid().GetHashCode()));
                    var brokenTask = photoStudio.GenerateImage(broken, int.MinValue, 16);
                    var followingTask = photoStudio.GenerateImage(following, int.MinValue + 1, 16);

                    var timeout = Stopwatch.StartNew();
                    while (!followingTask.Finished && !followingTask.Failed && timeout.Elapsed.TotalSeconds < 10)
                    {
                        RenderingServer.ForceDraw(false);
                        photoStudio._Process(1.0 / 60);

                        // Let background loading and instantiation run while manually driving the main thread.
                        Thread.Sleep(1);
                    }

                    // Complete cleanup even when assertions below fail.
                    photoStudio._Process(1.0 / 60);
                    AssertThat(brokenTask.Failed).IsTrue();
                    AssertThat(broken.FailureCount).IsEqual(1);
                    AssertThat(following.SetupCount).IsEqual(1);
                    AssertThat(followingTask.Finished || followingTask.Failed).IsTrue();
                }
            }
        }
        finally
        {
            photoStudio.UseBackgroundSceneLoad = oldBackgroundLoad;
            photoStudio.UseBackgroundSceneInstance = oldBackgroundInstance;
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NewerBatchRunsFirstAndPreservesOrderWithinBatches(bool flushExplicitly)
    {
        var photoStudio = PhotoStudio.Instance;
        var setupOrder = new List<int>();

        var active = QueueRequest(0);
        photoStudio._Process(1.0 / 60);

        QueueRequest(1);
        var olderBatchEnd = QueueRequest(2);
        var priorityRequest = QueueRequest(5, int.MinValue);

        if (flushExplicitly)
        {
            photoStudio.FlushBatch();
        }
        else
        {
            photoStudio._Process(0.06);
            photoStudio._Process(0.06);
        }

        QueueRequest(3);
        var newerBatchEnd = QueueRequest(4);

        for (int i = 0; i < 80; ++i)
        {
            RenderingServer.ForceDraw(false);
            photoStudio._Process(1.0 / 60);

            if ((olderBatchEnd.Finished || olderBatchEnd.Failed) && (newerBatchEnd.Finished || newerBatchEnd.Failed) &&
                (priorityRequest.Finished || priorityRequest.Failed))
            {
                photoStudio._Process(1.0 / 60);
                break;
            }
        }

        AssertThat(active.Finished || active.Failed).IsTrue();
        AssertThat((IEnumerable<int>)setupOrder).ContainsExactly(0, 5, 3, 4, 1, 2);

        IImageTask QueueRequest(int index, int priority = int.MinValue + 1)
        {
            var photographable = new TestSimulationPhotographable(false,
                unchecked((ulong)Guid.NewGuid().GetHashCode()))
            {
                OnSetup = () => setupOrder.Add(index),
            };

            return photoStudio.GenerateImage(photographable, priority, 16);
        }
    }

    private sealed class TestScenePhotographable : IScenePhotographable
    {
        private readonly int failureStage;
        private readonly ulong hash = unchecked((ulong)Guid.NewGuid().GetHashCode());

        public TestScenePhotographable(int failureStage)
        {
            this.failureStage = failureStage;
        }

        public int FailureCount { get; private set; }

        public string SceneToPhotographPath
        {
            get
            {
                FailAtStage(0);
                return "res://src/microbe_stage/CellHexesPhotoBuilder.tscn";
            }
        }

        public void ApplySceneParameters(Node3D instancedScene)
        {
            FailAtStage(1);
        }

        public Vector3 CalculatePhotographDistance(Node3D photographableObjectState)
        {
            FailAtStage(2);
            return new Vector3(0, 0, 10);
        }

        public ulong GetVisualHashCode()
        {
            return hash;
        }

        private void FailAtStage(int stage)
        {
            if (stage != failureStage)
                return;

            ++FailureCount;
            throw new InvalidOperationException("Expected scene photograph failure");
        }
    }

    private sealed class TestSimulationPhotographable : ISimulationPhotographable
    {
        private readonly bool throwDuringSetup;
        private readonly ulong hash;

        public TestSimulationPhotographable(bool throwDuringSetup, ulong hash)
        {
            this.throwDuringSetup = throwDuringSetup;
            this.hash = hash;
        }

        public int SetupCount { get; private set; }

        public bool ThrowPlainException { get; init; }

        public Action? OnSetup { get; init; }

        public ISimulationPhotographable.SimulationType SimulationToPhotograph =>
            ISimulationPhotographable.SimulationType.MicrobeGraphics;

        public Vector3 CalculatePhotographDistance(IWorldSimulation photographableObjectState)
        {
            return new Vector3(0, 0, 10);
        }

        public void SetupWorldEntities(IWorldSimulation worldSimulation)
        {
            ++SetupCount;
            OnSetup?.Invoke();

            if (ThrowPlainException)
                throw new Exception("Simulation photograph test request failed");

            if (throwDuringSetup)
                throw new InvalidOperationException("Invalid simulation photograph test request");
        }

        public bool StateHasStabilized(IWorldSimulation worldSimulation)
        {
            return true;
        }

        public ulong GetVisualHashCode()
        {
            return hash;
        }
    }
}
