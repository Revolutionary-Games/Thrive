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
    private readonly Stopwatch timeTracker = new();

    private readonly HashSet<string> temporaryResourceIds = new();
    private readonly List<IResource> stageResources = new();

    // TODO: do we need to keep visual resources / scenes loaded while the scene is active or is Godot's default memory
    // handling good enough?

    private MainGameState gameStateThatIsLoading;
    private bool gameStateLoaded = true;
    private int totalStageResourcesLoaded;
    private int totalStageResourcesToLoad = -1;

    private IResource? preparingBackgroundResource;
    private IResource? processingBackgroundResource;

    // TODO: implement relative performance detection

    private float savedForLaterProcessingTime;

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
        base._ExitTree();

        if (instance == this)
            instance = null;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // TODO: should this instead use a budget approach based exclusively on delta (and multiply it by
        // some fraction)?
        // TODO: maybe also considering a "deficit" from previous frame would be good for very long load tasks?
        var originalBudget =
            TimeSpan.FromSeconds(Math.Max(Constants.RESOURCE_TIME_BUDGET_PER_FRAME - delta,
                    Constants.RESOURCE_TIME_BUDGET_PER_FRAME * 0.05f) +
                savedForLaterProcessingTime);

        timeTracker.Restart();

        if (processingBackgroundResource?.Loaded == true)
        {
            processingBackgroundResource.OnComplete?.Invoke(processingBackgroundResource);
            processingBackgroundResource = null;
        }

        if (preparingBackgroundResource?.LoadingPrepared == true)
            preparingBackgroundResource = null;

        HandleLoadQueue(originalBudget);

        savedForLaterProcessingTime = Math.Clamp((float)(originalBudget - timeTracker.Elapsed).TotalSeconds,
            -Constants.RESOURCE_TIME_BUDGET_PER_FRAME * 2,
            Constants.RESOURCE_TIME_BUDGET_PER_FRAME * 0.5f);
    }

    public void QueueLoad(IResource resource)
    {
        queuedResources.Add(resource);
    }

    public void OnStageLoadStart(MainGameState gameState)
    {
        if (!gameStateLoaded)
            GD.PrintErr("Abandoning previous game state load and starting new one");

        gameStateLoaded = false;
        totalStageResourcesLoaded = 0;
        totalStageResourcesToLoad = -1;

        // Some stages are equivalent in terms of required resources
        switch (gameState)
        {
            case MainGameState.MicrobeEditor:
            case MainGameState.MulticellularEditor:
                gameStateThatIsLoading = MainGameState.MicrobeStage;
                break;
            case MainGameState.MacroscopicEditor:
                gameStateThatIsLoading = MainGameState.MacroscopicStage;
                break;
            case MainGameState.AscensionCeremony:
                gameStateThatIsLoading = MainGameState.SpaceStage;
                break;
            default:
                gameStateThatIsLoading = gameState;
                break;
        }
    }

    public bool ProgressStageLoad()
    {
        if (gameStateLoaded)
            return true;

        if (totalStageResourcesToLoad == -1)
        {
            if (StartStageResourceLoad())
            {
                // This returns only true on error
                gameStateLoaded = true;
                return true;
            }

            return false;
        }

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

        if (resource.UsesPostProcessing)
            resource.PerformPostProcessing();

        if (!resource.Loaded)
            throw new InvalidOperationException("Loading a resource didn't end up setting loaded flag");

        if (Constants.TRACK_ACTUAL_RESOURCE_LOAD_TIMES || Constants.REPORT_ALL_LOAD_TIMES)
        {
            var elapsed = stopwatch.Elapsed;

            var difference = elapsed.TotalSeconds - resource.EstimatedTimeRequired;

            if (Math.Abs(difference) > Constants.REPORT_LOAD_TIMES_OF_BY)
            {
                GD.Print($"Load time estimate off by {difference}s for {resource.Identifier}");
            }

            if (Constants.REPORT_ALL_LOAD_TIMES)
            {
                GD.Print($"Load time: {elapsed.TotalSeconds}s for {resource.Identifier}");
            }
        }

        // ReSharper enable HeuristicUnreachableCode
#pragma warning restore CS0162
    }

    private void HandleLoadQueue(TimeSpan originalBudget)
    {
        bool hasThingsInQueue = processingResources.Count > 0;

        // Ensures at least something gets loaded
        bool progressedLoading = false;

        while (true)
        {
            float timeRemaining = (float)(originalBudget - timeTracker.Elapsed).TotalSeconds;

            if (timeRemaining <= 0)
                break;

            if (hasThingsInQueue)
            {
                bool didSomething = false;

                int count = processingResources.Count;

                for (int i = 0; i < count; ++i)
                {
                    var resource = processingResources[i];

                    // If already loaded, don't need to do anything
                    if (resource.Loaded)
                    {
                        processingResources.RemoveAt(i);
                        --count;
                        --i;
                        continue;
                    }

                    if (!resource.LoadingPrepared)
                    {
                        // Need to prepare for loading this
                        if (preparingBackgroundResource == null)
                        {
                            TaskExecutor.Instance.AddTask(new Task(() => { PrepareLoad(resource); }), false);

                            preparingBackgroundResource = resource;
                            progressedLoading = true;
                        }

                        continue;
                    }

                    if (!resource.RequiresSyncLoad)
                    {
                        // TODO: implement proper background loading. As all resources currently are sync loaded
                        // no effort is put into the background load yet

                        if (processingBackgroundResource == null)
                        {
                            if (resource.UsesPostProcessing && resource.RequiresSyncPostProcess)
                            {
                                throw new NotImplementedException(
                                    "Missing handling for requiring sync post process but supporting async load");
                            }

                            TaskExecutor.Instance.AddTask(new Task(() => { PerformFullLoad(resource); }), false);

                            processingBackgroundResource = resource;
                            processingResources.RemoveAt(i);
                            --count;
                            didSomething = true;
                            progressedLoading = true;
                            --i;
                        }

                        continue;
                    }

                    // Run the first load that we can probably finish this process cycle
                    if (!progressedLoading || timeRemaining - resource.EstimatedTimeRequired > 0)
                    {
                        // TODO: allow splitting the post processing to the next frame
                        PerformFullLoad(resource);
                        resource.OnComplete?.Invoke(resource);

                        didSomething = true;
                        progressedLoading = true;
                        processingResources.RemoveAt(i);

                        // We break here to recompute the time remaining
                        break;
                    }
                }

                hasThingsInQueue = didSomething;

                // Let's try to avoid putting too many things in the processing container at once
                if (processingResources.Count > 20)
                    break;
            }
            else if (queuedResources.TryTake(out var queueResource, 0))
            {
                processingResources.AddToBack(queueResource);
                hasThingsInQueue = true;
                progressedLoading = true;
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
                    resource.UnLoad();
                    return true;
                }

                return false;
            });

            temporaryResourceIds.Clear();

            if (unloaded > 0)
                GD.Print($"Unloaded {unloaded} stage resources");
        }

        // The next frame after unloading, start loading new stuff
        stageResources.AddRange(resources.RequiredVisualResources);
        stageResources.AddRange(resources.RequiredScenes);

        // Queue all loads at once.
        // This is hopefully fine as this simplifies the throttling logic, and there shouldn't be that many resources
        // at any stage, so the time taken to add to the list should be minimal.
        foreach (var resource in stageResources)
        {
            // All resources are queued just in case something ends up flipping a flag to false, and we'd otherwise miss
            // a resource that needed to be loaded and got stuck indefinitely

            // if (!resource.Loaded)
            QueueLoad(resource);
        }

        totalStageResourcesToLoad = stageResources.Count;
        GD.Print($"Starting preload of {totalStageResourcesToLoad} stage resources");
        return false;
    }
}
