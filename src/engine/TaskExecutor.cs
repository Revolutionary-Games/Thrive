using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Environment = System.Environment;
using Thread = System.Threading.Thread;

/// <summary>
///   Manages running a reasonable number of parallel tasks at once
/// </summary>
#pragma warning disable CA1001 // singleton anyway
public class TaskExecutor
#pragma warning restore CA1001
{
    private static readonly TaskExecutor SingletonInstance = new TaskExecutor();

    private readonly BlockingCollection<ThreadCommand> queuedTasks =
        new BlockingCollection<ThreadCommand>();

    private bool running = true;
    private int currentThreadCount;

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

    /// <summary>
    ///   Sends a new task to be executed
    /// </summary>
    public void AddTask(Task task)
    {
        queuedTasks.Add(new ThreadCommand(ThreadCommand.Type.Task, task));
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

        var enumerated = tasks.ToList();

        if (enumerated.Count < 1)
        {
            // No tasks given to execute. Should we throw here?
            return;
        }

        foreach (var task in enumerated)
        {
            if (firstTask != null)
            {
                AddTask(task);
            }
            else
            {
                firstTask = task;
            }
        }

        // Run the first task on this thread
        // This should always be non-null given the check above, but I don't feel like changing this now to not
        // have to test this extensively
        firstTask?.RunSynchronously();

        // TODO: it should be plausible to make it so that only tasks in "tasks" are ran on the calling thread
        // but due to implementation difficulty that is not currently done, instead this parameter is used
        // to give control to the caller if they want to accept the tradeoffs regarding the current implementation
        if (runExtraTasksOnCallingThread)
        {
            // Process tasks also on the main thread

            // This should be the non-blocking variant so the current thread won't wait for more tasks,
            // just immediately exits the loop if there are no tasks to run
            while (queuedTasks.TryTake(out ThreadCommand command))
            {
                // If we take out a quit command here, we need to put it back for the actual threads to get and break
                if (command.CommandType == ThreadCommand.Type.Quit)
                {
                    queuedTasks.Add(new ThreadCommand(ThreadCommand.Type.Quit, null));
                    break;
                }

                if (ProcessNormalCommand(command))
                    break;
            }
        }

        // TODO: if Quit is called from another thread here, this thread will become permanently stuck waiting for the
        // tasks

        // Wait for all given tasks to complete
        foreach (var task in enumerated)
        {
            task.Wait();
        }
    }

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
    }

    private void SpawnThread()
    {
        var thread = new Thread(RunExecutorThread);
        thread.IsBackground = true;
        thread.Name = $"TaskThread_{++threadCounter}";
        thread.Start();
        ++currentThreadCount;
    }

    private void QuitThread()
    {
        if (currentThreadCount <= 0)
            return;

        queuedTasks.Add(new ThreadCommand(ThreadCommand.Type.Quit, null));

        --currentThreadCount;
    }

    private void RunExecutorThread()
    {
        while (running)
        {
            if (queuedTasks.TryTake(out ThreadCommand command, 30000))
            {
                if (command.CommandType == ThreadCommand.Type.Quit)
                {
                    return;
                }

                if (ProcessNormalCommand(command))
                    return;
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
        else
        {
            throw new Exception("invalid task type");
        }

        return false;
    }

    private struct ThreadCommand
    {
        public Type CommandType;
        public Task? Task;

        public ThreadCommand(Type commandType, Task? task)
        {
            CommandType = commandType;
            Task = task;
        }

        public enum Type
        {
            Task,
            Quit,
        }
    }
}
