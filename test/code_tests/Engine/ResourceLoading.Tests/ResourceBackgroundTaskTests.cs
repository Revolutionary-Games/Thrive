namespace ThriveTest.Engine.ResourceLoading.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class ResourceBackgroundTaskTests
{
    [Theory]
    [InlineData((int)ResourceLoadState.Queued, true, false, true)]
    [InlineData((int)ResourceLoadState.Preparing, false, false, true)]
    [InlineData((int)ResourceLoadState.Prepared, true, false, true)]
    [InlineData((int)ResourceLoadState.Loading, false, false, true)]
    [InlineData((int)ResourceLoadState.PendingMain, true, true, true)]
    [InlineData((int)ResourceLoadState.Completed, false, false, false)]
    public void Cancellation_HasDefinedResultForEveryLifecycleState(int stateValue, bool canSettle,
        bool needsUnload, bool cancelAccepted)
    {
        var state = (ResourceLoadState)stateValue;
        var lifecycle = new ResourceLoadLifecycle();
        var resource = new TestResource();
        MoveToState(lifecycle, resource, state);

        Assert.Equal(cancelAccepted, lifecycle.TryCancel(resource));
        Assert.Equal(cancelAccepted, resource.CancelRequested);
        Assert.Equal(canSettle, lifecycle.CancellationCanSettle(resource));
        Assert.Equal(needsUnload, lifecycle.CancellationNeedsUnload(resource));
        Assert.Equal(state, lifecycle.GetState(resource));
    }

    [Fact]
    public void Queue_RejectsDuplicateUntilOwnerCompletes()
    {
        var lifecycle = new ResourceLoadLifecycle();
        var resource = new TestResource();

        Assert.True(lifecycle.TryQueue(resource));
        Assert.False(lifecycle.TryQueue(resource));
        Assert.False(lifecycle.Complete(resource));
        Assert.Equal(ResourceLoadState.Completed, lifecycle.GetState(resource));
    }

    [Fact]
    public void Queue_CancelPendingReloadIsDeferredUntilLoadSettles()
    {
        var lifecycle = new ResourceLoadLifecycle();
        var resource = new TestResource();
        MoveToState(lifecycle, resource, ResourceLoadState.Loading);

        Assert.True(lifecycle.TryCancel(resource));
        Assert.False(lifecycle.TryQueue(resource));
        Assert.False(lifecycle.CancellationCanSettle(resource));

        lifecycle.FinishBackgroundLoad(resource);

        Assert.True(lifecycle.CancellationCanSettle(resource));
        Assert.True(lifecycle.CancellationNeedsUnload(resource));
        Assert.True(lifecycle.Complete(resource));
        Assert.Equal(ResourceLoadState.Queued, lifecycle.GetState(resource));
        Assert.False(resource.CancelRequested);
    }

    [Fact]
    public void Completion_IsNotObservedBeforeTaskCompletes_AndIsObservedOnlyOnce()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resource = new TestResource();
        var backgroundTask = new ResourceBackgroundTask(resource, completion.Task, ResourceBackgroundPhase.Prepare);

        Assert.False(backgroundTask.TryObserveCompletion());
        Assert.False(backgroundTask.CompletionObserved);

        completion.SetResult();

        Assert.True(backgroundTask.TryObserveCompletion());
        Assert.True(backgroundTask.CompletionObserved);
        Assert.False(backgroundTask.TryObserveCompletion());
        Assert.Same(resource, backgroundTask.Resource);
        Assert.Same(completion.Task, backgroundTask.Task);
        Assert.Equal(ResourceBackgroundPhase.Prepare, backgroundTask.Phase);
    }

    [Fact]
    public void Fault_IsPropagatedOnlyByFirstCompletionObservation()
    {
        var expected = new InvalidOperationException("load failed");
        var backgroundTask = new ResourceBackgroundTask(new TestResource(), Task.FromException(expected),
            ResourceBackgroundPhase.Load);

        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => backgroundTask.TryObserveCompletion()));
        Assert.True(backgroundTask.CompletionObserved);
        Assert.False(backgroundTask.TryObserveCompletion());
    }

    [Fact]
    public void Cancellation_IsPropagatedOnlyByFirstCompletionObservation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var backgroundTask = new ResourceBackgroundTask(new TestResource(),
            Task.FromCanceled(cancellation.Token), ResourceBackgroundPhase.Prepare);

        Assert.Throws<TaskCanceledException>(() => backgroundTask.TryObserveCompletion());
        Assert.True(backgroundTask.CompletionObserved);
        Assert.False(backgroundTask.TryObserveCompletion());
    }

    [Fact]
    public async Task ExitObservation_ObservesLaterTaskFaultExactlyOnce()
    {
        var expected = new InvalidOperationException("late load failure");
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var backgroundTask = new ResourceBackgroundTask(new TestResource(), completion.Task,
            ResourceBackgroundPhase.Load);
        var failureReported = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

        backgroundTask.ObserveFailureOnCompletion(e => failureReported.SetResult(e));
        completion.SetException(expected);

        Assert.Same(expected, await failureReported.Task.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.True(backgroundTask.CompletionObserved);
        Assert.False(backgroundTask.TryObserveCompletion());
    }

    private static void MoveToState(ResourceLoadLifecycle lifecycle, IResource resource, ResourceLoadState state)
    {
        if (state == ResourceLoadState.Completed)
            return;

        Assert.True(lifecycle.TryQueue(resource));

        if (state == ResourceLoadState.Queued)
            return;

        lifecycle.BeginPreparing(resource);

        if (state == ResourceLoadState.Preparing)
            return;

        lifecycle.FinishPreparing(resource);

        if (state == ResourceLoadState.Prepared)
            return;

        lifecycle.BeginLoading(resource);

        if (state == ResourceLoadState.Loading)
            return;

        lifecycle.FinishBackgroundLoad(resource);
    }

    private sealed class TestResource : IResource
    {
        public bool RequiresSyncLoad => false;
        public bool UsesPostProcessing => false;
        public bool RequiresSyncPostProcess => false;
        public bool CancelRequested { get; set; }
        public float EstimatedTimeRequired => 0;
        public bool LoadingPrepared { get; set; }
        public bool Loaded => false;
        public string Identifier => nameof(TestResource);
        public Action<IResource>? OnComplete { get; set; }

        public void PrepareLoading()
        {
        }

        public void Load()
        {
        }

        public void PerformPostProcessing()
        {
        }

        public void UnLoad()
        {
        }
    }
}
