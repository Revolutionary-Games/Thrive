namespace ThriveTest.Engine.ResourceLoading.Tests;

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
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

    [Theory]
    [InlineData(false, false, false, 0)]
    [InlineData(false, true, true, 0.25)]
    [InlineData(true, false, true, 0.25)]
    public void Contract_DefaultMainThreadEstimateIsDerivedFromPhaseFlags(bool requiresSyncLoad,
        bool usesPostProcessing, bool requiresSyncPostProcess, double expectedMainThreadEstimate)
    {
        IResource resource = new TestResource(0.25, requiresSyncLoad, usesPostProcessing,
            requiresSyncPostProcess);

        Assert.Equal(0.25f, resource.EstimatedTimeRequired);
        Assert.Equal((float)expectedMainThreadEstimate, resource.EstimatedMainThreadTimeRequired);
    }

    [Fact]
    public void Contract_DerivedMainThreadEstimateIsNotPartOfRegistryJson()
    {
        string propertyName = nameof(IResource.EstimatedMainThreadTimeRequired);

        Assert.DoesNotContain(propertyName, JsonConvert.SerializeObject(new SceneResource("res://test.tscn")));
        Assert.DoesNotContain(propertyName, JsonConvert.SerializeObject(new VisualResourceData()));
    }

    [Fact]
    public void Contract_AsyncLoadCannotRequireMissingPostProcessingPhase()
    {
        var resource = new TestResource(0, requiresSyncLoad: false, usesPostProcessing: false,
            requiresSyncPostProcess: true);

        Assert.Throws<InvalidOperationException>(() => ResourceManager.ValidateResourceConfiguration(resource));
    }

    [Fact]
    public void SynchronousPath_PerformsFullLoadAndCallback()
    {
        var resource = new TestResource(0.25, usesPostProcessing: true);
        resource.OnComplete = resource.RecordCallback;

        ResourceManager.PerformSynchronousLoadAndCallback(resource);

        Assert.True(resource.Loaded);
        Assert.Equal(1, resource.LoadCount);
        Assert.Equal(1, resource.PostProcessingCount);
        Assert.Equal(1, resource.CallbackCount);
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(2, 0, 0)]
    [InlineData(3, 1, 1)]
    public void PendingMainPath_AdmissionAndSettlementAreExactlyOnce(int failureMode,
        int expectedPostProcessingCount, int expectedCallbackCount)
    {
        bool throwDuringPostProcessing = failureMode == 1;
        bool usesPostProcessing = throwDuringPostProcessing || failureMode == 3;
        var resource = new TestResource(0.25, requiresSyncLoad: false,
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

        manager.ObserveProcessingBackgroundTask(ref budget, suppressFailureReporting: true);
        Assert.Equal(ResourceLoadState.Completed, lifecycle.GetState(resource));
        Assert.Equal(expectedPostProcessingCount, resource.PostProcessingCount);
        Assert.Equal(expectedCallbackCount, resource.CallbackCount);
        Assert.False(budget.TryAdmit(0.51, 0));

        var nextFrameBudget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);
        manager.ObserveProcessingBackgroundTask(ref nextFrameBudget, suppressFailureReporting: true);
        Assert.True(nextFrameBudget.TryAdmit(0.51, 0));

        if (failureMode == 2)
            Assert.Equal(1, callbackAttemptCount);
    }

    private static (ResourceManager Manager, ResourceLoadLifecycle Lifecycle)
        CreateManagerWithCompletedBackgroundLoad(TestResource resource)
    {
        var manager = (ResourceManager)RuntimeHelpers.GetUninitializedObject(typeof(ResourceManager));
        var lifecycle = new ResourceLoadLifecycle();
        SetPrivateField(manager, "loadLifecycle", lifecycle);
        SetPrivateField(manager, "timeTracker", new System.Diagnostics.Stopwatch());
        Assert.True(lifecycle.TryQueue(resource));
        lifecycle.MarkPrepared(resource);
        lifecycle.BeginLoading(resource);
        resource.Load();
        SetPrivateField(manager, "processingBackgroundTask",
            new ResourceBackgroundTask(resource, System.Threading.Tasks.Task.CompletedTask, ResourceBackgroundPhase.Load));
        return (manager, lifecycle);
    }

    private static void SetPrivateField<T>(ResourceManager manager, string fieldName, T value) =>
        typeof(ResourceManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(manager, value);

    private sealed class TestResource : IResource
    {
        public TestResource(double estimatedTimeRequired, bool requiresSyncLoad = true,
            bool usesPostProcessing = false, bool requiresSyncPostProcess = false)
        {
            EstimatedTimeRequired = (float)estimatedTimeRequired;
            RequiresSyncLoad = requiresSyncLoad;
            UsesPostProcessing = usesPostProcessing;
            RequiresSyncPostProcess = requiresSyncPostProcess;
        }

        public bool RequiresSyncLoad { get; }
        public bool UsesPostProcessing { get; }
        public bool RequiresSyncPostProcess { get; }
        public bool CancelRequested { get; set; }
        public float EstimatedTimeRequired { get; }
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
