﻿using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base class for all stage classes that are part of the strategy stages part of the game (society stage etc.)
/// </summary>
[GodotAbstract]
public partial class StrategyStageBase : StageBase, IStrategyStage
{
    [JsonProperty]
    protected SocietyResourceStorage resourceStorage = new();

#pragma warning disable CA2213
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    [Export]
    protected StrategicCamera strategicCamera = null!;
#pragma warning restore CA2213

    private readonly Dictionary<object, float> activeResearchContributions = new();

    protected StrategyStageBase()
    {
    }

    /// <summary>
    ///   Where the stage's strategic view camera is looking at
    /// </summary>
    public Vector3 CameraWorldPoint
    {
        get => strategicCamera.WorldLocation;
        set => strategicCamera.WorldLocation = value;
    }

    [JsonProperty]
    public TechnologyProgress? CurrentlyResearchedTechnology { get; private set; }

    [JsonIgnore]
    protected virtual IStrategyStageHUD BaseHUD => throw new GodotAbstractPropertyNotOverriddenException();

    public override void _ExitTree()
    {
        base._ExitTree();

        // Strategy stages don't switch to an editor scene, so we should always cancel auto-evo
        GameWorld.ResetAutoEvoRun();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (StageLoadingState != LoadState.NotLoading)
            return;

        if (!IsGameOver())
        {
            BaseHUD.UpdateScienceSpeed(activeResearchContributions.SumValues());

            if (CurrentlyResearchedTechnology?.Completed == true)
            {
                GD.Print("Current technology research completed");
                CurrentGame!.TechWeb.UnlockTechnology(CurrentlyResearchedTechnology.Technology);
                CurrentlyResearchedTechnology = null;

                // TODO: if research screen is open, it should have its state update here in regards to the unlocked
                // technology
            }

            BaseHUD.UpdateResearchProgress(CurrentlyResearchedTechnology);
        }

        BaseHUD.UpdateResourceDisplay(resourceStorage);
    }

    public Vector3 GetPlayerCursorPointedWorldPosition()
    {
        return strategicCamera.CursorWorldPos;
    }

    public override void OnFinishLoading(Save save)
    {
        throw new InvalidOperationException(
            "Saving for this late stage is not implemented, remove this exception once added");
    }

    [RunOnKeyDown("g_pause")]
    public void PauseKeyPressed()
    {
        // Check nothing else has keyboard focus and pause the game
        if (BaseHUD.GetFocusOwner() == null)
        {
            BaseHUD.PauseButtonPressed(!BaseHUD.Paused);
        }
    }

    public void ToggleResearchScreen()
    {
        BaseHUD.OpenResearchScreen();
    }

    [RunOnKeyDown("e_reset_camera")]
    public void ResetCamera()
    {
        strategicCamera.ZoomLevel = 1;

        // If we ever have camera rotation, that should reset as well
    }

    public bool AnimateCameraZoomTowards(float target, double delta, float speed = 1)
    {
        strategicCamera.AllowPlayerInput = false;

        if (strategicCamera.ZoomLevel > target)
        {
            strategicCamera.ZoomLevel -= (float)(speed * delta);

            if (strategicCamera.ZoomLevel < target)
            {
                strategicCamera.ZoomLevel = target;
                return true;
            }
        }
        else if (strategicCamera.ZoomLevel < target)
        {
            strategicCamera.ZoomLevel += (float)(speed * delta);

            if (strategicCamera.ZoomLevel > target)
            {
                strategicCamera.ZoomLevel = target;
                return true;
            }
        }
        else
        {
            // Already at target
            return true;
        }

        return false;
    }

    public void AddActiveResearchContribution(object researchSource, float researchPoints)
    {
        // TODO: come up with a way to get unique identifiers for the research sources
        // Using WeakReference doesn't work as it causes not equal objects to be created
        activeResearchContributions[researchSource] = researchPoints;
    }

    public void RemoveActiveResearchContribution(object researchSource)
    {
        activeResearchContributions.Remove(researchSource);
    }

    protected override void StartGUIStageTransition(bool longDuration, bool returnFromEditor)
    {
        BaseHUD.OnEnterStageLoadingScreen(longDuration, returnFromEditor);
    }

    protected override void OnTriggerHUDFinalLoadFadeIn()
    {
        BaseHUD.OnStageLoaded(this);
    }

    protected override void SetupStage()
    {
        base.SetupStage();

        if (CurrentGame == null)
            throw new InvalidOperationException("Base setup stage did not setup current game");

        CurrentGame.TechWeb.OnTechnologyUnlockedHandler += ShowTechnologyUnlockMessage;
    }

    protected override Node3D CreateGraphicsPreloadNode()
    {
        // The world point should always be under the camera so that it can be seen
        var result = new Node3D();
        rootOfDynamicallySpawned.AddChild(result);
        result.Position = strategicCamera.WorldLocation;

        return result;
    }

    protected override void OnLightLevelUpdate()
    {
        // TODO: day/night light effects
    }

    protected void ShowTechnologyUnlockMessage(Technology technology)
    {
        BaseHUD.HUDMessages.ShowMessage(
            Localization.Translate("TECHNOLOGY_UNLOCKED_NOTICE").FormatSafe(technology.Name),
            DisplayDuration.Long);
    }

    protected void StartResearching(string technologyName)
    {
        // Skip if trying to start the same research again, just to not lose progress as the GUI data passing to
        // ensure a technology is not started multiple times is complicated
        if (CurrentlyResearchedTechnology?.Technology.InternalName == technologyName)
        {
            GD.Print("Skipping trying to start the same research again");
            return;
        }

        GD.Print("Starting researching: ", technologyName);
        CurrentlyResearchedTechnology =
            new TechnologyProgress(SimulationParameters.Instance.GetTechnology(technologyName));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (CurrentGame != null)
                CurrentGame.TechWeb.OnTechnologyUnlockedHandler -= ShowTechnologyUnlockMessage;
        }

        base.Dispose(disposing);
    }
}
