namespace ThriveTest.Engine.ResourceLoading.Tests;

using System;
using Xunit;

/// <summary>
///   Tests deterministic admission and execution of resource loading completion units within a frame.
/// </summary>
public class ResourceLoadFrameBudgetTests
{
    private const double TARGET_FRAME_TIME_SECONDS = 1.0;

    [Theory]
    [InlineData(-0.05)]
    [InlineData(-0.06)]
    public void Execution_NonPositiveBudgetDoesNotRunAnyCompletionPhase(double savedProcessingTimeSeconds)
    {
        var state = new TestFrameState();
        var timeSource = new TestFrameTimeSource(state);
        var dispatcher = new TestResourceLoadDispatcher(state);
        var resource = new TestResource(0);
        var budget = new ResourceLoadFrameBudget(1.0, savedProcessingTimeSeconds, TARGET_FRAME_TIME_SECONDS);

        Assert.False(ResourceLoadCoordinator.TryRunSynchronousLoad(resource, ref budget, ref dispatcher,
            ref timeSource));
        Assert.Equal(0, state.FullLoadDispatchCount);
        Assert.Equal(0, state.CallbackDispatchCount);
    }

    [Theory]
    [InlineData(0.49)]
    [InlineData(0.5)]
    public void Execution_EstimateThatFitsRemainingBudgetRuns(double estimatedDurationSeconds)
    {
        var state = new TestFrameState();
        var timeSource = new TestFrameTimeSource(state);
        var dispatcher = new TestResourceLoadDispatcher(state);
        var resource = new TestResource(estimatedDurationSeconds);
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);

        Assert.True(ResourceLoadCoordinator.TryRunSynchronousLoad(resource, ref budget, ref dispatcher,
            ref timeSource));
        Assert.Equal(1, state.FullLoadDispatchCount);
        Assert.Equal(1, state.CallbackDispatchCount);
    }

    [Fact]
    public void Execution_FirstCompletionUnitMayExceedRemainingBudget()
    {
        var state = new TestFrameState();
        var timeSource = new TestFrameTimeSource(state);
        var dispatcher = new TestResourceLoadDispatcher(state);
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);
        var resource = new TestResource(0.51);

        Assert.True(ResourceLoadCoordinator.TryRunSynchronousLoad(resource, ref budget, ref dispatcher,
            ref timeSource));

        var followingResource = new TestResource(0.51);
        Assert.False(ResourceLoadCoordinator.TryRunSynchronousLoad(followingResource, ref budget, ref dispatcher,
            ref timeSource));
        Assert.Equal(1, state.FullLoadDispatchCount);
        Assert.Equal(1, state.CallbackDispatchCount);
    }

    [Fact]
    public void Execution_CallbackAndSynchronousUnitCannotBothUseForcedProgress()
    {
        var state = new TestFrameState();
        var timeSource = new TestFrameTimeSource(state);
        var dispatcher = new TestResourceLoadDispatcher(state, callbackDurationSeconds: 0.1);
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);
        var backgroundResource = new TestResource(0);

        Assert.True(ResourceLoadCoordinator.TryRunBackgroundCallback(backgroundResource, ref budget, ref dispatcher,
            ref timeSource));

        var synchronousResource = new TestResource(0.41);
        Assert.False(ResourceLoadCoordinator.TryRunSynchronousLoad(synchronousResource, ref budget, ref dispatcher,
            ref timeSource));
        Assert.Equal(0, state.FullLoadDispatchCount);
        Assert.Equal(1, state.CallbackDispatchCount);
    }

    [Fact]
    public void Execution_ElapsedSchedulingWorkDoesNotConsumeForcedProgress()
    {
        var state = new TestFrameState
        {
            Elapsed = TimeSpan.FromSeconds(0.1),
        };

        var timeSource = new TestFrameTimeSource(state);
        var dispatcher = new TestResourceLoadDispatcher(state);
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);
        var resource = new TestResource(0.5);

        Assert.True(ResourceLoadCoordinator.TryRunSynchronousLoad(resource, ref budget, ref dispatcher,
            ref timeSource));
        Assert.Equal(1, state.FullLoadDispatchCount);
    }

    [Theory]
    [InlineData(0.0, 0.5)]
    [InlineData(1.25, -0.25)]
    [InlineData(4.0, -2.0)]
    public void Carry_UsesActualElapsedTimeAndClampsToLimits(double elapsedSeconds, double expectedCarrySeconds)
    {
        var state = new TestFrameState
        {
            Elapsed = TimeSpan.FromSeconds(elapsedSeconds),
        };

        var timeSource = new TestFrameTimeSource(state);
        var budget = new ResourceLoadFrameBudget(0, 0, TARGET_FRAME_TIME_SECONDS);

        Assert.Equal(expectedCarrySeconds, budget.CalculateSecondsToCarry(ref timeSource), 6);
    }

    [Fact]
    public void Carry_UsesDurationMeasuredThroughProductionCoordinator()
    {
        var state = new TestFrameState();
        var timeSource = new TestFrameTimeSource(state);
        var dispatcher = new TestResourceLoadDispatcher(state, fullLoadDurationSeconds: 0.8);
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);
        var resource = new TestResource(0.75);

        Assert.True(ResourceLoadCoordinator.TryRunSynchronousLoad(resource, ref budget, ref dispatcher,
            ref timeSource));
        Assert.Equal(-0.3, budget.CalculateSecondsToCarry(ref timeSource), 6);
    }

    [Fact]
    public void ProductionDispatcher_SynchronousPathPerformsFullLoadAndCallback()
    {
        var state = new TestFrameState();
        var timeSource = new TestFrameTimeSource(state);
        var dispatcher = default(ResourceManager.ResourceManagerLoadDispatcher);
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);
        var resource = new TestResource(0.25, usesPostProcessing: true);
        resource.OnComplete = resource.RecordCallback;

        Assert.True(ResourceLoadCoordinator.TryRunSynchronousLoad(resource, ref budget, ref dispatcher,
            ref timeSource));
        Assert.True(resource.Loaded);
        Assert.Equal(1, resource.LoadCount);
        Assert.Equal(1, resource.PostProcessingCount);
        Assert.Equal(1, resource.CallbackCount);
    }

    [Fact]
    public void ProductionDispatcher_BackgroundPathOnlyInvokesCallback()
    {
        var state = new TestFrameState();
        var timeSource = new TestFrameTimeSource(state);
        var dispatcher = default(ResourceManager.ResourceManagerLoadDispatcher);
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);
        var resource = new TestResource(0);
        resource.OnComplete = resource.RecordCallback;

        Assert.True(ResourceLoadCoordinator.TryRunBackgroundCallback(resource, ref budget, ref dispatcher,
            ref timeSource));
        Assert.Equal(0, resource.LoadCount);
        Assert.Equal(0, resource.PostProcessingCount);
        Assert.Equal(1, resource.CallbackCount);
    }

    private sealed class TestFrameState
    {
        public TimeSpan Elapsed { get; set; }
        public int FullLoadDispatchCount { get; set; }
        public int CallbackDispatchCount { get; set; }
    }

    private readonly struct TestFrameTimeSource : IResourceLoadFrameTimeSource
    {
        private readonly TestFrameState state;

        public TestFrameTimeSource(TestFrameState state)
        {
            this.state = state;
        }

        public TimeSpan Elapsed => state.Elapsed;
    }

    private readonly struct TestResourceLoadDispatcher : IResourceLoadDispatcher
    {
        private readonly TestFrameState state;
        private readonly double fullLoadDurationSeconds;
        private readonly double callbackDurationSeconds;

        public TestResourceLoadDispatcher(TestFrameState state, double fullLoadDurationSeconds = 0,
            double callbackDurationSeconds = 0)
        {
            this.state = state;
            this.fullLoadDurationSeconds = fullLoadDurationSeconds;
            this.callbackDurationSeconds = callbackDurationSeconds;
        }

        public void ExecuteFullLoad(IResource resource)
        {
            ++state.FullLoadDispatchCount;
            state.Elapsed += TimeSpan.FromSeconds(fullLoadDurationSeconds);
        }

        public void InvokeCompletionCallback(IResource resource)
        {
            ++state.CallbackDispatchCount;
            state.Elapsed += TimeSpan.FromSeconds(callbackDurationSeconds);
        }
    }

    private sealed class TestResource : IResource
    {
        private readonly bool usesPostProcessing;

        public TestResource(double estimatedTimeRequired, bool usesPostProcessing = false)
        {
            EstimatedTimeRequired = (float)estimatedTimeRequired;
            this.usesPostProcessing = usesPostProcessing;
        }

        public bool RequiresSyncLoad => true;
        public bool UsesPostProcessing => usesPostProcessing;
        public bool RequiresSyncPostProcess => usesPostProcessing;
        public bool CancelRequested { get; set; }
        public float EstimatedTimeRequired { get; }
        public bool LoadingPrepared { get; set; } = true;
        public bool Loaded { get; private set; }
        public string Identifier => nameof(TestResource);
        public Action<IResource>? OnComplete { get; set; }
        public int LoadCount { get; private set; }
        public int PostProcessingCount { get; private set; }
        public int CallbackCount { get; private set; }

        public void PrepareLoading()
        {
            LoadingPrepared = true;
        }

        public void Load()
        {
            ++LoadCount;

            if (!UsesPostProcessing)
                Loaded = true;
        }

        public void PerformPostProcessing()
        {
            ++PostProcessingCount;
            Loaded = true;
        }

        public void UnLoad()
        {
            Loaded = false;
        }

        public void RecordCallback(IResource resource)
        {
            ++CallbackCount;
        }
    }
}
