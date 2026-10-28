using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

/// <summary>
///   Keeps the resources needed by the current stage and tracks their preloading through the resource manager.
/// </summary>
internal sealed class StageResourcePreloader
{
    private readonly Action<IResource> queueLoad;
    private readonly Func<bool> hasQueuedResources;

    private readonly HashSet<string> temporaryResourceIds = new();
    private readonly HashSet<string> alreadyLoadedResources = new();
    private readonly List<IResource> stageResources = new();

    // TODO: do we need to keep visual resources / scenes loaded while the scene is active or is Godot's default memory
    // handling good enough?

    private MainGameState gameStateThatIsLoading;
    private bool gameStateLoaded = true;
    private int totalStageResourcesLoaded;
    private int totalStageResourcesToLoad = -1;

    internal StageResourcePreloader(Action<IResource> queueLoad, Func<bool> hasQueuedResources)
    {
        this.queueLoad = queueLoad;
        this.hasQueuedResources = hasQueuedResources;
    }

    internal int CurrentProgress => totalStageResourcesLoaded;
    internal int TotalItems => totalStageResourcesToLoad;

    internal void Begin(MainGameState gameState)
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

    internal bool Progress()
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
        if (hasQueuedResources())
        {
            totalStageResourcesLoaded = stageResources.Count(r => r.Loaded);
            return false;
        }

        // Make sure all resources are loaded
        // As some can still be processing by this point
        if (stageResources.Exists(r => !r.Loaded))
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
                    resource.UnLoad();
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
            queueLoad(resource);
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
