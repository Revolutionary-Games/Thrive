using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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

    private readonly StageResourcePreloader stageResourcePreloader;

    private IResource? preparingBackgroundResource;
    private IResource? processingBackgroundResource;

    // TODO: implement relative performance detection

    private float savedForLaterProcessingTime;

    private ResourceManager()
    {
        instance = this;
        ProcessMode = ProcessModeEnum.Always;
        stageResourcePreloader = new StageResourcePreloader(QueueLoad, () => queuedResources.Count > 0);
    }

    public static ResourceManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    public Texture2D LoadingIcon { get; private set; } = null!;

    public int StageLoadCurrentProgress => stageResourcePreloader.CurrentProgress;
    public int StageLoadTotalItems => stageResourcePreloader.TotalItems;

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

        ObserveBackgroundCompletions();
        HandleLoadQueue(originalBudget);

        savedForLaterProcessingTime = Math.Clamp((float)(originalBudget - timeTracker.Elapsed).TotalSeconds,
            -Constants.RESOURCE_TIME_BUDGET_PER_FRAME * 2,
            Constants.RESOURCE_TIME_BUDGET_PER_FRAME * 0.5f);
    }

    public void QueueLoad(IResource resource)
    {
        queuedResources.Add(resource);
    }

    public void CancelLoad(IResource resource)
    {
        // TODO: the collection is a blocking one so we cannot immediately remove the resource, so for now we use
        // this soft cancellation flag to skip most of hte work
        resource.CancelRequested = true;
    }

    public void OnStageLoadStart(MainGameState gameState)
    {
        stageResourcePreloader.Begin(gameState);
    }

    public bool ProgressStageLoad()
    {
        return stageResourcePreloader.Progress();
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

    private void ObserveBackgroundCompletions()
    {
        if (processingBackgroundResource?.Loaded == true)
        {
            processingBackgroundResource.OnComplete?.Invoke(processingBackgroundResource);
            processingBackgroundResource = null;
        }

        if (preparingBackgroundResource?.LoadingPrepared == true)
            preparingBackgroundResource = null;
    }

    private void StartBackgroundPreparation(IResource resource)
    {
        TaskExecutor.Instance.AddTask(new Task(() => { PrepareLoad(resource); }), false);

        preparingBackgroundResource = resource;
    }

    private void StartBackgroundLoad(IResource resource)
    {
        if (resource.UsesPostProcessing && resource.RequiresSyncPostProcess)
        {
            throw new NotImplementedException(
                "Missing handling for requiring sync post process but supporting async load");
        }

        TaskExecutor.Instance.AddTask(new Task(() => { PerformFullLoad(resource); }), false);

        processingBackgroundResource = resource;
    }

    private void HandleLoadQueue(TimeSpan originalBudget)
    {
        bool hasThingsInQueue = processingResources.Count > 0;

        // Queueing and preparation also count as progress when deciding whether another load fits this frame
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

                    // If already loaded, don't need to do anything (or if cancelled)
                    if (resource.Loaded || resource.CancelRequested)
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
                            StartBackgroundPreparation(resource);
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
                            StartBackgroundLoad(resource);
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
                // Early skip cancelled items
                if (queueResource.CancelRequested)
                    continue;

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
}
