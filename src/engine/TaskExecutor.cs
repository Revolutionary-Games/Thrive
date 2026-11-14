using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Godot;
using Schedulers;
using Environment = System.Environment;
using Thread = System.Threading.Thread;

/// <summary>
///   Manages the running of a reasonable number of parallel tasks at once
/// </summary>
#pragma warning disable CA1001 // singleton anyway
public class TaskExecutor
#pragma warning restore CA1001
{
    private const int ThreadSleepAfterNoWorkFor = 160;

    private const int ParallelForBatchesPerThread = 4;

    private static readonly TaskExecutor SingletonInstance = new();

    private readonly object threadNotifySync = new();

    private readonly ConcurrentQueue<ThreadCommand> queuedTasks = new();

    private bool running = true;
    private int currentThreadCount;
    private int usedNativeTaskCount;

    /// <summary>
    ///   For naming the created threads.
    /// </summary>
    private int threadCounter;

    static TaskExecutor()
    {
    }

    private TaskExecutor(int overrideParallelCount = -1)
    {
        if (overrideParallelCount >= 0)
        {
            ParallelTasks = overrideParallelCount;
            SetNativeThreadCount(overrideParallelCount);
        }
        else
        {
            ReApplyThreadCount();
        }

        Thread.CurrentThread.Name = "TMain";

        // Start Arch multithreading
        var jobScheduler = new JobScheduler(new JobScheduler.Config
        {
            ThreadPrefixName = "Arch",

            // These thread counts about make sense now.
            // Because the calling thread is blocked whenever there are parallel operations running.
            // TODO: Though it would be nice again to find a way to share threads or some other mechanism to ensure
            // these don't run at once (though as these are for purely gameplay systems, they don't usually run at the
            // same time as something else that is taking up all of the normal threads)
            ThreadCount = Math.Clamp(ParallelTasks - 2, 2, 4),
            MaxExpectedConcurrentJobs = 64,
#if DEBUG
            StrictAllocationMode = true,
#else
            StrictAllocationMode = false,
#endif
        });
        World.SharedJobScheduler = jobScheduler;

        GD.Print("TaskExecutor started with parallel job count: ", ParallelTasks);
    }

    // Max is used here to ensure that clamp calls that use this value cannot fail
    public static int CPUCount => Math.Max(Environment.ProcessorCount, 1);
    public static int MinimumThreadCount => Settings.Instance.RunAutoEvoDuringGamePlay.Value ? 2 : 1;
    public static int MaximumThreadCount => CPUCount;

    public static TaskExecutor Instance => SingletonInstance;

    public int ParallelTasks
    {
        get => currentThreadCount;
        set
        {
            if (currentThreadCount == value)
                return;

            while (currentThreadCount > value)
            {
                QuitThread();
            }

            while (currentThreadCount < value)
            {
                SpawnThread();
            }
        }
    }

    public int NativeTasks
    {
        get => usedNativeTaskCount;
        set
        {
            if (usedNativeTaskCount == value)
                return;

            usedNativeTaskCount = Math.Clamp(value, 1, MaximumThreadCount);

            NativeInterop.NotifyWantedThreadCountChanged(usedNativeTaskCount);
        }
    }

    /// <summary>
    ///   Computes how many threads there should be by default
    /// </summary>
    /// <param name="hyperthreading">
    ///   True, if hyperthreading is on.
    ///   There is no platform-independent way to get this in C#.
    /// </param>
    /// <param name="autoEvoDuringGameplay">If true, reserves extra (minimum thread) for auto-evo</param>
    /// <returns>The number of threads to use</returns>
    public static int GetWantedThreadCount(bool hyperthreading, bool autoEvoDuringGameplay)
    {
        int targetTaskCount = CPUCount;

        // The divisible by 2 check here makes sure there are 2n number of threads (where n is the number of real cores
        // this holds for desktop hyperthreading where there are always 2 threads per core)
        if (hyperthreading && targetTaskCount % 2 == 0)
            targetTaskCount /= 2;

        int max = MaximumThreadCount;
        if (targetTaskCount > max)
            targetTaskCount = max;

        // There needs to be 2 threads as when auto-evo is running, it hogs one thread
        if (autoEvoDuringGameplay && targetTaskCount < 2)
        {
            targetTaskCount = 2;
        }

        return targetTaskCount;
    }

    public static int CalculateNativeThreadCountFromManagedThreads(int managedCount)
    {
        // Reduce thread count when low number of threads are used
        // TODO: tweak these low task number threads if necessary
        if (managedCount <= 3)
            return 1;

        if (managedCount <= 4)
            return 2;

        if (managedCount <= 6)
            return 3;

        int targetTaskCount = Math.Clamp((int)Math.Round(managedCount * 0.5f), 2, CPUCount);

        // Cap the maximum threads as there isn't that much benefit from too many threads
        // And in fact in the benchmark these hurt the first part score
        if (targetTaskCount > 8)
            return 8;

        return targetTaskCount;
    }

    // TODO: add a variant that allows adding multiple tasks at once
    /// <summary>
    ///   Sends a new task to be executed
    /// </summary>
    public void AddTask(Task task, bool wakeWorkerThread = true)
    {
        queuedTasks.Enqueue(new ThreadCommand(task));

        if (wakeWorkerThread)
            NotifyNewTasksAdded(1);
    }

    /// <summary>
    ///   Runs a list of tasks and waits for them to complete. The
    ///   first task is run on the calling thread before waiting. Failures propagate only after accepted work stops.
    /// </summary>
    /// <param name="tasks">
    ///   List of tasks to execute and wait to finish. Not modified but must be List to avoid a memory allocation in
    ///   the foreach. Entries must be distinct, unstarted tasks; do not mutate the list or start tasks elsewhere.
    /// </param>
    /// <param name="runExtraTasksOnCallingThread">
    ///   If true, the main thread processes tasks while there are queued tasks. Set this to false if you want to wait
    ///   only for the task list to complete. If this is true, then this call blocks until all tasks (for example,
    ///   ones queued from another thread while this method is executing) are complete, which may be unwanted in
    ///   some cases.
    /// </param>
    /// <param name="catchErrors">
    ///   If true, task errors are shown to the player. Scheduling failures still propagate after accepted work stops.
    /// </param>
    public void RunTasks(List<Task> tasks, bool runExtraTasksOnCallingThread = false, bool catchErrors = false)
    {
        var taskCount = tasks.Count;
        if (taskCount == 0)
            return;

        // Reject null entries before accepting any work. The caller owns this stable list and its unstarted tasks.
        foreach (var task in tasks)
            ArgumentNullException.ThrowIfNull(task);

        var firstTask = tasks[0];
        int queuedTaskCount = 0;
        bool firstTaskRan = false;
        bool schedulingFailed = false;
        List<Exception>? errors = null;

        try
        {
            // Queue all but the first task, recording only entries actually accepted by the executor.
            for (int i = 1; i < taskCount; ++i)
            {
                AddTask(tasks[i], false);
                ++queuedTaskCount;
            }

            NotifyAllNewTasksAdded();
            firstTask.RunSynchronously();
            firstTaskRan = true;

            if (runExtraTasksOnCallingThread)
            {
                // Helping retains the existing policy of running any queued work, including other callers' tasks.
                // This does not wait for future submissions after the queue becomes empty.
                while (queuedTasks.TryDequeue(out var command))
                {
                    if (command.CommandType == ThreadCommand.Type.Quit)
                    {
                        queuedTasks.Enqueue(new ThreadCommand(ThreadCommand.Type.Quit));
                        break;
                    }

                    if (ProcessNormalCommand(command))
                        break;
                }
            }
        }
        catch (Exception e)
        {
            schedulingFailed = true;
            errors ??= new List<Exception>();
            errors.Add(e);

            // A partially submitted batch still needs its workers woken before we wait for the accepted prefix.
            NotifyAllNewTasksAdded();
        }

        // A cancelled first task can fail to start. Never wait for an unstarted first task after submission fails.
        // Other accepted tasks must be waited even if they are still queued with TaskStatus.Created.
        int firstTaskToWait = firstTaskRan || firstTask.IsCompleted ? 0 : 1;
        for (int i = firstTaskToWait; i <= queuedTaskCount; ++i)
        {
            try
            {
                // TODO: so apparently this Wait call can allocate memory, in SpinThenBlockingWait which eventually
                // calls EnsureLockObjectCreated
                tasks[i].Wait();
            }
            catch (Exception e)
            {
                errors ??= new List<Exception>();
                errors.Add(e);
            }
        }

        if (errors == null)
            return;

        // Report errors only after all accepted work has stopped, including when error reporting itself fails.
        if (!catchErrors || schedulingFailed)
        {
            var failure = new AggregateException(errors).Flatten();

            if (!schedulingFailed)
                GD.PrintErr($"Error encountered from a waited task on the primary waiting thread: {failure}");

            throw failure;
        }

        foreach (var error in errors)
        {
            GD.PrintErr($"Error encountered from a waited task on the primary waiting thread: {error}");
            LogInterceptor.ForwardCaughtError(error, "Error from waited background task");
        }
    }

    /// <summary>
    ///   Runs a loop body over an index range in parallel and waits for it to complete. The calling thread processes
    ///   batches as well, so this is safe to call from a task running on this executor.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The range is split into contiguous batches that participating threads claim one at a time until none are
    ///     left. Unlike <see cref="RunTasks"/>, this doesn't need a task or closure per batch. Only one small object
    ///     is allocated per call (and none when the loop runs only on the calling thread).
    ///   </para>
    ///   <para>
    ///     If the body throws, batches not yet started are skipped and the first exception is thrown wrapped in an
    ///     <see cref="AggregateException"/> after all started batches have finished. Worker threads are not affected
    ///     by the failure.
    ///   </para>
    /// </remarks>
    /// <param name="fromInclusive">First index of the loop</param>
    /// <param name="toExclusive">One past the last index of the loop. Nothing is done if this is not larger.</param>
    /// <param name="body">The loop body that processes the batches</param>
    /// <param name="minBatchSize">
    ///   Smallest number of indices processed in one batch. Increase this for cheap loop bodies to reduce the
    ///   overhead of claiming batches.
    /// </param>
    /// <typeparam name="TBody">The IParallelForBody struct</typeparam>
    public void ParallelFor<TBody>(int fromInclusive, int toExclusive, TBody body, int minBatchSize = 1)
        where TBody : struct, IParallelForBody
    {
        if (minBatchSize < 1)
            throw new ArgumentOutOfRangeException(nameof(minBatchSize), "Batch size must be at least 1");

        if (toExclusive <= fromInclusive)
            return;

        long range = (long)toExclusive - fromInclusive;

        int workerThreads = currentThreadCount;

        long wantedBatches = (workerThreads + 1L) * ParallelForBatchesPerThread;
        long batchSize = Math.Max(minBatchSize, (range + wantedBatches - 1) / wantedBatches);
        long batchCount = (range + batchSize - 1) / batchSize;

        // The calling thread takes part as well, so one batch less needs helpers
        int helperCount = (int)Math.Min(workerThreads, batchCount - 1);

        if (helperCount <= 0)
        {
            try
            {
                body.Execute(fromInclusive, toExclusive);
            }
            catch (Exception e)
            {
                throw new AggregateException(e);
            }

            return;
        }

        var work = new ParallelForWork<TBody>(fromInclusive, toExclusive, (int)batchSize, (int)batchCount, body);

        for (int i = 0; i < helperCount; ++i)
        {
            queuedTasks.Enqueue(new ThreadCommand(work));
        }

        NotifyNewTasksAdded(helperCount);

        work.RunBatches();
        work.WaitForAllBatches();

        var error = work.FirstError;
        if (error != null)
            throw new AggregateException(error);
    }

    public void ReApplyThreadCount()
    {
        var settings = Settings.Instance;

        if (settings.UseManualThreadCount.Value)
        {
            ParallelTasks = Math.Clamp(settings.ThreadCount.Value, MinimumThreadCount, MaximumThreadCount);
        }
        else
        {
            ParallelTasks = GetWantedThreadCount(settings.AssumeCPUHasHyperthreading.Value,
                settings.RunAutoEvoDuringGamePlay.Value);
        }

        SetNativeThreadCount(ParallelTasks);
    }

    public void OnProgramExit()
    {
        // Stop Arch scheduling as it might keep the process otherwise alive incorrectly
        StopArch();

        running = false;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Stop Arch scheduling (though as a singleton this class is never really disposed)
            StopArch();
        }
    }

    private void StopArch()
    {
        // Stop Arch scheduling (though as a singleton this class is never really disposed)
        var scheduler = World.SharedJobScheduler;
        World.SharedJobScheduler = null;
        scheduler?.Dispose();
    }

    private void SpawnThread()
    {
        var thread = new Thread(RunExecutorThread)
        {
            IsBackground = true,
            Name = $"TaskThread_{++threadCounter}",
        };
        thread.Start();
        ++currentThreadCount;
    }

    private void QuitThread()
    {
        if (currentThreadCount <= 0)
            return;

        queuedTasks.Enqueue(new ThreadCommand(ThreadCommand.Type.Quit));
        NotifyNewTasksAdded(1);

        --currentThreadCount;
    }

    private void SetNativeThreadCount(int parallelTasks)
    {
        var settings = Settings.Instance;

        int result;

        if (settings.UseManualNativeThreadCount.Value)
        {
            result = settings.NativeThreadCount.Value;
        }
        else
        {
            result = CalculateNativeThreadCountFromManagedThreads(parallelTasks);
        }

        NativeTasks = result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NotifyAllNewTasksAdded()
    {
        lock (threadNotifySync)
        {
            Monitor.PulseAll(threadNotifySync);
        }

        CPUHelpers.SignalEvent();
    }

    private void NotifyNewTasksAdded(int count)
    {
        if (count < 0)
        {
            GD.PrintErr($"Too low count passed to {nameof(NotifyNewTasksAdded)}");
            return;
        }

        if (count == 1)
        {
            lock (threadNotifySync)
            {
                Monitor.Pulse(threadNotifySync);
            }

            CPUHelpers.SignalEvent();
            return;
        }

        // If count is more than threads, don't bother sending that many
        if (count > currentThreadCount)
            count = currentThreadCount;

        lock (threadNotifySync)
        {
            for (int i = 0; i < count; ++i)
            {
                Monitor.Pulse(threadNotifySync);
            }
        }

        CPUHelpers.SignalEvent();
    }

    private void RunExecutorThread()
    {
        // This is used to sleep only when no new work is arriving to allow this thread to sleep only sometimes
        int noWorkCounter = 0;

        // This whole thing is in a try-catch now to try to solve issue of background threads disappearing without a
        // trace
        try
        {
            while (running)
            {
                // Wait a bit before going to sleep
                if (noWorkCounter > ThreadSleepAfterNoWorkFor)
                {
                    lock (threadNotifySync)
                    {
                        // This timeout here is just for safety to avoid locking up, reducing this doesn't seem to have
                        // any performance impact. This is set now for a balance of threads not being able to be stuck
                        // too long in case the wake-up thread notifying is not working
                        Monitor.Wait(threadNotifySync, 10);
                    }
                }

                if (queuedTasks.TryDequeue(out ThreadCommand command))
                {
                    if (command.CommandType == ThreadCommand.Type.Quit)
                    {
                        return;
                    }

                    if (ProcessNormalCommand(command))
                        return;

                    noWorkCounter = 0;
                }
                else
                {
                    ++noWorkCounter;

                    // Reduce hyperthreading resource use while just busy looping
                    CPUHelpers.HyperThreadPause();
                }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("Background thread failed to run, this is a serious problem and may deadlock the game: ", e);
        }
    }

    private bool ProcessNormalCommand(ThreadCommand command)
    {
        if (command.CommandType == ThreadCommand.Type.Task)
        {
            try
            {
                // Task may not be null when the command type was a task
                command.Task!.RunSynchronously();
            }
            catch (TaskSchedulerException exception)
            {
                GD.Print("Background task failed due to thread exiting: ", exception.Message);
                return true;
            }
            catch (Exception e)
            {
                // This shouldn't hit in normal circumstances, but people have been submitting crash reports where
                // an exception likely directly is caused by the run synchronous call

                LogInterceptor.ForwardCaughtError(e);

                GD.PrintErr("Trying to run background task failed with exception: ", e);
                return true;
            }

            // Make sure task exceptions aren't ignored.
            // TODO: if some places want to catch the exception themselves we should make sure that this still allows
            // those places to do that
            if (command.Task.Exception != null)
            {
                // Forward to the GUI so that the player sees it
                LogInterceptor.ForwardCaughtError(command.Task.Exception);

                GD.PrintErr("Background task caused an exception: ", command.Task.Exception);
            }
        }
        else if (command.CommandType == ThreadCommand.Type.ParallelForHelper)
        {
            command.ParallelForWork!.RunBatches();
        }
        else if (command.CommandType == ThreadCommand.Type.Invalid)
        {
            GD.PrintErr("Something has queued a task of invalid type. " +
                "Ignoring it, but the underlying bug needs to be fixed.");
        }
        else
        {
            throw new Exception("Task command type value out of range");
        }

        return false;
    }

    private struct ThreadCommand
    {
        public readonly Task? Task;

        public readonly ParallelForWork? ParallelForWork;

        public readonly Type CommandType;

        public ThreadCommand(Task task)
        {
            CommandType = Type.Task;
            Task = task;
            ParallelForWork = null;

            if (Task == null)
                throw new ArgumentNullException(nameof(task), "Task must be provided to this constructor");
        }

        public ThreadCommand(ParallelForWork work)
        {
            CommandType = Type.ParallelForHelper;
            ParallelForWork = work;
            Task = null;

            if (ParallelForWork == null)
                throw new ArgumentNullException(nameof(work), "Work must be provided to this constructor");
        }

        public ThreadCommand(Type commandType)
        {
            CommandType = commandType;

            if (CommandType != Type.Quit)
                throw new ArgumentException("This constructor is only allowed to create quit type commands");

            Task = null;
            ParallelForWork = null;
        }

        public enum Type
        {
            // Default-initialise type to invalid to catch errors caused by default initialisation of this class
            Invalid = 0,
            Task,
            ParallelForHelper,
            Quit,
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 192)]
    private struct BatchCounters
    {
        [FieldOffset(64)]
        public int NextBatch;

        [FieldOffset(128)]
        public int CompletedBatches;
    }

    /// <summary>
    ///   Shared state of one <see cref="ParallelFor{TBody}"/> call.
    /// </summary>
    private abstract class ParallelForWork
    {
        public abstract Exception? FirstError { get; }

        /// <summary>
        ///   Claims and runs batches until there are none left. Never throws because of the loop body.
        /// </summary>
        public abstract void RunBatches();
    }

    private sealed class ParallelForWork<TBody> : ParallelForWork
        where TBody : struct, IParallelForBody
    {
        private readonly int fromInclusive;
        private readonly int toExclusive;
        private readonly int batchSize;
        private readonly int batchCount;

        private TBody body;

        private BatchCounters counters;

        private volatile bool failed;

        private Exception? firstError;

        public ParallelForWork(int fromInclusive, int toExclusive, int batchSize, int batchCount, TBody body)
        {
            this.fromInclusive = fromInclusive;
            this.toExclusive = toExclusive;
            this.batchSize = batchSize;
            this.batchCount = batchCount;
            this.body = body;
        }

        public override Exception? FirstError => Volatile.Read(ref firstError);

        public override void RunBatches()
        {
            while (true)
            {
                int batch = Interlocked.Increment(ref counters.NextBatch) - 1;

                if (batch >= batchCount)
                    return;

                // After a failure, the remaining batches are only counted as done so the caller can stop waiting
                if (!failed)
                {
                    long start = fromInclusive + (long)batch * batchSize;
                    int end = (int)Math.Min(start + batchSize, toExclusive);

                    try
                    {
                        body.Execute((int)start, end);
                    }
                    catch (Exception e)
                    {
                        Interlocked.CompareExchange(ref firstError, e, null);
                        failed = true;
                    }
                }

                // Full fence: makes the batch's results visible to the thread waiting for completion
                Interlocked.Increment(ref counters.CompletedBatches);
            }
        }

        /// <summary>
        ///   Waits until batches claimed by other threads have finished. Must be called only by the thread that
        ///   started the loop and only after it has run out of batches to claim in <see cref="RunBatches"/>.
        /// </summary>
        public void WaitForAllBatches()
        {
            // Batches are expected to be short, so this spins and yields instead of blocking. Sleeping is disabled
            // as a minimum sleep of a millisecond is a long time for loops that run each frame.
            var spinner = default(SpinWait);

            while (Volatile.Read(ref counters.CompletedBatches) < batchCount)
            {
                spinner.SpinOnce(-1);
            }

            body = default;
        }
    }
}
