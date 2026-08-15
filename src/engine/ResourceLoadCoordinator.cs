/// <summary>
///   Connects resource-loading phases to frame-budget admission without allocating on the normal execution path.
/// </summary>
internal static class ResourceLoadCoordinator
{
    /// <summary>
    ///   Tries to invoke the main-thread callback for a resource whose background load has completed.
    /// </summary>
    public static bool TryRunBackgroundCallback<TDispatcher, TTimeSource>(IResource resource,
        ref ResourceLoadFrameBudget frameBudget, ref TDispatcher dispatcher, ref TTimeSource timeSource)
        where TDispatcher : struct, IResourceLoadDispatcher
        where TTimeSource : struct, IResourceLoadFrameTimeSource
    {
        var completionUnit = new BackgroundResourceCallbackCompletionUnit<TDispatcher>(resource, dispatcher);
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
        var completionUnit = new SynchronousResourceLoadCompletionUnit<TDispatcher>(resource, dispatcher);
        return frameBudget.TryRunCompletionUnit(ref completionUnit, ref timeSource);
    }

    private readonly struct BackgroundResourceCallbackCompletionUnit<TDispatcher> : IResourceLoadCompletionUnit
        where TDispatcher : struct, IResourceLoadDispatcher
    {
        private readonly IResource resource;
        private readonly TDispatcher dispatcher;

        public BackgroundResourceCallbackCompletionUnit(IResource resource, TDispatcher dispatcher)
        {
            this.resource = resource;
            this.dispatcher = dispatcher;
        }

        public double EstimatedDurationSeconds => 0;

        public void Execute()
        {
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

        public double EstimatedDurationSeconds => resource.EstimatedTimeRequired;

        public void Execute()
        {
            dispatcher.ExecuteFullLoad(resource);
            dispatcher.InvokeCompletionCallback(resource);
        }
    }
}
