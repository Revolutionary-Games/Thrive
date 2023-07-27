using Godot;
using Newtonsoft.Json;

/// <summary>
///   The main class handling the society stage functions
/// </summary>
public class SocietyStage : StrategyStageBase, ISocietyStructureDataAccess,
    IStructureSelectionReceiver<StructureDefinition>
{
    [Export]
    public NodePath? SelectBuildingPopupPath;

    [Export]
    public NodePath IndustrialStageConfirmPopupPath = null!;

#pragma warning disable CA2213
    private SelectBuildingPopup selectBuildingPopup = null!;

    private PackedScene structureScene = null!;

    private CustomConfirmationDialog industrialStageConfirmPopup = null!;

    private Spatial? buildingToPlaceGhost;
#pragma warning restore CA2213

    private WorldResource foodResource = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private SocietyStructureSystem structureSystem = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CitizenMovingSystem citizenMovingSystem = null!;

    private long population = 1;

    private StructureDefinition? buildingTypeToPlace;

    private bool stageAdvanceConfirmed;

    [JsonProperty]
    private bool movingToSocietyStage;

    private bool stageLeaveTransitionStarted;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public SocietyHUD HUD { get; private set; } = null!;

    [JsonIgnore]
    public IResourceContainer SocietyResources => resourceStorage;

    [JsonIgnore]
    protected override IStrategyStageHUD BaseHUD => HUD;

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        structureScene = SpawnHelpers.LoadStructureScene();

        HUD.Init(this);

        SetupStage();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        HUD = GetNode<SocietyHUD>("SocietyHUD");

        selectBuildingPopup = GetNode<SelectBuildingPopup>(SelectBuildingPopupPath);
        industrialStageConfirmPopup = GetNode<CustomConfirmationDialog>(IndustrialStageConfirmPopupPath);

        // Systems
        structureSystem = new SocietyStructureSystem(rootOfDynamicallySpawned);
        citizenMovingSystem = new CitizenMovingSystem(rootOfDynamicallySpawned);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!IsGameOver())
        {
            structureSystem.Process(delta, this);

            // This doesn't really need to update all that often but for now this is fine performance-wise and is
            // easier to program
            resourceStorage.Capacity = structureSystem.CachedTotalStorage;

            // TODO: make population consume food

            // Update the place to place the selected building
            if (buildingToPlaceGhost != null)
            {
                // Don't update the placing when we have a popup open
                if (industrialStageConfirmPopup.Visible != true)
                {
                    // TODO: collision check with other buildings
                    buildingToPlaceGhost.GlobalTranslation = GetPlayerCursorPointedWorldPosition();
                }
            }

            HandlePopulationGrowth();

            citizenMovingSystem.Process(delta, population);

            if (movingToSocietyStage)
            {
                if (AnimateCameraZoomTowards(Constants.SOCIETY_CAMERA_ZOOM_INDUSTRIAL_EQUIVALENT, delta) &&
                    !stageLeaveTransitionStarted)
                {
                    HUD.EnsureGameIsUnpausedForEditor();

                    GD.Print("Starting fade out to industrial stage");

                    // The fade is pretty long here to give some time after the camera stops moving before the fade out
                    // is complete
                    TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.5f, SwitchToIndustrialScene,
                        false);
                    stageLeaveTransitionStarted = true;
                }
            }
            else
            {
                if (!movingToSocietyStage && structureSystem.CachedFactoryPower > 0)
                {
                    GD.Print("There's now a finished factory, starting going to industrial stage");
                    movingToSocietyStage = true;
                }
            }
        }

        HUD.UpdatePopulationDisplay(population);
    }

    public override void StartMusic()
    {
        Jukebox.Instance.PlayCategory("SocietyStage");
    }

    public PlacedStructure AddBuilding(StructureDefinition structureDefinition, Transform location)
    {
        // TODO: Proper storing of created structures for easier processing
        return SpawnHelpers.SpawnStructure(structureDefinition, location, rootOfDynamicallySpawned, structureScene);
    }

    public override void StartNewGame()
    {
        CurrentGame = GameProperties.StartSocietyStageGame(new WorldGenerationSettings());

        // Spawn an initial society center to get the player started when directly going to this stage
        var societyCenter =
            AddBuilding(SimulationParameters.Instance.GetStructure("societyCenter"), Transform.Identity);
        societyCenter.ForceCompletion();

        base.StartNewGame();
    }

    public void PerformBuildOrOpenMenu()
    {
        if (buildingTypeToPlace != null)
        {
            PlaceGhostBuilding();
            return;
        }

        // TODO: the buildable structures will need to refresh every few seconds as the passive resource income makes
        // it likely the player can build something new while this is open
        selectBuildingPopup.OpenWithStructures(CurrentGame!.TechWeb.GetAvailableStructures(), this, resourceStorage);
    }

    // TODO: for uniformity these (primary, secondary) should probably go through the society input node like in
    // space stage
    [RunOnKeyDown("e_primary")]
    public bool PlaceGhostBuilding()
    {
        if (buildingTypeToPlace == null)
            return false;

        // Warning before advancing stages
        if (!stageAdvanceConfirmed && buildingTypeToPlace.HasComponentFactory<FactoryComponentFactory>())
        {
            // TODO: pause the game while this popup is open
            industrialStageConfirmPopup.PopupCenteredShrink();
            return true;
        }

        if (!PlaceCurrentStructureIfPossible())
        {
            // TODO: play an invalid placement sound
            GD.Print("Couldn't place selected building");
            return true;
        }

        return true;
    }

    [RunOnKeyDown("e_secondary", Priority = 1)]
    [RunOnKeyDown("e_cancel_current_action", Priority = 1)]
    public bool CancelBuildingPlaceIfInProgress()
    {
        if (buildingTypeToPlace == null)
            return false;

        buildingToPlaceGhost?.QueueFree();
        buildingToPlaceGhost = null;

        buildingTypeToPlace = null;
        return true;
    }

    public void OnStructureTypeSelected(StructureDefinition structureDefinition)
    {
        // This is canceled to free up any previous resources
        CancelBuildingPlaceIfInProgress();

        buildingTypeToPlace = structureDefinition;

        buildingToPlaceGhost = buildingTypeToPlace.GhostScene.Instance<Spatial>();

        rootOfDynamicallySpawned.AddChild(buildingToPlaceGhost);
    }

    public override bool IsGameOver()
    {
        // TODO: lose condition
        return false;
    }

    protected override void SetupStage()
    {
        base.SetupStage();

        foodResource = SimulationParameters.Instance.GetWorldResource("food");

        resourceStorage.Capacity = structureSystem.CalculateTotalStorage();

        citizenMovingSystem.Init(CurrentGame!.GameWorld.PlayerSpecies);

        // TODO: this is only unlocked here for now to prevent the player from accidentally wasting limited resources
        // in the previous prototype. Once that's no longer the case discovering this should be moved to the previous
        // stage. This is here rather than OnGameStarted to have this unlock appear to the player.
        CurrentGame.TechWeb.UnlockTechnology(SimulationParameters.Instance.GetTechnology("hunterGathering"));
    }

    protected override void OnGameStarted()
    {
        // Intentionally not translated prototype message
        HUD.HUDMessages.ShowMessage(
            "You are now in the Society Stage prototype. Build a few basic structures to gain resources, " +
            "then research factories and build one to advance to the next stage.",
            DisplayDuration.ExtraLong);
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
            if (SelectBuildingPopupPath != null)
            {
                SelectBuildingPopupPath.Dispose();
                IndustrialStageConfirmPopupPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void HandlePopulationGrowth()
    {
        var housing = structureSystem.CachedHousingCapacity;

        // TODO: adjust food need and reproduction rate based on species properties
        float requiredForNewMember = 1;

        // Don't grow if not enough housing
        // And for now just for the prototype only grow when otherwise full on food
        if (population >= housing || resourceStorage.GetAvailableAmount(foodResource) < resourceStorage.Capacity)
            return;

        if (resourceStorage.Take(foodResource, requiredForNewMember) > 0)
        {
            // Took some food to grow
            ++population;
        }
    }

    private bool PlaceCurrentStructureIfPossible()
    {
        if (buildingTypeToPlace == null || buildingToPlaceGhost == null)
            return false;

        if (!buildingTypeToPlace.TakeResourcesToStartIfPossible(resourceStorage))
            return false;

        // TODO: free space check, could maybe set a flag in _Process that is then used here
        AddBuilding(buildingTypeToPlace, buildingToPlaceGhost.GlobalTransform);

        buildingToPlaceGhost?.QueueFree();
        buildingToPlaceGhost = null;

        buildingTypeToPlace = null;
        return true;
    }

    private void ConfirmStageAdvance()
    {
        GD.Print("Confirmed advancing to industrial stage");
        stageAdvanceConfirmed = true;
        PlaceGhostBuilding();
    }

    private void CancelStageAdvance()
    {
        CancelBuildingPlaceIfInProgress();
    }

    private void SwitchToIndustrialScene()
    {
        GD.Print("Switching to industrial scene");

        var industrialStage =
            SceneManager.Instance.LoadScene(MainGameState.IndustrialStage).Instance<IndustrialStage>();
        industrialStage.CurrentGame = CurrentGame;
        industrialStage.TakeInitialResourcesFrom(SocietyResources);

        SceneManager.Instance.SwitchToScene(industrialStage);

        // Preserve some of the state when moving to the stage for extra continuity
        industrialStage.CameraWorldPoint = CameraWorldPoint / Constants.INDUSTRIAL_STAGE_SIZE_MULTIPLIER;

        var cityPosition = industrialStage.CameraWorldPoint;
        cityPosition.y = 0;

        // TODO: preserve the initial city building visuals
        industrialStage.AddCity(new Transform(Basis.Identity, cityPosition), true);
    }
}
