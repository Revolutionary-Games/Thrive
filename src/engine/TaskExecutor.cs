using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DefaultEcs.Threading;
using Godot;
using Environment = System.Environment;
using Thread = System.Threading.Thread;

/// <summary>
///   Manages running a reasonable number of parallel tasks at once
/// </summary>
#pragma warning disable CA1001 // singleton anyway
public class TaskExecutor : IParallelRunner
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

    private int queuedParallelRunnableCount;

    private int ecsThrottling = 4;

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

        // Mono doesn't have this for some reason
        // Thread.CurrentThread.Name = "main";
        GD.Print("TaskExecutor started with parallel job count: ", ParallelTasks);
    }

    public static int CPUCount => Environment.ProcessorCount;
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

    /// <summary>
    ///   Set to a value lower than <see cref="ParallelTasks"/> to throttle how many threads the ECS system is allowed
    ///   to use. This is because the ECS runner always uses all threads even if each thread would only have a couple
    ///   of entities to process.
    /// </summary>
    public int ECSThrottling
    {
        get => ecsThrottling;
        set
        {
            if (value > 0)
            {
                ecsThrottling = value;
            }
            else
            {
                ecsThrottling = 1;
            }
        }
    }

    /// <summary>
    ///   How many tasks are used by ECS operations. +1 is here as the main thread also is used
    /// </summary>
    public int DegreeOfParallelism => Math.Min(currentThreadCount + 1, ECSThrottling);

    public int NativeTasks
    {
        get => usedNativeTaskCount;
        set
        {
            if (usedNativeTaskCount == value)
                return;

            usedNativeTaskCount = Mathf.Clamp(value, 1, MaximumThreadCount);

            NativeInterop.NotifyWantedThreadCountChanged(usedNativeTaskCount);
        }
    }

    /// <summary>
    ///   Computes how many threads there should be by default
    /// </summary>
    /// <param name="hyperthreading">
    ///   True if hyperthreading is on. There is no platform independent way to get this in C#.
    /// </param>
    /// <param name="autoEvoDuringGameplay">If true, reserves extra (minimum thread) for auto-evo</param>
    /// <returns>The number of threads to use</returns>
    public static int GetWantedThreadCount(bool hyperthreading, bool autoEvoDuringGameplay)
    {
        int targetTaskCount = CPUCount;

        // The divisible by 2 check here makes sure there are 2n number of threads (where n is the number of real cores
        // this holds for desktop hyperthreading where there's always 2 threads per core)
        if (hyperthreading && targetTaskCount % 2 == 0)
            targetTaskCount /= 2;

        int max = MaximumThreadCount;
        if (targetTaskCount > max)
            targetTaskCount = max;

        // There needs to be 2 threads as when auto-evo is running it hogs one thread
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

        int targetTaskCount = Mathf.Clamp((int)Math.Round(managedCount * 0.5f), 2, CPUCount);

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
    ///   Runs an ECS library runnable on the current thread and the available executors (waits for all ECS runnables
    ///   to complete, even from other threads)
    /// </summary>
    public void Run(IParallelRunnable runnable)
    {
        int maxIndex = DegreeOfParallelism - 1;

        Interlocked.Add(ref queuedParallelRunnableCount, maxIndex);

        for (int i = 0; i < maxIndex; ++i)
        {
            queuedTasks.Enqueue(new ThreadCommand(runnable, i, maxIndex));
        }

        NotifyNewTasksAdded(maxIndex);

        // Current thread runs at the max index
        runnable.Run(maxIndex, maxIndex);

        // If only ran on the main thread can exit early, no need to try to wait
        if (maxIndex < 1)
            return;

        Interlocked.MemoryBarrier();

        while (queuedParallelRunnableCount > 0)
        {
            Interlocked.MemoryBarrier();

            // TODO: add this when we can to reduce hyperthreading resource use while waiting
            // System.Runtime.Intrinsics.X86.X86Base.Pause();
        }

#if DEBUG
        if (queuedParallelRunnableCount < 0)
            throw new Exception("After waiting for parallel runnables count got negative");
#endif
    }

    /// <summary>
    ///   Runs a list of tasks and waits for them to complete. The
    ///   first task is ran on the calling thread before waiting.
    /// </summary>
    /// <param name="tasks">List of tasks to execute and wait to finish</param>
    /// <param name="runExtraTasksOnCallingThread">
    ///   If true the main thread processes tasks while there are queued tasks. Set this to false if you want to wait
    ///   only for the tasks list to complete. If this is true then this call blocks until all tasks (for example
    ///   ones queued from another thread while this method is executing) are complete, which may be unwanted in
    ///   some cases.
    /// </param>
    public void RunTasks(IEnumerable<Task> tasks, bool runExtraTasksOnCallingThread = false)
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

        // Should be fine to wake up all the threads as main thread is going to also be busy so this is purely to be
        // able to run things at full speed
        NotifyAllNewTasksAdded();

        // Run the first task on this thread
        firstTask.RunSynchronously();

        // TODO: it should be plausible to make it so that only tasks in "tasks" are ran on the calling thread
        // but due to implementation difficulty that is not currently done, instead this parameter is used
        // to give control to the caller if they want to accept the tradeoffs regarding the current implementation
        if (runExtraTasksOnCallingThread)
        {
            // Process tasks also on the main thread

            // This should be the non-blocking variant so the current thread won't wait for more tasks,
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
            task.Wait();
        }

        mainThreadTaskStorage.Clear();
    }

    // TODO: maybe remove this given the comment in RunTasks?
    public void Quit()
    {
        running = false;
        ParallelTasks = 0;
    }

    public void ReApplyThreadCount()
    {
        var settings = Settings.Instance;

        if (settings.UseManualThreadCount.Value)
        {
            ParallelTasks = Mathf.Clamp(settings.ThreadCount.Value, MinimumThreadCount, MaximumThreadCount);
        }
        else
        {
            ParallelTasks = GetWantedThreadCount(settings.AssumeCPUHasHyperthreading.Value,
                settings.RunAutoEvoDuringGamePlay.Value);
        }

        SetNativeThreadCount(ParallelTasks);
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
        }
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

        while (running)
        {
            // Wait a bit before going to sleep
            if (noWorkCounter > ThreadSleepAfterNoWorkFor)
            {
                lock (threadNotifySync)
                {
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
            }
        }
    }

    private bool ProcessNormalCommand(ThreadCommand command)
    {
        if (command.CommandType == ThreadCommand.Type.Task)
        {
            try
            {
                // Task may not be null when the command type was task
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
                // an exception likely directly is caused by the run synchronously call

#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#endif

                GD.PrintErr("Trying to run background task failed with exception: ", e);
                return true;
            }

            // Make sure task exceptions aren't ignored.
            // TODO: it used to be that not all places properly waited for tasks, that's why this code is here
            // but now some places actually want to handle the task exceptions themselves, so this should
            // be removed after making sure no places ignore the exceptions
            if (command.Task.Exception != null)
            {
#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#endif

                GD.Print("Background task caused an exception: ", command.Task.Exception);
            }
        }
        else if (command.CommandType == ThreadCommand.Type.ParallelRunnable)
        {
            try
            {
                command.ParallelRunnable!.Run(command.ParallelIndex, command.MaxIndex);
            }
            catch (Exception exception)
            {
#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#endif

                // TODO: should this quit the game immediately due to the exception (or pass it to the main thread
                // for example with a field that Run would check after running the tasks)?

                GD.Print("Background ParallelRunnable failed due to: ", exception);
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
        public readonly IParallelRunnable? ParallelRunnable;

        public readonly Type CommandType;
        public readonly int ParallelIndex;
        public readonly int MaxIndex;

        public ThreadCommand(Task task)
        {
            CommandType = Type.Task;
            Task = task;

            if (Task == null)
                throw new ArgumentNullException(nameof(task), "Task must be provided to this constructor");

            ParallelRunnable = null;
            ParallelIndex = 0;
            MaxIndex = 0;
        }

        public ThreadCommand(Type commandType)
        {
            CommandType = commandType;

            if (CommandType != Type.Quit)
                throw new ArgumentException("This constructor is only allowed to create quit type commands");

            Task = null;
            ParallelRunnable = null;
            ParallelIndex = 0;
            MaxIndex = 0;
        }

        public ThreadCommand(IParallelRunnable parallelRunnable, int index, int maxIndex)
        {
            CommandType = Type.ParallelRunnable;
            ParallelRunnable = parallelRunnable;
            ParallelIndex = index;
            MaxIndex = maxIndex;

            // This is inside a debug block as there's never been a crash report with the parallel runnable being
            // null
#if DEBUG
            if (ParallelRunnable == null)
            {
                throw new ArgumentNullException(nameof(parallelRunnable),
                    "Parallel runnable must be provided to this constructor");
            }
#endif

            Task = null;
        }

        public enum Type
        {
            // Default initialize type to invalid in order to catch errors caused by default initialization of
            // this class
            Invalid = 0,
            Task,
            ParallelRunnable,
            Quit,
        }
    }
}
