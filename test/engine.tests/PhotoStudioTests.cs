using System;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

/// <summary>
///   Checks PhotoStudio recovery from invalid simulation image requests.
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
        var validPhotographable = new TestSimulationPhotographable(false,
            unchecked((ulong)Guid.NewGuid().GetHashCode()));

        var invalidTask = photoStudio.GenerateImage(invalidPhotographable, int.MinValue, 16);
        var validTask = photoStudio.GenerateImage(validPhotographable, int.MinValue + 1, 16);

        for (int i = 0; i < 32 && !validTask.Finished; ++i)
        {
            photoStudio._Process(1.0 / 60);
        }

        AssertThat(invalidTask.Failed).IsTrue();
        AssertThat(invalidTask.Finished).IsFalse();
        AssertThat(invalidPhotographable.SetupCount).IsEqual(1);
        AssertThat(validPhotographable.SetupCount).IsEqual(1);
        AssertThat(validTask.Finished).IsTrue();

        // A corrected request with the same appearance must replace the failed cached task.
        var correctedPhotographable = new TestSimulationPhotographable(false,
            invalidPhotographable.GetVisualHashCode());
        var correctedTask = photoStudio.GenerateImage(correctedPhotographable, int.MinValue, 16);

        for (int i = 0; i < 32 && !correctedTask.Finished; ++i)
        {
            photoStudio._Process(1.0 / 60);
        }

        AssertThat(correctedTask.Failed).IsFalse();
        AssertThat(correctedPhotographable.SetupCount).IsEqual(1);
        AssertThat(correctedTask.Finished).IsTrue();

        // Complete the image task's cleanup step before leaving the singleton in use by other tests.
        photoStudio._Process(1.0 / 60);
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

        public ISimulationPhotographable.SimulationType SimulationToPhotograph =>
            ISimulationPhotographable.SimulationType.MicrobeGraphics;

        public Vector3 CalculatePhotographDistance(IWorldSimulation photographableObjectState)
        {
            return new Vector3(0, 0, 10);
        }

        public void SetupWorldEntities(IWorldSimulation worldSimulation)
        {
            ++SetupCount;

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
