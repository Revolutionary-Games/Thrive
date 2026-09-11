using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
using Environment = System.Environment;

[TestSuite]
[RequireGodotRuntime]
public class ResourceLoadingTests
{
    [TestCase]
    public void UnregisteredTransitionReportsMissingRequest()
    {
        var lifecycle = new ResourceLoadLifecycle();
        var resource = new TestResource();

        AssertThrown(() => lifecycle.BeginPreparing(resource))
            .IsInstanceOf<InvalidOperationException>()
            .StartsWithMessage($"Resource {resource.Identifier} is not registered");
    }

    [TestCase]
    public void CancelledRequestRequiresExplicitSubmissionAfterCompletion()
    {
        var lifecycle = new ResourceLoadLifecycle();
        var resource = new TestResource();

        AssertThat(lifecycle.TryQueue(resource)).IsTrue();
        AssertThat(lifecycle.TryQueue(resource)).IsFalse();
        AssertThat(lifecycle.TryCancel(resource)).IsTrue();
        AssertThat(lifecycle.TryQueue(resource)).IsFalse();
        AssertThat(resource.CancelRequested).IsTrue();

        lifecycle.Complete(resource);

        AssertThat(lifecycle.IsActive(resource)).IsFalse();
        AssertThat(resource.CancelRequested).IsTrue();
        AssertThat(lifecycle.TryQueue(resource)).IsTrue();
        AssertThat(resource.CancelRequested).IsFalse();
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, true)]
    [TestCase(true, true, true)]
    [TestCase(true, true, false)]
    public async Task CancellationUnloadsWithoutCallingCompletion(bool loadedDuringLoad, bool cancelInPost,
        bool synchronousLoad)
    {
        var manager = ResourceManager.Instance;
        var mainThread = Environment.CurrentManagedThreadId;
        var resource = new TestResource
        {
            RequiresSyncLoad = synchronousLoad,
            LoadingPrepared = true,
            LoadedDuringLoad = loadedDuringLoad,
            UsesPostProcessing = cancelInPost,
            RequiresSyncPostProcess = cancelInPost,
        };

        if (cancelInPost)
            resource.DuringPostProcess = () => manager.CancelLoad(resource);
        else
            resource.DuringLoad = () => manager.CancelLoad(resource);

        try
        {
            manager.QueueLoad(resource);
            await WaitUntil(() => cancelInPost ? resource.PostProcessCount > 0 : resource.LoadCount > 0);

            AssertThat(resource.CompletionCount).IsEqual(0);
            AssertThat(resource.UnloadCount).IsEqual(1);
            AssertThat(resource.UnloadThread).IsEqual(mainThread);
            AssertThat(resource.Loaded).IsFalse();

            resource.DuringLoad = null;
            resource.DuringPostProcess = null;
            resource.LoadedDuringLoad = true;
            manager.QueueLoad(resource);
            await WaitUntil(() => resource.CompletionCount == 1);

            AssertThat(resource.LoadCount).IsEqual(2);
            AssertThat(resource.CompletionThread).IsEqual(mainThread);
        }
        finally
        {
            manager.CancelLoad(resource);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PreparationOverlapsOneLoadAndCancellationWaitsForItsReturn(bool nextLoadIsSynchronous)
    {
        var manager = ResourceManager.Instance;
        var originalThreadCount = TaskExecutor.Instance.ParallelTasks;
        TaskExecutor.Instance.ParallelTasks = Math.Max(2, originalThreadCount);
        using var loadStarted = new ManualResetEventSlim();
        using var releaseLoad = new ManualResetEventSlim();
        using var prepareStarted = new ManualResetEventSlim();
        using var releasePrepare = new ManualResetEventSlim();
        using var nextPrepareStarted = new ManualResetEventSlim();
        var first = new TestResource
        {
            LoadingPrepared = true,
            DuringLoad = () => BlockUntilReleased(loadStarted, releaseLoad),
        };
        var second = new TestResource
        {
            RequiresSyncLoad = nextLoadIsSynchronous,
            DuringPrepare = () => BlockUntilReleased(prepareStarted, releasePrepare),
        };
        var third = new TestResource { DuringPrepare = nextPrepareStarted.Set };

        try
        {
            manager.QueueLoad(first);
            await WaitUntil(() => loadStarted.IsSet);
            manager.QueueLoad(second);
            manager.QueueLoad(third);
            await WaitUntil(() => prepareStarted.IsSet);
            await WaitFrames(5);

            AssertThat(nextPrepareStarted.IsSet).IsFalse();
            releasePrepare.Set();
            await WaitUntil(() => nextPrepareStarted.IsSet);
            AssertThat(second.LoadCount).IsEqual(0);
            AssertThat(third.LoadCount).IsEqual(0);

            manager.CancelLoad(first);
            manager.QueueLoad(first);
            await WaitFrames(5);
            AssertThat(first.CancelRequested).IsTrue();
            AssertThat(first.UnloadCount).IsEqual(0);

            releaseLoad.Set();
            await WaitUntil(() => first.UnloadCount == 1 && second.CompletionCount == 1 && third.CompletionCount == 1);
            AssertThat(first.LoadCount).IsEqual(1);
            AssertThat(first.CompletionCount).IsEqual(0);
            AssertThat(first.UnloadThread).IsEqual(Environment.CurrentManagedThreadId);

            manager.QueueLoad(first);
            await WaitUntil(() => first.CompletionCount == 1);
            AssertThat(first.LoadCount).IsEqual(2);
            AssertThat(first.CompletionThread).IsEqual(Environment.CurrentManagedThreadId);
        }
        finally
        {
            manager.CancelLoad(first);
            manager.CancelLoad(second);
            manager.CancelLoad(third);
            releasePrepare.Set();
            releaseLoad.Set();

            try
            {
                await WaitUntil(() => (!loadStarted.IsSet || first.LoadReturned) &&
                    (!prepareStarted.IsSet || second.PrepareReturned));

                // Requiring preparation as well as loading waits for both occupied slots before signals are disposed.
                var marker = new TestResource { RequiresSyncLoad = true };
                manager.QueueLoad(marker);
                await WaitUntil(() => marker.CompletionCount == 1);
            }
            finally
            {
                TaskExecutor.Instance.ParallelTasks = originalThreadCount;
            }
        }
    }

    [TestCase("prepare")]
    [TestCase("load")]
    [TestCase("post-process")]
    [TestCase("callback")]
    public async Task FailedOperationDoesNotBlockLaterRequestsOrRepeatItsCallback(string failingOperation)
    {
        var manager = ResourceManager.Instance;
        Action fail = () => throw new InvalidOperationException("Expected resource test failure");
        var callbackAttempts = 0;
        var failing = new TestResource
        {
            DuringPrepare = failingOperation == "prepare" ? fail : null,
            DuringLoad = failingOperation == "load" ? fail : null,
            UsesPostProcessing = failingOperation == "post-process",
            RequiresSyncPostProcess = failingOperation == "post-process",
            DuringPostProcess = failingOperation == "post-process" ? fail : null,
            OnComplete = _ =>
            {
                ++callbackAttempts;
                fail();
            },
        };
        var following = new TestResource();

        try
        {
            manager.QueueLoad(failing);
            manager.QueueLoad(following);
            await WaitUntil(() => following.CompletionCount == 1);
            await WaitFrames(5);

            AssertThat(callbackAttempts).IsEqual(failingOperation == "callback" ? 1 : 0);
            AssertThat(following.CompletionCount).IsEqual(1);
            AssertThat(following.CompletionThread).IsEqual(Environment.CurrentManagedThreadId);
        }
        finally
        {
            manager.CancelLoad(failing);
            manager.CancelLoad(following);
        }
    }

    [TestCase]
    public void BackgroundFailureIsObservedOnlyOnce()
    {
        var task = new ResourceBackgroundTask(new TestResource(),
            Task.FromException(new InvalidOperationException("Expected resource test failure")),
            ResourceBackgroundPhase.Load);

        AssertThrown(task.ObserveCompletion).IsInstanceOf<InvalidOperationException>();
        task.ObserveCompletion();
        AssertThat(task.CompletionObserved).IsTrue();
    }

    private static void BlockUntilReleased(ManualResetEventSlim started, ManualResetEventSlim release)
    {
        started.Set();
        if (!release.Wait(TimeSpan.FromSeconds(10)))
            throw new TimeoutException("Test did not release the resource operation");
    }

    private static async Task WaitFrames(int count)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        for (int i = 0; i < count; ++i)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var timeout = Stopwatch.StartNew();
        var tree = (SceneTree)Engine.GetMainLoop();

        while (!condition())
        {
            if (timeout.Elapsed > TimeSpan.FromSeconds(5))
                throw new TimeoutException("Resource loading test did not complete within five seconds");

            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private sealed class TestResource : IResource
    {
        private int loadCount;
        private volatile bool cancelRequested;
        private volatile bool loadReturned;
        private volatile bool prepareReturned;

        public TestResource()
        {
            OnComplete = _ =>
            {
                ++CompletionCount;
                CompletionThread = Environment.CurrentManagedThreadId;
            };
        }

        public bool RequiresSyncLoad { get; init; }
        public bool UsesPostProcessing { get; init; }
        public bool RequiresSyncPostProcess { get; init; }

        public bool CancelRequested
        {
            get => cancelRequested;
            set => cancelRequested = value;
        }

        public float EstimatedTimeRequired => 0;
        public bool LoadingPrepared { get; set; }
        public bool Loaded { get; private set; }
        public string Identifier => "Resource loading test";
        public Action<IResource>? OnComplete { get; set; }
        public bool LoadedDuringLoad { get; set; } = true;
        public Action? DuringPrepare { get; init; }
        public Action? DuringLoad { get; set; }
        public Action? DuringPostProcess { get; set; }
        public int LoadCount => Volatile.Read(ref loadCount);
        public bool LoadReturned => loadReturned;
        public bool PrepareReturned => prepareReturned;
        public int CompletionCount { get; private set; }
        public int CompletionThread { get; private set; }
        public int PostProcessCount { get; private set; }
        public int UnloadCount { get; private set; }
        public int UnloadThread { get; private set; }

        public void PrepareLoading()
        {
            try
            {
                DuringPrepare?.Invoke();
            }
            finally
            {
                prepareReturned = true;
            }
        }

        public void Load()
        {
            loadReturned = false;
            try
            {
                Interlocked.Increment(ref loadCount);
                Loaded = LoadedDuringLoad;
                DuringLoad?.Invoke();
            }
            finally
            {
                loadReturned = true;
            }
        }

        public void PerformPostProcessing()
        {
            ++PostProcessCount;
            Loaded = true;
            DuringPostProcess?.Invoke();
        }

        public void UnLoad()
        {
            ++UnloadCount;
            UnloadThread = Environment.CurrentManagedThreadId;
            Loaded = false;
        }
    }
}
