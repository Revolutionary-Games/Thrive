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
        if (task != null)
        {
            queuedTasks.Add(new ThreadCommand(ThreadCommand.Type.Task, task));
        }
    }

    /// <summary>
    ///   Runs a list of tasks and waits for them to complete. The
    ///   first task is ran on the calling thread before waiting.
    /// </summary>
    public void RunTasks(IEnumerable<Task> tasks)
    {
        // Queue all but the first task
        Task firstTask = null;

        var enumerated = tasks.ToList();
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
        firstTask?.RunSynchronously();

        // Wait for all tasks to complete
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

                if (command.CommandType == ThreadCommand.Type.Task)
                {
                    try
                    {
                        command.Task.RunSynchronously();
                    }
                    catch (TaskSchedulerException exception)
                    {
                        GD.Print("Background task failed due to thread exiting: ", exception.Message);
                        return;
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
            }
        }
    }

    private struct ThreadCommand
    {
        public Type CommandType;
        public Task Task;

        public ThreadCommand(Type commandType, Task task)
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
