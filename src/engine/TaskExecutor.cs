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
    private static readonly TaskExecutor SingletonInstance = new();

    private readonly object threadNotifySync = new();

    private readonly ConcurrentQueue<ThreadCommand> queuedTasks = new();

    private readonly List<Task> mainThreadTaskStorage = new();

    private bool running = true;
    private int currentThreadCount;

    private int queuedParallelRunnableCount;

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
    ///   How many tasks are used by ECS operations. +1 is here as the main thread also is used
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: should this be limited for some systems where there would be very few entities per thread?
    ///   </para>
    /// </remarks>
    public int DegreeOfParallelism => currentThreadCount + 1;

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

    // TODO: add a variant that allows adding multiple tasks at once
    /// <summary>
    ///   Sends a new task to be executed
    /// </summary>
    public void AddTask(Task task, bool wakeWorkerThread = true)
    {
        queuedTasks.Enqueue(new ThreadCommand(ThreadCommand.Type.Task, task));

        if (wakeWorkerThread)
            NotifyNewTasksAdded(1);
    }

    /// <summary>
    ///   Runs an ECS library runnable on the main thread and the available executors
    /// </summary>
    public void Run(IParallelRunnable runnable)
    {
        int maxIndex = DegreeOfParallelism - 1;

        if (Interlocked.Exchange(ref queuedParallelRunnableCount, maxIndex) != 0)
            throw new Exception("TaskExecutor got into an inconsistent state while running ParallelRunnable tasks");

        for (int i = 0; i < maxIndex; ++i)
        {
            queuedTasks.Enqueue(new ThreadCommand(runnable, i, maxIndex));
        }

        NotifyNewTasksAdded(maxIndex);

        // Main thread runs at the max index
        runnable.Run(maxIndex, maxIndex);

        Interlocked.MemoryBarrier();

        while (queuedParallelRunnableCount > 0)
        {
            Interlocked.MemoryBarrier();

            // TODO: add this when we can to reduce hyperthreading resource use while waiting
            // System.Runtime.Intrinsics.X86.X86Base.Pause();
        }

#if DEBUG
        if (queuedParallelRunnableCount != 0)
            throw new Exception("After waiting for parallel runnables count got out of sync");
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
                    queuedTasks.Enqueue(new ThreadCommand(ThreadCommand.Type.Quit, null));
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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

        queuedTasks.Enqueue(new ThreadCommand(ThreadCommand.Type.Quit, null));
        NotifyNewTasksAdded(1);

        --currentThreadCount;
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
            if (noWorkCounter > 1000)
            {
                lock (threadNotifySync)
                {
                    Monitor.Wait(threadNotifySync, 5);
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

            // Make sure task exceptions aren't ignored.
            // TODO: it used to be that not all places properly waited for tasks, that's why this code is here
            // but now some places actually want to handle the task exceptions themselves, so this should
            // be removed after making sure no places ignore the exceptions
            if (command.Task.Exception != null)
                GD.Print("Background task caused an exception: ", command.Task.Exception);
        }
        else if (command.CommandType == ThreadCommand.Type.ParallelRunnable)
        {
            try
            {
                command.ParallelRunnable!.Run(command.ParallelIndex, command.MaxIndex);
            }
            catch (Exception exception)
            {
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
        else
        {
            throw new Exception("invalid task type");
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

        public ThreadCommand(Type commandType, Task? task)
        {
            CommandType = commandType;
            Task = task;

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

            Task = null;
        }

        public enum Type
        {
            Task,
            ParallelRunnable,
            Quit,
        }
    }
}
