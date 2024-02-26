using System;
using Godot;
using Newtonsoft.Json;
using Array = Godot.Collections.Array;

/// <summary>
///   The main class handling the industrial stage functions
/// </summary>
public partial class IndustrialStage : StrategyStageBase, ISocietyStructureDataAccess
{
    [Export]
    public NodePath? NameLabelSystemPath;

    [Export]
    public NodePath SpaceMoveConfirmationPopupPath = null!;

    private const string PauseReasonForNextStage = "confirmMoveToSpace";

#pragma warning disable CA2213
    private StrategicEntityNameLabelSystem nameLabelSystem = null!;

    private CustomConfirmationDialog spaceMoveConfirmationPopup = null!;

    private PackedScene cityScene = null!;

    // TODO: switch to using proper unit class here
    private Node3D? toSpaceAnimatedUnit;
#pragma warning restore CA2213

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CitySystem citySystem = null!;

    [JsonProperty]
    private StageMovePhase movingToSpaceStagePhase;

    [JsonProperty]
    private Vector3? cameraPanStart;

    [JsonProperty]
    private Vector3? cameraPanEnd;

    [JsonProperty]
    private double stageMoveStepElapsed;

    [JsonProperty]
    private float toSpaceUnitAcceleration;

    [JsonProperty]
    private UnitType? launchedSpacecraftType;

    private enum StageMovePhase
    {
        NotMoving,
        ZoomingCamera,
        RocketLaunching,
        FollowingRocket,
        FadingOut,
    }

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public IndustrialHUD HUD { get; private set; } = null!;

    [JsonIgnore]
    public IResourceContainer SocietyResources => resourceStorage;

    public override MainGameState GameState => MainGameState.IndustrialStage;

    [JsonIgnore]
    protected override IStrategyStageHUD BaseHUD => HUD;

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        cityScene = SpawnHelpers.LoadCityScene();

        nameLabelSystem.Init(strategicCamera, rootOfDynamicallySpawned);
        nameLabelSystem.Visible = true;

        HUD.Init(this);

        SetupStage();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        HUD = GetNode<IndustrialHUD>("IndustrialHUD");
        spaceMoveConfirmationPopup = GetNode<CustomConfirmationDialog>(SpaceMoveConfirmationPopupPath);

        // Systems
        nameLabelSystem = GetNode<StrategicEntityNameLabelSystem>(NameLabelSystemPath);
        citySystem = new CitySystem(rootOfDynamicallySpawned);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!IsGameOver())
        {
            citySystem.Process((float)delta, this);

            resourceStorage.Capacity = citySystem.CachedTotalStorage;

            HandleStageTransition(delta);
        }

        HUD.UpdatePopulationDisplay(citySystem.CachedTotalPopulation);
    }

    public override void StartMusic()
    {
        Jukebox.Instance.PlayCategory("IndustrialStage");
    }

    public PlacedCity AddCity(Transform3D location, bool playerCity)
    {
        if (CurrentGame == null)
            throw new InvalidOperationException("Current game not set");

        var techWeb = CurrentGame.TechWeb;

        if (!playerCity)
        {
            // TODO: AI civilizations' tech webs
            GD.Print("TODO: implement AI civilization tech unlocking");
            techWeb = new TechWeb();
        }

        var city = SpawnHelpers.SpawnCity(location, rootOfDynamicallySpawned, cityScene, playerCity, techWeb);

        var binds = new Array();
        binds.Add(city);
        city.Connect(nameof(PlacedCity.OnSelectedEventHandler), new Callable(this, nameof(OpenCityInfo)), binds);

        return city;
    }

    public override void StartNewGame()
    {
        CurrentGame = GameProperties.StartIndustrialStageGame(new WorldGenerationSettings());

        // Spawn an initial city
        AddCity(Transform3D.Identity, true);

        base.StartNewGame();
    }

    public void TakeInitialResourcesFrom(IResourceContainer resources)
    {
        // Force capacity up temporarily to be able to get probably all of the resources
        resourceStorage.Capacity = 10000;
        SocietyResources.TransferFrom(resources);
    }

    protected override void SetupStage()
    {
        base.SetupStage();

        // Get systems started
        citySystem.CalculateDerivedStats();
        resourceStorage.Capacity = citySystem.CachedTotalStorage;
    }

    protected override void OnGameStarted()
    {
        // Intentionally not translated prototype message
        HUD.HUDMessages.ShowMessage(
            "To advance: research rocketry and then select your city to build it to be able to go to space",
            DisplayDuration.ExtraLong);
    }

    protected override bool IsGameOver()
    {
        // TODO: lose condition
        return false;
    }

    protected override void OnGameOver()
    {
        // TODO: once possible to lose, show in the GUI
    }

    protected override void AutoSave()
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    protected override void PerformQuickSave()
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (NameLabelSystemPath != null)
            {
                NameLabelSystemPath.Dispose();
                SpaceMoveConfirmationPopupPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OpenCityInfo(PlacedCity city)
    {
        HUD.OpenCityScreen(city);
    }

    private void CheckCanMoveToNextStage()
    {
        var spaceCraftData = citySystem.FirstLaunchableSpacecraft;

        if (spaceCraftData == null || movingToSpaceStagePhase != StageMovePhase.NotMoving)
            return;

        if (spaceMoveConfirmationPopup.Visible)
        {
            GD.PrintErr("Space confirm popup shouldn't be open yet");
            return;
        }

        // There's a spacecraft to launch, confirm before launch the move to the new stage
        spaceMoveConfirmationPopup.PopupCenteredShrink();
        PauseManager.Instance.AddPause(PauseReasonForNextStage);
    }

    private void CancelMoveToNextStage()
    {
        // For now delete the spacecraft to not immediately show the popup again
        var spaceCraftData = citySystem.FirstLaunchableSpacecraft;

        if (spaceCraftData == null)
        {
            GD.PrintErr("Spacecraft to cancel not found");
            return;
        }

        GD.Print("Destroying spacecraft as canceling going to space");

        if (!spaceCraftData.Value.City.OnUnitUnGarrisoned(spaceCraftData.Value.Spacecraft))
            GD.PrintErr("Failed to un-garrison spacecraft");

        citySystem.ClearLaunchableSpacecraft();

        // TODO: deleting the unit when that is needed once moving to a proper unit class

        PauseManager.Instance.Resume(PauseReasonForNextStage);
    }

    private void ConfirmMoveToNextStage()
    {
        var spaceCraftData = citySystem.FirstLaunchableSpacecraft;

        if (spaceCraftData == null)
        {
            GD.PrintErr("Spacecraft to launch not found");
            return;
        }

        citySystem.ClearLaunchableSpacecraft();
        PauseManager.Instance.Resume(PauseReasonForNextStage);

        if (movingToSpaceStagePhase != StageMovePhase.NotMoving)
        {
            GD.PrintErr("Already moving to space");
            return;
        }

        var spacecraft = spaceCraftData.Value.Spacecraft;
        if (!spaceCraftData.Value.City.OnUnitUnGarrisoned(spacecraft))
            GD.PrintErr("Failed to un-garrison spacecraft for launch");

        launchedSpacecraftType = spacecraft;

        // TODO: switch to using proper in-play unit class here
        // For now the prototype just displays the visuals
        var scene = spacecraft.WorldRepresentation;

        toSpaceAnimatedUnit = new Node3D();
        toSpaceAnimatedUnit.AddChild(scene.Instantiate<Node3D>());

        toSpaceAnimatedUnit.Scale = new Vector3(Constants.SPACE_TO_INDUSTRIAL_SCALE_FACTOR,
            Constants.SPACE_TO_INDUSTRIAL_SCALE_FACTOR,
            Constants.SPACE_TO_INDUSTRIAL_SCALE_FACTOR);

        rootOfDynamicallySpawned.AddChild(toSpaceAnimatedUnit);

        toSpaceAnimatedUnit.GlobalPosition = spaceCraftData.Value.City.GlobalPosition;

        HUD.CloseAllOpenWindows();

        // Start the first phase of the stage move with a camera animation
        movingToSpaceStagePhase = StageMovePhase.ZoomingCamera;

        cameraPanStart = strategicCamera.WorldLocation;
        cameraPanEnd = spaceCraftData.Value.City.GlobalPosition;

        strategicCamera.AllowPlayerInput = false;
        strategicCamera.MinZoomLevel *= Constants.INDUSTRIAL_TO_SPACE_CAMERA_MIN_HEIGHT_MULTIPLIER;
        strategicCamera._Ready();
    }

    private void HandleStageTransition(double delta)
    {
        switch (movingToSpaceStagePhase)
        {
            case StageMovePhase.NotMoving:
                CheckCanMoveToNextStage();
                break;
            case StageMovePhase.ZoomingCamera:
            {
                if (cameraPanStart == null || cameraPanEnd == null)
                    throw new InvalidOperationException("Camera animation variables not set");

                stageMoveStepElapsed += delta;

                strategicCamera.WorldLocation = cameraPanStart.Value.Lerp(cameraPanEnd.Value,
                    (float)Math.Min(stageMoveStepElapsed / Constants.INDUSTRIAL_TO_SPACE_CAMERA_PAN_DURATION, 1));

                if (AnimateCameraZoomTowards(strategicCamera.MinZoomLevel, delta,
                        Constants.INDUSTRIAL_TO_SPACE_CAMERA_ZOOM_SPEED) &&
                    stageMoveStepElapsed >= Constants.INDUSTRIAL_TO_SPACE_CAMERA_PAN_DURATION)
                {
                    // Finished with this step
                    movingToSpaceStagePhase = StageMovePhase.RocketLaunching;
                    stageMoveStepElapsed = 0;
                }

                break;
            }

            case StageMovePhase.RocketLaunching:
            {
                if (toSpaceAnimatedUnit == null)
                    throw new InvalidOperationException("Unit going to space not set");

                HandleRocketMovingUp(delta);

                if ((toSpaceAnimatedUnit.GlobalPosition - strategicCamera.WorldLocation).Y >
                    Constants.INDUSTRIAL_TO_SPACE_CAMERA_ROCKET_FOLLOW_START)
                {
                    movingToSpaceStagePhase = StageMovePhase.FollowingRocket;
                }

                break;
            }

            case StageMovePhase.FollowingRocket:
            {
                HandleRocketMovingUp(delta);

                strategicCamera.WorldLocation = strategicCamera.WorldLocation.Lerp(
                    toSpaceAnimatedUnit!.GlobalPosition, Constants.INDUSTRIAL_TO_SPACE_CAMERA_ROCKET_FOLLOW_SPEED);

                if (toSpaceAnimatedUnit.GlobalPosition.Y > Constants.INDUSTRIAL_TO_SPACE_END_ROCKET_HEIGHT)
                {
                    movingToSpaceStagePhase = StageMovePhase.FadingOut;
                    TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut,
                        Constants.INDUSTRIAL_TO_SPACE_FADE_DURATION, SwitchToSpaceScene);
                }

                break;
            }

            case StageMovePhase.FadingOut:
            {
                HandleRocketMovingUp(delta);
                strategicCamera.WorldLocation = strategicCamera.WorldLocation.Lerp(
                    toSpaceAnimatedUnit!.GlobalPosition, Constants.INDUSTRIAL_TO_SPACE_CAMERA_ROCKET_FOLLOW_SPEED);

                // TODO: maybe already fade out the stars in somehow? (or maybe even in the previous step)
                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HandleRocketMovingUp(double delta)
    {
        if (toSpaceAnimatedUnit == null)
            throw new InvalidOperationException("Unit going to space not set");

        // TODO: unit specific acceleration values / movement here
        toSpaceUnitAcceleration += (float)(delta * Constants.INDUSTRIAL_TO_SPACE_ROCKET_ACCELERATION);

        toSpaceAnimatedUnit.GlobalPosition += new Vector3(0, toSpaceUnitAcceleration, 0);
    }

    private void SwitchToSpaceScene()
    {
        GD.Print("Switching to space scene");

        var spaceStage = SceneManager.Instance.LoadScene(MainGameState.SpaceStage).Instantiate<SpaceStage>();
        spaceStage.CurrentGame = CurrentGame;

        SceneManager.Instance.SwitchToScene(spaceStage);

        // Create initial fleet from the ship going to space
        if (launchedSpacecraftType == null)
        {
            GD.PrintErr("Spacecraft to put in initial fleet not found, using fallback unit");

            launchedSpacecraftType = SimulationParameters.Instance.GetUnitType("simpleSpaceRocket");
        }

        // TODO: preserve the actual cities placed on the starting planet

        spaceStage.SetupForExistingGameFromAnotherStage(true, launchedSpacecraftType, SocietyResources);
        launchedSpacecraftType = null;
    }
}
