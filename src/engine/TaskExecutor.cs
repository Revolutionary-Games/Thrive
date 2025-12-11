using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    private static readonly TaskExecutor SingletonInstance = new();

    private readonly object threadNotifySync = new();

    private readonly ConcurrentQueue<ThreadCommand> queuedTasks = new();

    private readonly List<Task> mainThreadTaskStorage = new();

    private bool running = true;
    private int currentThreadCount;
    private int usedNativeTaskCount;

    private volatile int queuedParallelRunnableCount;

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
    ///   first task is run on the calling thread before waiting.
    /// </summary>
    /// <param name="tasks">
    ///   List of tasks to execute and wait to finish. Not modified but must be List to avoid a memory allocation in
    ///   the foreach.
    /// </param>
    /// <param name="runExtraTasksOnCallingThread">
    ///   If true, the main thread processes tasks while there are queued tasks. Set this to false if you want to wait
    ///   only for the task list to complete. If this is true, then this call blocks until all tasks (for example,
    ///   ones queued from another thread while this method is executing) are complete, which may be unwanted in
    ///   some cases.
    /// </param>
    public void RunTasks(List<Task> tasks, bool runExtraTasksOnCallingThread = false)
    {
        // Queue all but the first task
        Task? firstTask = null;

        foreach (var task in tasks)
        {
            if (firstTask != null)
            {
                AddTask(task, false);
            }
            else
            {
                firstTask = task;
            }

            mainThreadTaskStorage.Add(task);
        }

        if (firstTask == null)
        {
            // No tasks given to execute. Should we throw here?
            return;
        }

        // Should be fine to wake up all the threads as the main thread is going to also be busy,
        // so this is purely to be able to run things at full speed
        NotifyAllNewTasksAdded();

        // Run the first task on this thread
        firstTask.RunSynchronously();

        // TODO: it should be plausible to make it so that only tasks in "tasks" are ran on the calling thread
        // but due to implementation difficulty that is not currently done, instead this parameter is used
        // to give control to the caller if they want to accept the tradeoffs regarding the current implementation
        if (runExtraTasksOnCallingThread)
        {
            // Process tasks also on the main thread

            // This should be the non-blocking variant, so the current thread won't wait for more tasks,
            // just immediately exits the loop if there are no tasks to run
            while (queuedTasks.TryDequeue(out ThreadCommand command))
            {
                // If we take out a quit command here, we need to put it back for the actual threads to get and break
                if (command.CommandType == ThreadCommand.Type.Quit)
                {
                    queuedTasks.Enqueue(new ThreadCommand(ThreadCommand.Type.Quit));
                    break;
                }

                if (ProcessNormalCommand(command))
                    break;
            }
        }

        // TODO: if Quit is called from another thread here, this thread will become permanently stuck waiting for the
        // tasks

        // Wait for all given tasks to complete
        foreach (var task in mainThreadTaskStorage)
        {
            // TODO: so apparently this Wait call can allocate memory, in SpinThenBlockingWait which eventually calls
            // EnsureLockObjectCreated
            task.Wait();
        }

        mainThreadTaskStorage.Clear();
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
        else if (command.CommandType == ThreadCommand.Type.ParallelRunnable)
        {
            try
            {
                throw new Exception("TODO: reimplement parallel runnable");

                // command.ParallelRunnable!.Run(command.ParallelIndex, command.MaxIndex);
            }
            catch (Exception e)
            {
                LogInterceptor.ForwardCaughtError(e);

                GD.PrintErr("Background ParallelRunnable failed due to: ", e);
                return true;
            }
            finally
            {
                Interlocked.Decrement(ref queuedParallelRunnableCount);
            }
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

        public readonly Type CommandType;

        public ThreadCommand(Task task)
        {
            CommandType = Type.Task;
            Task = task;

            if (Task == null)
                throw new ArgumentNullException(nameof(task), "Task must be provided to this constructor");
        }

        public ThreadCommand(Type commandType)
        {
            CommandType = commandType;

            if (CommandType != Type.Quit)
                throw new ArgumentException("This constructor is only allowed to create quit type commands");

            Task = null;
        }

        public enum Type
        {
            // Default-initialise type to invalid to catch errors caused by default initialisation of this class
            Invalid = 0,
            Task,
            ParallelRunnable,
            Quit,
        }
    }
}
