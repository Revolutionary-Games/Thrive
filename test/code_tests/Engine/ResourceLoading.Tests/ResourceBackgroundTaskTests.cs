namespace ThriveTest.Engine.ResourceLoading.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class ResourceBackgroundTaskTests
{
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
