namespace ThriveTest.Engine.ResourceLoading.Tests;

using System;
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
    public void Admission_CallbackOnlyUnitConsumesForcedProgressOpportunity()
    {
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);

        Assert.True(budget.TryAdmit(0, 0.1));
        Assert.False(budget.TryAdmit(0.41, 0.1));
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
    [InlineData(false, false, 0)]
    [InlineData(true, true, 1)]
    public void PendingMainPath_PerformsRequiredPhasesAndCallback(bool usesPostProcessing,
        bool requiresSyncPostProcess, int expectedPostProcessingCount)
    {
        var resource = new TestResource(0.25, requiresSyncLoad: false, usesPostProcessing,
            requiresSyncPostProcess);
        resource.OnComplete = resource.RecordCallback;
        resource.Load();

        ResourceManager.PerformPendingMainThreadPhases(resource, null);

        Assert.True(resource.Loaded);
        Assert.Equal(1, resource.LoadCount);
        Assert.Equal(expectedPostProcessingCount, resource.PostProcessingCount);
        Assert.Equal(1, resource.CallbackCount);
    }

    [Fact]
    public void Execution_CallbackFailureDoesNotRestoreAdmissionOpportunity()
    {
        var budget = new ResourceLoadFrameBudget(0.5, 0, TARGET_FRAME_TIME_SECONDS);
        var resource = new TestResource(0.51);
        resource.OnComplete = _ => throw new InvalidOperationException("callback failed");

        Assert.True(budget.TryAdmit(((IResource)resource).EstimatedMainThreadTimeRequired, 0));
        Assert.Throws<InvalidOperationException>(() => ResourceManager.PerformSynchronousLoadAndCallback(resource));
        Assert.False(budget.TryAdmit(0.51, 0));
    }

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
