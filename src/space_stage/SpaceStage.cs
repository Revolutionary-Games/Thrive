using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   The main class handling the space stage functions (and also the ascension stage as that just adds some extra
///   tools)
/// </summary>
public class SpaceStage : StrategyStageBase, ISocietyStructureDataAccess
{
    [Export]
    public NodePath? NameLabelSystemPath;

    // [Export]
    // public NodePath DescendConfirmationPopupPath = null!;

#pragma warning disable CA2213
    private StrategicEntityNameLabelSystem nameLabelSystem = null!;

    // private CustomConfirmationDialog descendConfirmationPopup = null!;

    private PackedScene planetScene = null!;
    private PackedScene fleetScene = null!;
#pragma warning restore CA2213

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private PlanetSystem planetSystem = null!;

    private bool zoomingOutFromFleet;
    private float targetZoomOutLevel;

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

        HUD = GetNode<SpaceHUD>("SpaceHUD");

        // descendConfirmationPopup = GetNode<CustomConfirmationDialog>(DescendConfirmationPopupPath);

        // Systems
        nameLabelSystem = GetNode<StrategicEntityNameLabelSystem>(NameLabelSystemPath);
        planetSystem = new PlanetSystem(rootOfDynamicallySpawned);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (zoomingOutFromFleet)
        {
            if (AnimateCameraZoomTowards(targetZoomOutLevel, delta, 0.8f))
            {
                // Zoom complete, unlock the camera for normal movement
                strategicCamera.AllowPlayerInput = false;
                zoomingOutFromFleet = false;
            }
        }

        if (!IsGameOver())
        {
            planetSystem.Process(delta, this);

            resourceStorage.Capacity = planetSystem.CachedTotalStorage;
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

        var binds = new Godot.Collections.Array();
        binds.Add(planet);
        planet.Connect(nameof(PlacedPlanet.OnSelected), this, nameof(OpenPlanetInfo), binds);

        return planet;
    }

    public SpaceFleet AddFleet(Transform location, UnitType initialShip, bool playerFleet)
    {
        var fleet = SpawnHelpers.SpawnFleet(location, rootOfDynamicallySpawned, fleetScene, playerFleet, initialShip);

        var binds = new Godot.Collections.Array();
        binds.Add(fleet);
        fleet.Connect(nameof(SpaceFleet.OnSelected), this, nameof(OpenFleetInfo), binds);

        return fleet;
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

    /// <summary>
    ///   Jumps the camera to a fleet position and then smoothly zooms out
    /// </summary>
    /// <param name="fleet">The fleet to zoom out from</param>
    public void ZoomOutFromFleet(SpaceFleet fleet)
    {
        strategicCamera.WorldLocation = fleet.GlobalTranslation;

        targetZoomOutLevel = strategicCamera.ZoomLevel;
        strategicCamera.ZoomLevel = strategicCamera.MinZoomLevel;

        zoomingOutFromFleet = true;
    }

    public void TakeInitialResourcesFrom(IResourceContainer resources)
    {
        SocietyResources.TransferFrom(resources);
    }

    protected override void SetupStage()
    {
        base.SetupStage();

        // Get systems started
        planetSystem.CalculateDerivedStats();
        resourceStorage.Capacity = planetSystem.CachedTotalStorage;
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
            // When DescendConfirmationPopupPath is uncommented this will be needed
            // ReSharper disable once UseNullPropagation
            if (NameLabelSystemPath != null)
            {
                NameLabelSystemPath.Dispose();

                // DescendConfirmationPopupPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OpenPlanetInfo(PlacedPlanet planet)
    {
        HUD.OpenPlanetScreen(planet);
    }

    private void OpenFleetInfo(PlacedPlanet planet)
    {
        // TODO: implement this
        throw new NotImplementedException();
    }
}
