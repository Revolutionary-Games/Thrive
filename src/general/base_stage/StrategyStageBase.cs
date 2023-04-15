using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base class for all stage classes that are part of the strategy stages part of the game (society stage etc.)
/// </summary>
public abstract class StrategyStageBase : StageBase, IStrategyStage
{
    [Export]
    public NodePath? StrategicCameraPath;

#pragma warning disable CA2213
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private StrategicCamera strategicCamera = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Where the stage's strategic view camera is looking at
    /// </summary>
    public Vector3 CameraWorldPoint
    {
        get => strategicCamera.WorldLocation;
        set => strategicCamera.WorldLocation = value;
    }

    [JsonIgnore]
    protected abstract IStrategyStageHUD BaseHUD { get; }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        strategicCamera = GetNode<StrategicCamera>(StrategicCameraPath);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // Strategy stages don't switch to an editor scene, so we should always cancel auto-evo
        GameWorld.ResetAutoEvoRun();
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

    [RunOnKeyDown("e_reset_camera")]
    public void ResetCamera()
    {
        strategicCamera.ZoomLevel = 1;

        // If we ever have camera rotation, that should reset as well
    }

    protected override void StartGUIStageTransition(bool longDuration, bool returnFromEditor)
    {
        BaseHUD.OnEnterStageTransition(longDuration, returnFromEditor);
    }

    protected override void SetupStage()
    {
        base.SetupStage();

        if (CurrentGame == null)
            throw new InvalidOperationException("Base setup stage did not setup current game");

        CurrentGame.TechWeb.OnTechnologyUnlockedHandler += ShowTechnologyUnlockMessage;
    }

    protected void ShowTechnologyUnlockMessage(Technology technology)
    {
        BaseHUD.HUDMessages.ShowMessage(
            TranslationServer.Translate("TECHNOLOGY_UNLOCKED_NOTICE").FormatSafe(technology.Name),
            DisplayDuration.Long);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StrategicCameraPath?.Dispose();

            if (CurrentGame != null)
                CurrentGame.TechWeb.OnTechnologyUnlockedHandler -= ShowTechnologyUnlockMessage;
        }

        base.Dispose(disposing);
    }
}
