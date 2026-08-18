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

    public IResource Resource { get; } = resource ?? throw new ArgumentNullException(nameof(resource));
    public Task Task { get; } = task ?? throw new ArgumentNullException(nameof(task));
    public ResourceBackgroundPhase Phase { get; } = phase;
    public bool IsCompleted => Task.IsCompleted;
    public bool CompletionObserved => Volatile.Read(ref completionObserved) != 0;

    /// <summary>
    ///   Observes a completed task's result, propagating faults and cancellation to the caller.
    /// </summary>
    /// <returns><c>true</c> only for the first observation of a completed task.</returns>
    public bool TryObserveCompletion()
    {
        if (!Task.IsCompleted || Interlocked.Exchange(ref completionObserved, 1) != 0)
            return false;

        Task.GetAwaiter().GetResult();
        return true;
    }

    /// <summary>
    ///   Claims completion observation when the manager exits, including for a task that is still running.
    /// </summary>
    public void ObserveFailureOnCompletion(Action<Exception> reportFailure)
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
///   Tracks the single owner and deferred reload request for each resource instance.
/// </summary>
internal sealed class ResourceLoadLifecycle
{
    private readonly Dictionary<IResource, ResourceLoadState> activeResources =
        new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<IResource> deferredReloads = new(ReferenceEqualityComparer.Instance);

    public ResourceLoadState GetState(IResource resource)
    {
        return activeResources.GetValueOrDefault(resource, ResourceLoadState.Completed);
    }

    public bool IsActive(IResource resource)
    {
        return activeResources.ContainsKey(resource);
    }

    public bool TryQueue(IResource resource)
    {
        if (activeResources.ContainsKey(resource))
        {
            if (resource.CancelRequested)
                deferredReloads.Add(resource);

            return false;
        }

        resource.CancelRequested = false;
        activeResources.Add(resource, ResourceLoadState.Queued);
        return true;
    }

    public bool TryCancel(IResource resource)
    {
        if (!activeResources.ContainsKey(resource))
            return false;

        resource.CancelRequested = true;
        return true;
    }

    public void BeginPreparing(IResource resource)
    {
        Transition(resource, ResourceLoadState.Queued, ResourceLoadState.Preparing);
    }

    public void FinishPreparing(IResource resource)
    {
        Transition(resource, ResourceLoadState.Preparing, ResourceLoadState.Prepared);
    }

    public void MarkPrepared(IResource resource)
    {
        Transition(resource, ResourceLoadState.Queued, ResourceLoadState.Prepared);
    }

    public void BeginLoading(IResource resource)
    {
        Transition(resource, ResourceLoadState.Prepared, ResourceLoadState.Loading);
    }

    public void FinishBackgroundLoad(IResource resource)
    {
        Transition(resource, ResourceLoadState.Loading, ResourceLoadState.PendingMain);
    }

    public bool CancellationCanSettle(IResource resource)
    {
        return GetState(resource) is ResourceLoadState.Queued or ResourceLoadState.Prepared
            or ResourceLoadState.PendingMain;
    }

    public bool CancellationNeedsUnload(IResource resource)
    {
        return GetState(resource) == ResourceLoadState.PendingMain;
    }

    /// <summary>
    ///   Releases the current owner and queues a requested reload only after that owner has settled.
    /// </summary>
    /// <returns><c>true</c> when the caller must add the resource back to its queue.</returns>
    public bool Complete(IResource resource)
    {
        if (!activeResources.Remove(resource))
            return false;

        if (!deferredReloads.Remove(resource))
            return false;

        resource.CancelRequested = false;
        activeResources.Add(resource, ResourceLoadState.Queued);
        return true;
    }

    private void Transition(IResource resource, ResourceLoadState expected, ResourceLoadState next)
    {
        if (!activeResources.TryGetValue(resource, out var current) || current != expected)
        {
            throw new InvalidOperationException(
                $"Resource {resource.Identifier} cannot transition from {current} to {next}; expected {expected}");
        }

        activeResources[resource] = next;
    }
}
