namespace ThriveTest.Engine.ResourceLoading.Tests;

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Nito.Collections;
using Xunit;

/// <summary>
///   Tests scalar frame-budget admission and the two resource-loading execution paths.
/// </summary>
public class ResourceLoadFrameBudgetTests
{
    private const double TARGET_FRAME_TIME_SECONDS = 1.0;

    [Theory]
    [InlineData(-0.05)]
    [InlineData(-0.06)]
    public void Admission_NonPositiveBudgetRejectsWork(double savedProcessingTimeSeconds)
    {
        var budget = new ResourceLoadFrameBudget(1.0, savedProcessingTimeSeconds, TARGET_FRAME_TIME_SECONDS);

        Assert.False(budget.TryAdmit(0, 0));
    }

    [Theory]
    [InlineData(0.49)]
    [InlineData(0.5)]
    public void Admission_EstimateThatFitsRemainingBudgetIsAccepted(double estimatedDurationSeconds)
    {
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);

        Assert.True(budget.TryAdmit(estimatedDurationSeconds, 0));
    }

    [Fact]
    public void Admission_FirstOversizedUnitIsAcceptedButFollowingOneIsRejected()
    {
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);

        Assert.True(budget.TryAdmit(0.51, 0));
        Assert.False(budget.TryAdmit(0.51, 0));
    }

    [Fact]
    public void Admission_ElapsedSchedulingTimeDoesNotConsumeForcedProgressOpportunity()
    {
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);

        Assert.True(budget.TryAdmit(0.5, 0.1));
    }

    [Theory]
    [InlineData(0.0, 0.5)]
    [InlineData(1.25, -0.25)]
    [InlineData(4.0, -2.0)]
    public void Carry_UsesActualElapsedTimeAndClampsToLimits(double elapsedSeconds, double expectedCarrySeconds)
    {
        var budget = new ResourceLoadFrameBudget(0, 0, TARGET_FRAME_TIME_SECONDS);

        Assert.Equal(expectedCarrySeconds, budget.CalculateSecondsToCarry(elapsedSeconds), 6);
    }

    [Fact]
    public void Contract_AsyncLoadCannotRequireMissingPostProcessingPhase()
    {
        var resource = new TestResource(0, requiresSyncLoad: false, usesPostProcessing: false,
            requiresSyncPostProcess: true);

        Assert.Throws<InvalidOperationException>(() => ResourceManager.ValidateResourceConfiguration(resource));
    }

    [Fact]
    public void SynchronousPath_AdmissionUsesTotalEstimate()
    {
        var precedingResource = new TestResource(0);
        var resource = new TestResource(0.75, usesPostProcessing: true);
        resource.OnComplete = resource.RecordCallback;
        var (manager, lifecycle) = CreateManager();
        manager.QueueLoad(precedingResource);
        manager.QueueLoad(resource);

        manager._Process(0);

        Assert.True(precedingResource.Loaded);
        Assert.Equal(ResourceLoadState.Prepared, lifecycle.GetState(resource));
        Assert.Equal(0, resource.LoadCount);

        manager._Process(0);

        Assert.Equal(1, resource.LoadCount);
        Assert.Equal(1, resource.PostProcessingCount);
        Assert.Equal(1, resource.CallbackCount);
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(2, 0, 0)]
    [InlineData(3, 1, 1)]
    [InlineData(4, 0, 0)]
    public void PendingMainPath_AdmissionAndSettlementAreExactlyOnce(int failureMode,
        int expectedPostProcessingCount, int expectedCallbackCount)
    {
        bool throwDuringPostProcessing = failureMode == 1;
        bool usesPostProcessing = throwDuringPostProcessing || failureMode is 3 or 4;
        var resource = new TestResource(0.75, requiresSyncLoad: false,
            usesPostProcessing, requiresSyncPostProcess: usesPostProcessing);
        resource.PostProcessingAction = throwDuringPostProcessing ?
            () => throw new InvalidOperationException("post-processing failed") : null;

        int callbackAttemptCount = 0;
        resource.OnComplete = failureMode == 2 ? _ =>
        {
            ++callbackAttemptCount;
            throw new InvalidOperationException("callback failed");
        } : resource.RecordCallback;
        var (manager, lifecycle) = CreateManagerWithCompletedBackgroundLoad(resource);
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);

        if (failureMode is 0 or 4)
            Assert.True(budget.TryAdmit(0, 0));

        manager.ObserveProcessingBackgroundTask(ref budget, suppressFailureReporting: true);
        Assert.Equal(failureMode == 4 ? ResourceLoadState.PendingMain : ResourceLoadState.Completed,
            lifecycle.GetState(resource));
        Assert.Equal(expectedPostProcessingCount, resource.PostProcessingCount);
        Assert.Equal(expectedCallbackCount, resource.CallbackCount);
        Assert.False(budget.TryAdmit(0.51, 0));

        var nextFrameBudget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);
        manager.ObserveProcessingBackgroundTask(ref nextFrameBudget, suppressFailureReporting: true);
        Assert.Equal(failureMode != 4, nextFrameBudget.TryAdmit(0.51, 0));

        if (failureMode == 2)
            Assert.Equal(1, callbackAttemptCount);
    }

    private static (ResourceManager Manager, ResourceLoadLifecycle Lifecycle)
        CreateManagerWithCompletedBackgroundLoad(TestResource resource)
    {
        var (manager, lifecycle) = CreateManager();
        Assert.True(lifecycle.TryQueue(resource));
        lifecycle.MarkPrepared(resource);
        lifecycle.BeginLoading(resource);
        resource.Load();
        SetPrivateField(manager, "processingBackgroundTask",
            new ResourceBackgroundTask(resource, System.Threading.Tasks.Task.CompletedTask, ResourceBackgroundPhase.Load));
        return (manager, lifecycle);
    }

    private static (ResourceManager Manager, ResourceLoadLifecycle Lifecycle) CreateManager()
    {
        var manager = (ResourceManager)RuntimeHelpers.GetUninitializedObject(typeof(ResourceManager));
        var lifecycle = new ResourceLoadLifecycle();
        SetPrivateField(manager, "queuedResources", new BlockingCollection<IResource>());
        SetPrivateField(manager, "processingResources", new Deque<IResource>());
        SetPrivateField(manager, "loadLifecycle", lifecycle);
        SetPrivateField(manager, "timeTracker", new System.Diagnostics.Stopwatch());
        return (manager, lifecycle);
    }

    private static void SetPrivateField<T>(ResourceManager manager, string fieldName, T value)
    {
        typeof(ResourceManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(manager, value);
    }

    private sealed class TestResource(double estimatedTimeRequired, bool requiresSyncLoad = true,
        bool usesPostProcessing = false, bool requiresSyncPostProcess = false)
        : IResource
    {
        public bool RequiresSyncLoad { get; } = requiresSyncLoad;
        public bool UsesPostProcessing { get; } = usesPostProcessing;
        public bool RequiresSyncPostProcess { get; } = requiresSyncPostProcess;
        public bool CancelRequested { get; set; }
        public float EstimatedTimeRequired { get; } = (float)estimatedTimeRequired;
        public bool LoadingPrepared { get; set; } = true;
        public bool Loaded { get; private set; }
        public string Identifier => nameof(TestResource);
        public Action<IResource>? OnComplete { get; set; }
        public int LoadCount { get; private set; }
        public int PostProcessingCount { get; private set; }
        public int CallbackCount { get; private set; }
        public Action? PostProcessingAction { get; set; }

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
            PostProcessingAction?.Invoke();
            Loaded = true;
        }

        public void UnLoad()
        {
            Loaded = false;
        }

        public void RecordCallback(IResource resource)
        {
            if (UsesPostProcessing && !Loaded)
                throw new InvalidOperationException("callback ran before post-processing completed");

            ++CallbackCount;
        }

    }
}
