using System;

/// <summary>
///   Connects resource-loading phases to frame-budget admission without allocating on the normal execution path.
/// </summary>
internal static class ResourceLoadCoordinator
{
    /// <summary>
    ///   Rejects phase declarations that cannot describe a coherent resource load.
    /// </summary>
    public static void ValidateResourceConfiguration(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (!resource.RequiresSyncLoad && !resource.UsesPostProcessing && resource.RequiresSyncPostProcess)
        {
            throw new InvalidOperationException(
                $"Async resource {resource.Identifier} requires synchronous post-processing but has no " +
                "post-processing phase");
        }
    }

    /// <summary>
    ///   Tries to run the indivisible main-thread phases for a resource whose background load has completed.
    /// </summary>
    public static bool TryRunPendingMainThreadPhases<TDispatcher, TTimeSource>(IResource resource,
        ref ResourceLoadFrameBudget frameBudget, ref TDispatcher dispatcher, ref TTimeSource timeSource)
        where TDispatcher : struct, IResourceLoadDispatcher
        where TTimeSource : struct, IResourceLoadFrameTimeSource
    {
        ValidateResourceConfiguration(resource);

        var completionUnit = new PendingMainThreadCompletionUnit<TDispatcher>(resource, dispatcher);
        return frameBudget.TryRunCompletionUnit(ref completionUnit, ref timeSource);
    }

    /// <summary>
    ///   Tries to synchronously load a resource and then invoke its main-thread completion callback.
    /// </summary>
    public static bool TryRunSynchronousLoad<TDispatcher, TTimeSource>(IResource resource,
        ref ResourceLoadFrameBudget frameBudget, ref TDispatcher dispatcher, ref TTimeSource timeSource)
        where TDispatcher : struct, IResourceLoadDispatcher
        where TTimeSource : struct, IResourceLoadFrameTimeSource
    {
        ValidateResourceConfiguration(resource);

        var completionUnit = new SynchronousResourceLoadCompletionUnit<TDispatcher>(resource, dispatcher);
        return frameBudget.TryRunCompletionUnit(ref completionUnit, ref timeSource);
    }

    private readonly struct PendingMainThreadCompletionUnit<TDispatcher> : IResourceLoadCompletionUnit
        where TDispatcher : struct, IResourceLoadDispatcher
    {
        private readonly IResource resource;
        private readonly TDispatcher dispatcher;

        public PendingMainThreadCompletionUnit(IResource resource, TDispatcher dispatcher)
        {
            this.resource = resource;
            this.dispatcher = dispatcher;
        }

        public double EstimatedDurationSeconds => resource.EstimatedMainThreadTimeRequired;

        public void Execute()
        {
            if (resource.UsesPostProcessing && resource.RequiresSyncPostProcess)
                dispatcher.ExecuteMainThreadPostProcessing(resource);

            dispatcher.InvokeCompletionCallback(resource);
        }
    }

    private readonly struct SynchronousResourceLoadCompletionUnit<TDispatcher> : IResourceLoadCompletionUnit
        where TDispatcher : struct, IResourceLoadDispatcher
    {
        private readonly IResource resource;
        private readonly TDispatcher dispatcher;

        public SynchronousResourceLoadCompletionUnit(IResource resource, TDispatcher dispatcher)
        {
            this.resource = resource;
            this.dispatcher = dispatcher;
        }

        public double EstimatedDurationSeconds => resource.EstimatedMainThreadTimeRequired;

        public void Execute()
        {
            dispatcher.ExecuteFullLoad(resource);
            dispatcher.InvokeCompletionCallback(resource);
        }
    }
}
