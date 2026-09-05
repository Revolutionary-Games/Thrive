using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal enum ResourceBackgroundPhase
{
    Prepare,
    Load,
}

internal enum ResourceLoadState
{
    Queued,
    Preparing,
    Prepared,
    Loading,
    PendingMain,
    Completed,
}

/// <summary>
///   Binds one resource phase to its background task and observes its result at most once.
/// </summary>
internal sealed class ResourceBackgroundTask(IResource resource, Task task, ResourceBackgroundPhase phase)
{
    private int completionObserved;

    internal IResource Resource { get; } = resource ?? throw new ArgumentNullException(nameof(resource));
    internal Task Task { get; } = task ?? throw new ArgumentNullException(nameof(task));
    internal ResourceBackgroundPhase Phase { get; } = phase;
    internal bool IsCompleted => Task.IsCompleted;
    internal bool CompletionObserved => Volatile.Read(ref completionObserved) != 0;

    /// <summary>
    ///   Observes a completed task at most once, propagating faults and cancellation to the caller. Does nothing if the
    ///   task is still running or its completion was already observed.
    /// </summary>
    internal void ObserveCompletion()
    {
        if (!Task.IsCompleted || Interlocked.Exchange(ref completionObserved, 1) != 0)
            return;

        Task.GetAwaiter().GetResult();
    }

    /// <summary>
    ///   Claims completion observation when the manager exits, including for a task that is still running.
    /// </summary>
    internal void ObserveFailureOnCompletion(Action<Exception> reportFailure)
    {
        _ = Task.ContinueWith(completedTask =>
        {
            if (Interlocked.Exchange(ref completionObserved, 1) != 0)
                return;

            try
            {
                completedTask.GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                reportFailure(e);
            }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}

/// <summary>
///   Tracks a single active request for each resource instance. Repeated requests are ignored until it completes.
/// </summary>
internal sealed class ResourceLoadLifecycle
{
    private readonly Dictionary<IResource, ResourceLoadState> activeResources = new(ReferenceEqualityComparer.Instance);

    internal ResourceLoadState GetState(IResource resource)
    {
        return activeResources.GetValueOrDefault(resource, ResourceLoadState.Completed);
    }

    internal bool IsActive(IResource resource)
    {
        return activeResources.ContainsKey(resource);
    }

    internal bool TryQueue(IResource resource)
    {
        if (activeResources.ContainsKey(resource))
            return false;

        resource.CancelRequested = false;
        activeResources.Add(resource, ResourceLoadState.Queued);
        return true;
    }

    internal bool TryCancel(IResource resource)
    {
        if (!activeResources.ContainsKey(resource))
            return false;

        resource.CancelRequested = true;
        return true;
    }

    internal void BeginPreparing(IResource resource)
    {
        Transition(resource, ResourceLoadState.Queued, ResourceLoadState.Preparing);
    }

    internal void FinishPreparing(IResource resource)
    {
        Transition(resource, ResourceLoadState.Preparing, ResourceLoadState.Prepared);
    }

    internal void MarkPrepared(IResource resource)
    {
        Transition(resource, ResourceLoadState.Queued, ResourceLoadState.Prepared);
    }

    internal void BeginLoading(IResource resource)
    {
        Transition(resource, ResourceLoadState.Prepared, ResourceLoadState.Loading);
    }

    internal void FinishBackgroundLoad(IResource resource)
    {
        Transition(resource, ResourceLoadState.Loading, ResourceLoadState.PendingMain);
    }

    /// <summary>
    ///   Releases the current request without clearing cancellation or scheduling another load.
    /// </summary>
    internal void Complete(IResource resource)
    {
        activeResources.Remove(resource);
    }

    private void Transition(IResource resource, ResourceLoadState expected, ResourceLoadState next)
    {
        if (!activeResources.TryGetValue(resource, out var current))
            throw new InvalidOperationException($"Resource {resource.Identifier} is not registered for loading");

        if (current != expected)
        {
            throw new InvalidOperationException(
                $"Resource {resource.Identifier} cannot transition from {current} to {next}; expected {expected}");
        }

        activeResources[resource] = next;
    }
}
