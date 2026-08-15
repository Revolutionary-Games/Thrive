using System;
using System.Threading.Tasks;

internal enum ResourceBackgroundPhase
{
    Prepare,
    Load,
}

/// <summary>
///   Binds one resource phase to its background task and observes its result at most once.
/// </summary>
internal sealed class ResourceBackgroundTask
{
    private bool completionObserved;

    public ResourceBackgroundTask(IResource resource, Task task, ResourceBackgroundPhase phase)
    {
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        Task = task ?? throw new ArgumentNullException(nameof(task));
        Phase = phase;
    }

    public IResource Resource { get; }
    public Task Task { get; }
    public ResourceBackgroundPhase Phase { get; }
    public bool IsCompleted => Task.IsCompleted;
    public bool CompletionObserved => completionObserved;

    /// <summary>
    ///   Observes a completed task's result, propagating faults and cancellation to the caller.
    /// </summary>
    /// <returns><c>true</c> only for the first observation of a completed task.</returns>
    public bool TryObserveCompletion()
    {
        if (completionObserved || !Task.IsCompleted)
            return false;

        completionObserved = true;
        Task.GetAwaiter().GetResult();
        return true;
    }
}
