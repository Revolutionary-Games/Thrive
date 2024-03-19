using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Nito.Collections;

/// <summary>
///   Manages loading game resources
/// </summary>
/// <remarks>
///   <para>
///     The plan for this is to eventually to allow the game to show a loading screen while loading all microbe stage
///     assets for example. For now this is just a loading work queue.
///   </para>
///   <para>
///     Godot 4.0 should make background loading much more doable so this should be reworked at that time.
///   </para>
/// </remarks>
[GodotAutoload]
public partial class ResourceManager : Node
{
    private static ResourceManager? instance;

    private readonly BlockingCollection<IResource> queuedResources = new();
    private readonly Deque<IResource> processingResources = new();
    private readonly Stopwatch timeTracker = new();

    private IResource? preparingBackgroundResource;
    private IResource? processingBackgroundResource;

    private float savedForLaterProcessingTime;

    private ResourceManager()
    {
        instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public static ResourceManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    public Texture2D LoadingIcon { get; private set; } = null!;

    public override void _Ready()
    {
        base._Ready();

        LoadingIcon = GD.Load<Texture2D>("res://assets/textures/gui/bevel/IconGenerating.png");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // TODO: should this instead use a budget approach based exclusively on delta (and multiply it by
        // some fraction)?
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

                    if (!resource.LoadingPrepared)
                    {
                        // Need to prepare this
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

        savedForLaterProcessingTime = Mathf.Clamp((float)(originalBudget - timeTracker.Elapsed).TotalSeconds,
            -Constants.RESOURCE_TIME_BUDGET_PER_FRAME * 2,
            Constants.RESOURCE_TIME_BUDGET_PER_FRAME * 0.5f);
    }

    public void QueueLoad(IResource resource)
    {
        queuedResources.Add(resource);
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
        if (Constants.TRACK_ACTUAL_RESOURCE_LOAD_TIMES)
            stopwatch = Stopwatch.StartNew();

        resource.Load();

        if (resource.UsesPostProcessing)
            resource.PerformPostProcessing();

        if (!resource.Loaded)
            throw new InvalidOperationException("Loading a resource didn't end up setting loaded flag");

        if (Constants.TRACK_ACTUAL_RESOURCE_LOAD_TIMES)
        {
            var elapsed = stopwatch.Elapsed;

            var difference = elapsed.TotalSeconds - resource.EstimatedTimeRequired;

            if (Math.Abs(difference) > Constants.REPORT_LOAD_TIMES_OF_BY)
            {
                GD.Print($"Load time estimate off by {difference}s for {resource.Identifier}");
            }
        }

        // ReSharper enable HeuristicUnreachableCode
#pragma warning restore CS0162
    }
}
