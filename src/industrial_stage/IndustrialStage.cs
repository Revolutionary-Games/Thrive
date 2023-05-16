using System;
using Godot;
using Newtonsoft.Json;
using Array = Godot.Collections.Array;

/// <summary>
///   The main class handling the industrial stage functions
/// </summary>
public class IndustrialStage : StrategyStageBase, ISocietyStructureDataAccess
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
    private Spatial? toSpaceAnimatedUnit;
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
    private float stageMoveStepElapsed;

    [JsonProperty]
    private float toSpaceUnitAcceleration;

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

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!IsGameOver())
        {
            citySystem.Process(delta, this);

            resourceStorage.Capacity = citySystem.CachedTotalStorage;

            HandleStageTransition(delta);
        }

        HUD.UpdatePopulationDisplay(citySystem.CachedTotalPopulation);
    }

    public override void StartMusic()
    {
        Jukebox.Instance.PlayCategory("IndustrialStage");
    }

    public PlacedCity AddCity(Transform location, bool playerCity)
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
        city.Connect(nameof(PlacedCity.OnSelected), this, nameof(OpenCityInfo), binds);

        return city;
    }

    public override void StartNewGame()
    {
        CurrentGame = GameProperties.StartIndustrialStageGame(new WorldGenerationSettings());

        // Spawn an initial city
        AddCity(Transform.Identity, true);

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

        // TODO: switch to using proper in-play unit class here
        // For now the prototype just displays the visuals
        var scene = spacecraft.WorldRepresentation;

        toSpaceAnimatedUnit = new Spatial();
        toSpaceAnimatedUnit.AddChild(scene.Instance<Spatial>());

        toSpaceAnimatedUnit.Scale = new Vector3(Constants.SPACE_TO_INDUSTRIAL_SCALE_FACTOR,
            Constants.SPACE_TO_INDUSTRIAL_SCALE_FACTOR,
            Constants.SPACE_TO_INDUSTRIAL_SCALE_FACTOR);

        rootOfDynamicallySpawned.AddChild(toSpaceAnimatedUnit);

        toSpaceAnimatedUnit.GlobalTranslation = spaceCraftData.Value.City.GlobalTranslation;

        HUD.CloseAllOpenWindows();

        // Start the first phase of the stage move with a camera animation
        movingToSpaceStagePhase = StageMovePhase.ZoomingCamera;

        cameraPanStart = strategicCamera.WorldLocation;
        cameraPanEnd = spaceCraftData.Value.City.GlobalTranslation;

        strategicCamera.AllowPlayerInput = false;
        strategicCamera.MinZoomLevel *= Constants.INDUSTRIAL_TO_SPACE_CAMERA_MIN_HEIGHT_MULTIPLIER;
        strategicCamera._Ready();
    }

    private void HandleStageTransition(float delta)
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

                strategicCamera.WorldLocation = cameraPanStart.Value.LinearInterpolate(cameraPanEnd.Value,
                    Math.Min(stageMoveStepElapsed / Constants.INDUSTRIAL_TO_SPACE_CAMERA_PAN_DURATION, 1));

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

                if ((toSpaceAnimatedUnit.GlobalTranslation - strategicCamera.WorldLocation).y >
                    Constants.INDUSTRIAL_TO_SPACE_CAMERA_ROCKET_FOLLOW_START)
                {
                    movingToSpaceStagePhase = StageMovePhase.FollowingRocket;
                }

                break;
            }

            case StageMovePhase.FollowingRocket:
            {
                HandleRocketMovingUp(delta);

                strategicCamera.WorldLocation = strategicCamera.WorldLocation.LinearInterpolate(
                    toSpaceAnimatedUnit!.GlobalTranslation, Constants.INDUSTRIAL_TO_SPACE_CAMERA_ROCKET_FOLLOW_SPEED);

                if (toSpaceAnimatedUnit.GlobalTranslation.y > Constants.INDUSTRIAL_TO_SPACE_END_ROCKET_HEIGHT)
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
                strategicCamera.WorldLocation = strategicCamera.WorldLocation.LinearInterpolate(
                    toSpaceAnimatedUnit!.GlobalTranslation, Constants.INDUSTRIAL_TO_SPACE_CAMERA_ROCKET_FOLLOW_SPEED);

                // TODO: maybe already fade out the stars in somehow? (or maybe even in the previous step)
                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HandleRocketMovingUp(float delta)
    {
        if (toSpaceAnimatedUnit == null)
            throw new InvalidOperationException("Unit going to space not set");

        // TODO: unit specific acceleration values / movement here
        toSpaceUnitAcceleration += delta * Constants.INDUSTRIAL_TO_SPACE_ROCKET_ACCELERATION;

        toSpaceAnimatedUnit.GlobalTranslation += new Vector3(0, toSpaceUnitAcceleration, 0);
    }

    private void SwitchToSpaceScene()
    {
        GD.Print("Switching to space scene");

        var spaceStage =
            SceneManager.Instance.LoadScene(MainGameState.SpaceStage).Instance<SpaceStage>();
        spaceStage.CurrentGame = CurrentGame;

        SceneManager.Instance.SwitchToScene(spaceStage);

        // Copy our resources to the new stage, this is after the scene switch to make sure the storage capacity is
        // initialized already
        spaceStage.TakeInitialResourcesFrom(SocietyResources);

        spaceStage.AddPlanet(Transform.Identity, true);

        // TODO: preserve the actual cities placed on the starting planet

        // Create initial fleet from the ship going to space
        var spaceCraftData = citySystem.FirstLaunchableSpacecraft;
        if (spaceCraftData == null)
        {
            GD.PrintErr("Spacecraft to put in initial fleet not found, using fallback unit");

            spaceCraftData = (null!, SimulationParameters.Instance.GetUnitType("simpleSpaceRocket"));
        }

        var fleet = spaceStage.AddFleet(new Transform(Basis.Identity, new Vector3(6, 0, 0)),
            spaceCraftData.Value.Spacecraft, true);

        // Focus the camera initially on the ship to make the stage transition smoother
        spaceStage.ZoomOutFromFleet(fleet);

        // Add an order to have the fleet be moving
        fleet.PerformOrder(new FleetMovementOrder(fleet, new Vector3(20, 0, 0)));
    }
}
