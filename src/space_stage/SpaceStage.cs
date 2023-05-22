using System;
using Godot;
using Newtonsoft.Json;
using Array = Godot.Collections.Array;

/// <summary>
///   The main class handling the space stage functions (and also the ascension stage as that just adds some extra
///   tools)
/// </summary>
public class SpaceStage : StrategyStageBase, ISocietyStructureDataAccess
{
    [Export]
    public NodePath? NameLabelSystemPath;

    [Export]
    public NodePath AscensionCongratulationsPopupPath = null!;

    [Export]
    public NodePath DescendSetupPopupPath = null!;

#pragma warning disable CA2213
    private StrategicEntityNameLabelSystem nameLabelSystem = null!;

    private AscensionCongratulationsPopup ascensionCongratulationsPopup = null!;
    private DescendConfirmationDialog descendConfirmationPopup = null!;

    private PackedScene planetScene = null!;
    private PackedScene fleetScene = null!;
    private PackedScene structureScene = null!;

    private Spatial? structureToPlaceGhost;
#pragma warning restore CA2213

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private PlanetSystem planetSystem = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private SpaceStructureSystem structureSystem = null!;

    private SpaceStructureDefinition? structureTypeToPlace;
    private SpaceFleet? structurePlacingFleet;

    [JsonProperty]
    private bool zoomingOutFromFleet;

    [JsonProperty]
    private float targetZoomOutLevel;

    [JsonProperty]
    private float minZoomLevelToRestore;

    [JsonProperty]
    private bool playingAscensionAnimation;

    private bool fadingOutToAscension;

    [JsonProperty]
    private float ascendAnimationElapsed;

    [JsonProperty]
    private Vector3 ascendAnimationStart;

    [JsonProperty]
    private Vector3 ascendAnimationEnd;

    private float defaultZoomLevel;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public SpaceHUD HUD { get; private set; } = null!;

    [JsonIgnore]
    public IResourceContainer SocietyResources => resourceStorage;

    [JsonIgnore]
    protected override IStrategyStageHUD BaseHUD => HUD;

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        planetScene = SpawnHelpers.LoadPlanetScene();
        fleetScene = SpawnHelpers.LoadFleetScene();
        structureScene = SpawnHelpers.LoadSpaceStructureScene();

        nameLabelSystem.Init(strategicCamera, rootOfDynamicallySpawned);
        nameLabelSystem.Visible = true;

        HUD.Init(this);

        SetupStage();

        minZoomLevelToRestore = strategicCamera.MinZoomLevel;
        defaultZoomLevel = strategicCamera.ZoomLevel;
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        HUD = GetNode<SpaceHUD>("SpaceHUD");

        ascensionCongratulationsPopup = GetNode<AscensionCongratulationsPopup>(AscensionCongratulationsPopupPath);
        descendConfirmationPopup = GetNode<DescendConfirmationDialog>(DescendSetupPopupPath);

        // Systems
        nameLabelSystem = GetNode<StrategicEntityNameLabelSystem>(NameLabelSystemPath);
        planetSystem = new PlanetSystem(rootOfDynamicallySpawned);
        structureSystem = new SpaceStructureSystem(rootOfDynamicallySpawned);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (zoomingOutFromFleet)
        {
            if (AnimateCameraZoomTowards(targetZoomOutLevel, delta, Constants.SPACE_INITIAL_ANIMATION_ZOOM_SPEED))
            {
                // Zoom complete, unlock the camera for normal movement
                strategicCamera.AllowPlayerInput = true;
                strategicCamera.MinZoomLevel = minZoomLevelToRestore;
                zoomingOutFromFleet = false;
            }
        }
        else if (playingAscensionAnimation)
        {
            if (!fadingOutToAscension)
            {
                ascendAnimationElapsed += delta;

                strategicCamera.WorldLocation = ascendAnimationStart.LinearInterpolate(ascendAnimationEnd,
                    Math.Min(1, ascendAnimationElapsed / Constants.SPACE_ASCEND_ANIMATION_DURATION));

                if (AnimateCameraZoomTowards(strategicCamera.MinZoomLevel, delta,
                        Constants.SPACE_ASCEND_ANIMATION_ZOOM_SPEED) &&
                    ascendAnimationElapsed >= Constants.SPACE_ASCEND_ANIMATION_DURATION)
                {
                    fadingOutToAscension = true;

                    TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut,
                        Constants.SPACE_ASCEND_SCREEN_FADE, SwitchToAscensionScene, false);
                }
            }
        }

        if (!IsGameOver())
        {
            planetSystem.Process(delta, this);

            structureSystem.Process(delta, this);

            resourceStorage.Capacity = planetSystem.CachedTotalStorage;

            // Update the place to place the selected structure
            if (structureToPlaceGhost != null)
            {
                // TODO: placement validity checks (placement restrictions and hitting other structures), show the
                // ghost differently when can't place
                structureToPlaceGhost.GlobalTranslation = GetPlayerCursorPointedWorldPosition();
            }

            // TODO: prototype code that can be entirely removed once the relevant feature is done
            if (!zoomingOutFromFleet)
            {
                if (strategicCamera.ZoomLevel <= strategicCamera.MinZoomLevel)
                {
                    // Intentionally not translated prototype message
                    HUD.HUDMessages.ShowMessage(
                        "Zooming back into planets is a planned Space Stage feature, it will be added at some point",
                        DisplayDuration.Short);
                }
            }
        }

        HUD.UpdatePopulationDisplay(planetSystem.CachedTotalPopulation);
    }

    public override void StartMusic()
    {
        Jukebox.Instance.PlayCategory("SpaceStage");
    }

    public PlacedPlanet AddPlanet(Transform location, bool playerPlanet)
    {
        if (CurrentGame == null)
            throw new InvalidOperationException("Current game not set");

        var techWeb = CurrentGame.TechWeb;

        if (!playerPlanet)
        {
            // TODO: AI civilizations' tech webs
            GD.Print("TODO: implement AI civilization tech unlocking");
            techWeb = new TechWeb();
        }

        var planet = SpawnHelpers.SpawnPlanet(location, rootOfDynamicallySpawned, planetScene, playerPlanet, techWeb);

        var binds = new Array();
        binds.Add(planet);
        planet.Connect(nameof(PlacedPlanet.OnSelected), this, nameof(OpenPlanetInfo), binds);

        return planet;
    }

    public SpaceFleet AddFleet(Transform location, UnitType initialShip, bool playerFleet)
    {
        var fleet = SpawnHelpers.SpawnFleet(location, rootOfDynamicallySpawned, fleetScene, playerFleet, initialShip);

        var binds = new Array();
        binds.Add(fleet);
        fleet.Connect(nameof(SpaceFleet.OnSelected), this, nameof(OpenFleetInfo), binds);

        return fleet;
    }

    public PlacedSpaceStructure AddStructure(SpaceStructureDefinition structureDefinition, Transform location,
        bool playerOwned)
    {
        var structure = SpawnHelpers.SpawnSpaceStructure(structureDefinition, location, rootOfDynamicallySpawned,
            structureScene,
            playerOwned);

        var binds = new Array();
        binds.Add(structure);
        structure.Connect(nameof(PlacedSpaceStructure.OnSelected), this, nameof(OpenStructureInfo), binds);

        return structure;
    }

    public override void StartNewGame()
    {
        CurrentGame = GameProperties.StartSpaceStageGame(new WorldGenerationSettings());

        // Spawn an initial planet
        var planet = AddPlanet(Transform.Identity, true);

        base.StartNewGame();

        // Initial spaceship like when coming from industrial
        var initialShip = SimulationParameters.Instance.GetUnitType("simpleSpaceRocket");

        AddFleet(new Transform(Basis.Identity, planet.GlobalTranslation + new Vector3(15, 0, 0)), initialShip,
            true);
    }

    public void SetupForExistingGameFromAnotherStage(bool spawnPlanet, UnitType spacecraft,
        IResourceContainer? societyResources)
    {
        if (GetTree() == null)
            throw new InvalidOperationException("This can only be called after this is scene attached");

        // Copy resources from the other stage
        if (societyResources != null)
            TakeInitialResourcesFrom(SocietyResources);

        if (spawnPlanet)
            AddPlanet(Transform.Identity, true);

        var fleet = AddFleet(new Transform(Basis.Identity, new Vector3(6, 0, 0)),
            spacecraft, true);

        // Focus the camera initially on the ship to make the stage transition smoother
        ZoomOutFromFleet(fleet);

        // Add an order to have the fleet be moving
        fleet.PerformOrder(new FleetMovementOrder(fleet, new Vector3(20, 0, 0)));
    }

    public void SelectUnitUnderCursor()
    {
        // TODO: allow multi selection when holding down shift

        var fleet = FindFleetAtWorldPosition(GetPlayerCursorPointedWorldPosition());

        if (fleet == null)
        {
            // Ensure no fleet is selected if nothing can be selected
            HUD.CloseFleetInfo();
        }
        else
        {
            OpenFleetInfo(fleet);
        }

        // TODO: selected units should have some kind of indicator around them
    }

    public void PerformUnitContextCommandIfSelected()
    {
        var fleet = HUD.GetSelectedFleet();

        // Disabled as the context sensitive check below when written will make this not usable with null propagate
        // ReSharper disable once UseNullPropagation
        if (fleet == null)
            return;

        // TODO: context sensitive commands (for now assumes movement always)

        // TODO: implement order queue only happening with shift pressed
        fleet.PerformOrder(new FleetMovementOrder(fleet, GetPlayerCursorPointedWorldPosition()));
    }

    public void StartPlacingStructure(SpaceFleet fleetToConstructWith, SpaceStructureDefinition structureDefinition)
    {
        CancelStructurePlaceIfInProgress();

        structureTypeToPlace = structureDefinition;
        structurePlacingFleet = fleetToConstructWith;

        structureToPlaceGhost = structureTypeToPlace.GhostScene.Instance<Spatial>();

        rootOfDynamicallySpawned.AddChild(structureToPlaceGhost);
    }

    public bool AttemptPlaceStructureIfInProgress()
    {
        if (structureTypeToPlace == null)
            return false;

        if (!PlaceCurrentStructureIfPossible())
        {
            // TODO: play an invalid placement sound (and show a hud message when the condition for failure is
            // complex
            GD.Print("Couldn't place selected structure");
            return true;
        }

        return true;
    }

    public bool CancelStructurePlaceIfInProgress()
    {
        if (structureTypeToPlace == null)
            return false;

        structureToPlaceGhost?.QueueFree();
        structureToPlaceGhost = null;

        structureTypeToPlace = null;
        return true;
    }

    /// <summary>
    ///   Jumps the camera to a fleet position and then smoothly zooms out
    /// </summary>
    /// <param name="fleet">The fleet to zoom out from</param>
    public void ZoomOutFromFleet(SpaceFleet fleet)
    {
        strategicCamera.WorldLocation = fleet.GlobalTranslation;

        targetZoomOutLevel = strategicCamera.ZoomLevel;
        minZoomLevelToRestore = strategicCamera.MinZoomLevel;

        var startZoom = minZoomLevelToRestore * Constants.SPACE_INITIAL_ANIMATION_MIN_ZOOM_SCALE;
        strategicCamera.MinZoomLevel = startZoom;
        strategicCamera.ZoomLevel = startZoom;

        zoomingOutFromFleet = true;
    }

    public void TakeInitialResourcesFrom(IResourceContainer resources)
    {
        SocietyResources.TransferFrom(resources);
    }

    public void OnStartAscension(PlacedSpaceStructure ascensionGate)
    {
        playingAscensionAnimation = true;
        ascendAnimationElapsed = 0;
        minZoomLevelToRestore = strategicCamera.MinZoomLevel;
        strategicCamera.MinZoomLevel *= Constants.SPACE_ASCEND_ANIMATION_MIN_ZOOM_SCALE;

        ascendAnimationStart = strategicCamera.WorldLocation;
        ascendAnimationEnd = ascensionGate.GlobalTranslation;

        strategicCamera.AllowPlayerInput = false;

        HUD.CloseAllOpenWindows();
    }

    public void OnReturnedFromAscension()
    {
        // Restore player input to the camera
        strategicCamera.AllowPlayerInput = true;
        strategicCamera.MinZoomLevel = minZoomLevelToRestore;
        strategicCamera.ZoomLevel = defaultZoomLevel;

        // Replay the animation
        HUD.OnEnterStageTransition(true, true);

        // And finally setup things right for the ascension
    }

    public void OnBecomeAscended()
    {
        if (CurrentGame == null)
            throw new InvalidOperationException("No current game");

        CurrentGame.OnBecomeAscended();

        // Show a message about becoming ascended
        ascensionCongratulationsPopup.ShowWithInfo(CurrentGame);

        // TODO: notify the hud about ascension if there's something in there that needs to react?
    }

    protected override void SetupStage()
    {
        base.SetupStage();

        // Get systems started
        planetSystem.CalculateDerivedStats();
        resourceStorage.Capacity = planetSystem.CachedTotalStorage;

        structureSystem.CalculateDerivedStats();
    }

    protected override void OnGameStarted()
    {
        // Intentionally not translated prototype message
        HUD.HUDMessages.ShowMessage(
            "Research and build the Ascension Gate and energy structures to power it, then activate it",
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
                AscensionCongratulationsPopupPath.Dispose();
                DescendSetupPopupPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private bool PlaceCurrentStructureIfPossible()
    {
        if (structureTypeToPlace == null || structureToPlaceGhost == null)
            return false;

        if (!structureTypeToPlace.TakeResourcesToStartIfPossible(resourceStorage))
            return false;

        // TODO: free space (and other conditions) check, could maybe set a flag in _Process that is then used here

        // Create the structure
        var structure = AddStructure(structureTypeToPlace, structureToPlaceGhost.GlobalTransform, true);

        structureToPlaceGhost?.QueueFree();
        structureToPlaceGhost = null;

        structureTypeToPlace = null;

        // Make the fleet go and build it
        if (structurePlacingFleet == null)
        {
            GD.PrintErr("No fleet to construct placed structure with");
            return true;
        }

        // TODO: more intelligent action interrupt / queueing

        // TODO: more intelligent calculation for the distance from which building is possible
        float buildDistance = 5;

        var structureToFleet = structurePlacingFleet.GlobalTranslation - structure.GlobalTranslation;

        var placeToBuildFrom = structure.GlobalTranslation + structureToFleet.Normalized() * buildDistance;

        structurePlacingFleet.QueueOrder(new FleetMovementOrder(structurePlacingFleet, placeToBuildFrom));
        structurePlacingFleet.QueueOrder(new FleetBuildOrder(structurePlacingFleet, structure, SocietyResources));

        return true;
    }

    private void OpenPlanetInfo(PlacedPlanet planet)
    {
        HUD.OpenPlanetScreen(planet);
    }

    private void OpenFleetInfo(SpaceFleet fleet)
    {
        HUD.OpenFleetInfo(fleet);
    }

    private void OpenStructureInfo(PlacedSpaceStructure structure)
    {
        HUD.OpenStructureInfo(structure);
    }

    private SpaceFleet? FindFleetAtWorldPosition(Vector3 location)
    {
        var radiusSquared = Constants.SPACE_FLEET_SELECTION_RADIUS * Constants.SPACE_FLEET_SELECTION_RADIUS;

        float bestDistance = float.MaxValue;
        SpaceFleet? bestFleet = null;

        foreach (var fleet in rootOfDynamicallySpawned.GetChildrenToProcess<SpaceFleet>(Constants
                     .SPACE_FLEET_ENTITY_GROUP))
        {
            var distance = fleet.GlobalTranslation.DistanceSquaredTo(location);

            if (distance <= radiusSquared)
            {
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestFleet = fleet;
                }
            }
        }

        return bestFleet;
    }

    private void SwitchToAscensionScene()
    {
        GD.Print("Switching to ascension ceremony scene");

        var ascensionScene =
            SceneManager.Instance.LoadScene(MainGameState.AscensionCeremony).Instance<AscensionCeremony>();
        ascensionScene.CurrentGame = CurrentGame;

        var us = (SpaceStage?)SceneManager.Instance.SwitchToScene(ascensionScene, true);

        if (us == this)
        {
            ascensionScene.ReturnToScene = this;
        }
        else
        {
            GD.PrintErr("Could not save current space stage");
        }
    }
}
