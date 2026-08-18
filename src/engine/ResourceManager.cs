using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Nito.Collections;

/// <summary>
///   Manages loading of the game resources
/// </summary>
/// <remarks>
///   <para>
///     Godot 4.0 should make background loading much more usable, so this should be experimented with to add some
///     threading.
///   </para>
/// </remarks>
/// <remarks>
///   <para>
///     TODO: this should have a baseline performance detection of the current computer and adjust all load times
///     accordingly
///   </para>
/// </remarks>
[GodotAutoload]
public partial class ResourceManager : Node
{
    private static ResourceManager? instance;

    private readonly BlockingCollection<IResource> queuedResources = new();
    private readonly Deque<IResource> processingResources = new();
    private readonly ResourceLoadLifecycle loadLifecycle = new();
    private readonly Stopwatch timeTracker = new();

    private readonly HashSet<string> temporaryResourceIds = new();
    private readonly HashSet<string> alreadyLoadedResources = new();
    private readonly List<IResource> stageResources = new();

    // TODO: do we need to keep visual resources / scenes loaded while the scene is active or is Godot's default memory
    // handling good enough?

    private MainGameState gameStateThatIsLoading;
    private bool gameStateLoaded = true;
    private int totalStageResourcesLoaded;
    private int totalStageResourcesToLoad = -1;

    private ResourceBackgroundTask? preparingBackgroundTask;
    private ResourceBackgroundTask? processingBackgroundTask;
    private bool shuttingDown;

    // TODO: implement relative performance detection

    private double savedForLaterProcessingTime;

    private ResourceManager()
    {
        instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public static ResourceManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    public Texture2D LoadingIcon { get; private set; } = null!;

    public int StageLoadCurrentProgress => totalStageResourcesLoaded;
    public int StageLoadTotalItems => totalStageResourcesToLoad;

    public override void _Ready()
    {
        base._Ready();

        LoadingIcon = GD.Load<Texture2D>("res://assets/textures/gui/bevel/IconGenerating.png");
    }

    public override void _ExitTree()
    {
        shuttingDown = true;
        queuedResources.CompleteAdding();

        ObserveTaskFailureAfterExit(preparingBackgroundTask);
        ObserveTaskFailureAfterExit(processingBackgroundTask);

        base._ExitTree();

        if (instance == this)
            instance = null;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (shuttingDown)
            return;

        // Carry unused processing time, or a bounded deficit, between frames to spread resource loading work.
        var frameBudget = new ResourceLoadFrameBudget(delta, savedForLaterProcessingTime,
            Constants.RESOURCE_TIME_BUDGET_PER_FRAME);

        timeTracker.Restart();

        ObservePreparingBackgroundTask();
        ObserveProcessingBackgroundTask(ref frameBudget);

        // The processing task doubles as the single PendingMain slot. It must be released before another load starts.
        if (processingBackgroundTask == null)
            HandleLoadQueue(ref frameBudget);

        savedForLaterProcessingTime = frameBudget.CalculateSecondsToCarry(timeTracker.Elapsed.TotalSeconds);
    }

    public void QueueLoad(IResource resource)
    {
        if (shuttingDown)
            return;

        // Reject contradictory phases before the lifecycle takes ownership and could otherwise become stuck.
        ValidateResourceConfiguration(resource);

        if (!loadLifecycle.TryQueue(resource))
            return;

        queuedResources.Add(resource);
    }

    public void CancelLoad(IResource resource)
    {
        if (!shuttingDown)
            loadLifecycle.TryCancel(resource);
    }

    public void OnStageLoadStart(MainGameState gameState)
    {
        if (!gameStateLoaded)
        {
            GD.PrintErr("Abandoning previous game state load and starting new one");

            foreach (var resource in stageResources)
                loadLifecycle.TryCancel(resource);
        }

        gameStateLoaded = false;
        totalStageResourcesLoaded = 0;
        totalStageResourcesToLoad = -1;

        // Some stages are equivalent in terms of required resources
        gameStateThatIsLoading = gameState switch
        {
            MainGameState.MicrobeEditor or MainGameState.MulticellularEditor => MainGameState.MicrobeStage,
            MainGameState.MacroscopicEditor => MainGameState.MacroscopicStage,
            MainGameState.AscensionCeremony => MainGameState.SpaceStage,
            _ => gameState,
        };
    }

    public bool ProgressStageLoad()
    {
        if (gameStateLoaded)
            return true;

        if (totalStageResourcesToLoad == -1)
        {
            if (!StartStageResourceLoad())
                return false;

            // This returns only true on error
            gameStateLoaded = true;
            return true;
        }

        // Do not inspect state written by a background operation before its Task completion has been observed.
        if (preparingBackgroundTask != null || processingBackgroundTask != null)
            return false;

        // Wait until the pending loads are empty
        if (queuedResources.Count > 0)
        {
            totalStageResourcesLoaded = stageResources.Count(r => r.Loaded);
            return false;
        }

        // Make sure all resources are loaded
        // As some can still be processing by this point
        if (stageResources.Any(r => !r.Loaded))
        {
            // TODO: does this need unstuck logic if some resource is not getting loaded?
            // For example, due to something unloading a resource after we loaded it and it is no longer in the load
            // queue due to that so the state will never change to loaded
            return false;
        }

        // Done loading
        totalStageResourcesLoaded = totalStageResourcesToLoad;
        gameStateLoaded = true;

#if DEBUG
        var resources = SimulationParameters.Instance.GetStageResources(gameStateThatIsLoading);

        foreach (var resource in resources.RequiredScenes)
        {
            if (!resource.Loaded || resource.LoadedScene == null)
            {
                GD.PrintErr(
                    $"Somehow preloaded scene is not loaded for stage ({gameStateThatIsLoading}): {resource.Path}");

                if (Debugger.IsAttached)
                    Debugger.Break();
            }
        }
#endif

        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            queuedResources.Dispose();
        }

        base.Dispose(disposing);
    }

    internal static void ValidateResourceConfiguration(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (!resource.RequiresSyncLoad && !resource.UsesPostProcessing && resource.RequiresSyncPostProcess)
        {
            throw new InvalidOperationException(
                $"Async resource {resource.Identifier} requires synchronous post-processing but has no " +
                "post-processing phase");
        }
    }

    private static void PerformSynchronousLoadAndCallback(IResource resource)
    {
        PerformFullLoad(resource);
        resource.OnComplete?.Invoke(resource);
    }

    private static void PerformPendingMainThreadPhases(IResource resource, Stopwatch? splitLoadStopwatch)
    {
        if (resource.UsesPostProcessing && resource.RequiresSyncPostProcess)
        {
            splitLoadStopwatch?.Start();
            PerformMainThreadPostProcessing(resource);

            if (splitLoadStopwatch != null)
            {
                splitLoadStopwatch.Stop();
                ReportResourceLoadTime(resource, splitLoadStopwatch.Elapsed,
                    GetEstimatedMainThreadTimeRequired(resource));
            }
        }

        resource.OnComplete?.Invoke(resource);
    }

    private static void PrepareLoad(IResource resource)
    {
        resource.PrepareLoading();
        resource.LoadingPrepared = true;
    }

    private static void PerformFullLoad(IResource resource)
    {
        if (!resource.LoadingPrepared)
            throw new InvalidOperationException("Resource is not prepared for load yet");

        Stopwatch? stopwatch;

        // Controlled by a constant variable that we want to toggle
        // ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS0162
        if (Constants.TRACK_ACTUAL_RESOURCE_LOAD_TIMES || Constants.REPORT_ALL_LOAD_TIMES)
            stopwatch = Stopwatch.StartNew();

        resource.Load();

        if (resource.CancelRequested)
            return;

        if (resource.UsesPostProcessing)
            resource.PerformPostProcessing();

        if (!resource.Loaded)
            throw new InvalidOperationException("Loading a resource didn't end up setting loaded flag");

        if (Constants.TRACK_ACTUAL_RESOURCE_LOAD_TIMES || Constants.REPORT_ALL_LOAD_TIMES)
            ReportResourceLoadTime(resource, stopwatch.Elapsed, resource.EstimatedTimeRequired);
    }

    private static void ReportResourceLoadTime(IResource resource, TimeSpan elapsed, double estimatedTimeRequired)
    {
        var difference = elapsed.TotalSeconds - estimatedTimeRequired;

        if (Math.Abs(difference) > Constants.REPORT_LOAD_TIMES_OF_BY)
            GD.Print($"Load time estimate off by {difference}s for {resource.Identifier}");

        if (Constants.REPORT_ALL_LOAD_TIMES)
            GD.Print($"Load time: {elapsed.TotalSeconds}s for {resource.Identifier}");
    }

    private static Stopwatch? CreateSplitLoadStopwatch(IResource resource)
    {
        if (resource.RequiresSyncPostProcess &&
            (Constants.TRACK_ACTUAL_RESOURCE_LOAD_TIMES || Constants.REPORT_ALL_LOAD_TIMES))
            return new Stopwatch();

        return null;
    }

    // ReSharper enable HeuristicUnreachableCode
#pragma warning restore CS0162

    private static float GetEstimatedMainThreadTimeRequired(IResource resource)
    {
        return resource.RequiresSyncLoad || (resource.UsesPostProcessing && resource.RequiresSyncPostProcess) ?
            resource.EstimatedTimeRequired :
            0;
    }

    private static void PerformBackgroundLoad(IResource resource)
    {
        if (!resource.RequiresSyncPostProcess)
        {
            PerformFullLoad(resource);
            return;
        }

        if (!resource.LoadingPrepared)
            throw new InvalidOperationException("Resource is not prepared for load yet");

        // Completion and loaded-state validation are deferred to the indivisible main-thread post-processing unit.
        resource.Load();
    }

    private static void PerformMainThreadPostProcessing(IResource resource)
    {
        resource.PerformPostProcessing();

        if (!resource.Loaded)
            throw new InvalidOperationException("Loading a resource didn't end up setting loaded flag");
    }

    private static void ReportResourceOperationFailure(IResource resource, ResourceBackgroundPhase phase,
        Exception exception)
    {
        ReportResourceOperationFailure(resource, $"background {phase}", exception);
    }

    private static void ReportResourceOperationFailure(IResource resource, string operation, Exception exception)
    {
        GD.PrintErr($"Resource {operation} failed for {resource.Identifier}: ", exception);
    }

    private void ObservePreparingBackgroundTask()
    {
        var backgroundTask = preparingBackgroundTask;

        if (backgroundTask == null || !backgroundTask.IsCompleted)
            return;

        try
        {
            backgroundTask.TryObserveCompletion();
            loadLifecycle.FinishPreparing(backgroundTask.Resource);

            if (backgroundTask.Resource.CancelRequested)
            {
                RemoveProcessingResource(backgroundTask.Resource);
                CompleteResource(backgroundTask.Resource);
            }
        }
        catch (Exception e)
        {
            RemoveProcessingResource(backgroundTask.Resource);
            CompleteResource(backgroundTask.Resource);
            ReportResourceOperationFailure(backgroundTask.Resource, backgroundTask.Phase, e);
        }
        finally
        {
            preparingBackgroundTask = null;
        }
    }

#pragma warning disable SA1202 // Kept beside the private background-task observer that immediately precedes it.
    internal void ObserveProcessingBackgroundTask(ref ResourceLoadFrameBudget frameBudget,
        bool suppressFailureReporting = false)
    {
        var backgroundTask = processingBackgroundTask;

        if (backgroundTask == null)
            return;

        if (!backgroundTask.CompletionObserved)
        {
            if (!backgroundTask.IsCompleted)
                return;

            try
            {
                backgroundTask.TryObserveCompletion();
                loadLifecycle.FinishBackgroundLoad(backgroundTask.Resource);
            }
            catch (Exception e)
            {
                processingBackgroundTask = null;
                CompleteResource(backgroundTask.Resource);
                ReportResourceOperationFailure(backgroundTask.Resource, backgroundTask.Phase, e);
                return;
            }
        }

        if (backgroundTask.Resource.CancelRequested)
        {
            processingBackgroundTask = null;
            RemoveStageResource(backgroundTask.Resource);
            BestEffortUnload(backgroundTask.Resource);
            CompleteResource(backgroundTask.Resource);
            return;
        }

        var requiresMainThreadPostProcessing = backgroundTask.Resource.UsesPostProcessing &&
            backgroundTask.Resource.RequiresSyncPostProcess;

        if (!requiresMainThreadPostProcessing && backgroundTask.Resource.OnComplete == null)
        {
            processingBackgroundTask = null;
            CompleteResource(backgroundTask.Resource);
            return;
        }

        try
        {
            if (!frameBudget.TryAdmit(GetEstimatedMainThreadTimeRequired(backgroundTask.Resource),
                    timeTracker.Elapsed.TotalSeconds))
                return;

            PerformPendingMainThreadPhases(backgroundTask.Resource,
                backgroundTask.Task.AsyncState as Stopwatch);
            processingBackgroundTask = null;
            CompleteResource(backgroundTask.Resource);
        }
        catch (Exception e)
        {
            // The callback was admitted and started, so it must not be repeated on the next frame.
            processingBackgroundTask = null;
            CompleteResource(backgroundTask.Resource);
            if (!suppressFailureReporting)
            {
                ReportResourceOperationFailure(backgroundTask.Resource,
                    "main-thread post-processing or completion callback", e);
            }
        }
    }
#pragma warning restore SA1202

    private void CompleteResource(IResource resource)
    {
        if (loadLifecycle.Complete(resource) && !shuttingDown)
            queuedResources.Add(resource);
    }

    private void BestEffortUnload(IResource resource)
    {
        try
        {
            resource.UnLoad();
        }
        catch (Exception e)
        {
            ReportResourceOperationFailure(resource, "unload after cancellation", e);
        }
    }

    private void ObserveTaskFailureAfterExit(ResourceBackgroundTask? backgroundTask)
    {
        backgroundTask?.ObserveFailureOnCompletion(e =>
            ReportResourceOperationFailure(backgroundTask.Resource, $"background {backgroundTask.Phase} on exit", e));
    }

    private void RemoveProcessingResource(IResource resource)
    {
        for (int i = 0; i < processingResources.Count; ++i)
        {
            if (!ReferenceEquals(processingResources[i], resource))
                continue;

            processingResources.RemoveAt(i);
            return;
        }
    }

    private void RemoveStageResource(IResource resource)
    {
        for (int i = 0; i < stageResources.Count; ++i)
        {
            if (!ReferenceEquals(stageResources[i], resource))
                continue;

            stageResources.RemoveAt(i);
            return;
        }
    }

    private void HandleLoadQueue(ref ResourceLoadFrameBudget frameBudget)
    {
        bool hasThingsInQueue = processingResources.Count > 0;

        while (true)
        {
            double timeRemaining = frameBudget.GetRemainingSeconds(timeTracker.Elapsed.TotalSeconds);

            if (timeRemaining <= 0)
                break;

            if (hasThingsInQueue)
            {
                bool didSomething = false;

                int count = processingResources.Count;

                for (int i = 0; i < count; ++i)
                {
                    var resource = processingResources[i];

                    if (ReferenceEquals(preparingBackgroundTask?.Resource, resource))
                        continue;

                    if (resource.Loaded)
                    {
                        processingResources.RemoveAt(i);
                        CompleteResource(resource);
                        --count;
                        --i;
                        continue;
                    }

                    if (resource.CancelRequested && loadLifecycle.CancellationCanSettle(resource))
                    {
                        if (loadLifecycle.CancellationNeedsUnload(resource))
                            BestEffortUnload(resource);

                        processingResources.RemoveAt(i);
                        CompleteResource(resource);
                        --count;
                        --i;
                        continue;
                    }

                    if (!resource.LoadingPrepared)
                    {
                        // Need to prepare for loading this
                        if (preparingBackgroundTask == null)
                        {
                            loadLifecycle.BeginPreparing(resource);
                            var task = new Task(() => { PrepareLoad(resource); });
                            preparingBackgroundTask = new ResourceBackgroundTask(resource, task,
                                ResourceBackgroundPhase.Prepare);
                            TaskExecutor.Instance.AddTask(task, false);
                        }

                        continue;
                    }

                    if (loadLifecycle.GetState(resource) == ResourceLoadState.Queued)
                        loadLifecycle.MarkPrepared(resource);

                    if (!resource.RequiresSyncLoad)
                    {
                        // TODO: implement proper background loading. As all resources currently are sync loaded
                        // no effort is put into the background load yet

                        if (processingBackgroundTask == null)
                        {
                            loadLifecycle.BeginLoading(resource);

                            // Keep the timer with the task so deferred main-thread post-processing can resume it.
                            var loadStopwatch = CreateSplitLoadStopwatch(resource);
                            var task = new Task(state =>
                            {
                                var stopwatch = state as Stopwatch;
                                stopwatch?.Start();
                                PerformBackgroundLoad(resource);
                                stopwatch?.Stop();
                            }, loadStopwatch);
                            processingBackgroundTask = new ResourceBackgroundTask(resource, task,
                                ResourceBackgroundPhase.Load);
                            TaskExecutor.Instance.AddTask(task, false);
                            processingResources.RemoveAt(i);

                            // Do not start another load until this task and its PendingMain phase release the slot.
                            return;
                        }

                        continue;
                    }

                    // A unit that fits may run normally. The frame's first completion unit may exceed the remaining
                    // positive budget to guarantee progress, but later units must fit.
                    try
                    {
                        if (!frameBudget.TryAdmit(GetEstimatedMainThreadTimeRequired(resource),
                                timeTracker.Elapsed.TotalSeconds))
                            continue;

                        PerformSynchronousLoadAndCallback(resource);
                        didSomething = true;
                        processingResources.RemoveAt(i);
                        CompleteResource(resource);
                    }
                    catch (Exception e)
                    {
                        // A load or callback that started must be settled exactly once, even when it throws.
                        didSomething = true;
                        processingResources.RemoveAt(i);
                        CompleteResource(resource);
                        ReportResourceOperationFailure(resource, "synchronous load or completion callback", e);
                    }

                    // We break here to recompute the time remaining
                    break;
                }

                hasThingsInQueue = didSomething;

                // Let's try to avoid putting too many things in the processing container at once
                if (processingResources.Count > 20)
                    break;
            }
            else if (queuedResources.TryTake(out var queueResource, 0))
            {
                // Early skip cancelled items
                if (queueResource.CancelRequested)
                {
                    CompleteResource(queueResource);
                    continue;
                }

                processingResources.AddToBack(queueResource);
                hasThingsInQueue = true;
            }
            else
            {
                // Nothing to do
                break;
            }
        }
    }

    private bool StartStageResourceLoad()
    {
        if (stageResources.Any(loadLifecycle.IsActive))
            return false;

        StageResourcesList resources;
        try
        {
            resources = SimulationParameters.Instance.GetStageResources(gameStateThatIsLoading);
        }
        catch (Exception e)
        {
#if DEBUG
            Debugger.Break();
#endif

            GD.PrintErr("Error while trying to get stage resources: ", e);
            GD.PrintErr("WILL NOT PRELOAD RESOURCES; THIS WILL CAUSE LAG SPIKES!");
            return true;
        }

        alreadyLoadedResources.Clear();

        if (stageResources.Count > 0)
        {
            // Unload resources that won't be needed in the new game state

            // First need to detect the identifiers of stuff that are kept
            foreach (var item in resources.RequiredVisualResources)
            {
                temporaryResourceIds.Add(item.Identifier);
            }

            foreach (var item in resources.RequiredScenes)
            {
                temporaryResourceIds.Add(item.Identifier);
            }

            // Then perform the unloading. This allocates one small lambda so that should not be bad at all
            var unloaded = stageResources.RemoveAll(resource =>
            {
                if (!temporaryResourceIds.Contains(resource.Identifier))
                {
                    try
                    {
                        resource.UnLoad();
                    }
                    catch (Exception e)
                    {
                        // The resource is removed from the active stage even if releasing its contents fails.
                        ReportResourceOperationFailure(resource, "unload", e);
                    }

                    return true;
                }

                alreadyLoadedResources.Add(resource.Identifier);
                return false;
            });

            temporaryResourceIds.Clear();

            if (unloaded > 0)
                GD.Print($"Unloaded {unloaded} stage resources");
        }

        int reused = 0;

        // The next frame after unloading, start loading new stuff
        // Only add resources that are not loaded already to save on resource loads when swapping between similar
        // stages
        foreach (var requiredVisualResource in resources.RequiredVisualResources)
        {
            if (!alreadyLoadedResources.Contains(requiredVisualResource.Identifier))
            {
                stageResources.Add(requiredVisualResource);
            }
            else
            {
                ++reused;
            }
        }

        foreach (var requiredScene in resources.RequiredScenes)
        {
            if (!alreadyLoadedResources.Contains(requiredScene.Identifier))
            {
                stageResources.Add(requiredScene);
            }
            else
            {
                ++reused;
            }
        }

        // Queue all loads at once.
        // This is hopefully fine as this simplifies the throttling logic, and there shouldn't be that many resources
        // at any stage, so the time taken to add to the list should be minimal.
        foreach (var resource in stageResources)
        {
            // All resources are queued just in case something ends up flipping a flag to false, and we'd otherwise
            // miss a resource that needed to be loaded and got stuck indefinitely

            // if (!resource.Loaded)
            QueueLoad(resource);
        }

        totalStageResourcesToLoad = stageResources.Count;
        GD.Print($"Starting preload of {totalStageResourcesToLoad} stage resources");

        if (reused > 0)
        {
            GD.Print($"Reused {reused} already loaded resources");
        }

        return false;
    }
}
