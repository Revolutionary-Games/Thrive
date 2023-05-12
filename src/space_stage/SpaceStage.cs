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
#pragma warning restore CA2213

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private PlanetSystem planetSystem = null!;

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
            // TODO: AI civilizations tech web's
            GD.Print("TODO: implement AI civilization tech unlocking");
            techWeb = new TechWeb();
        }

        var planet = SpawnHelpers.SpawnPlanet(location, rootOfDynamicallySpawned, planetScene, playerPlanet, techWeb);

        var binds = new Godot.Collections.Array();
        binds.Add(planet);
        planet.Connect(nameof(PlacedPlanet.OnSelected), this, nameof(OpenPlanetInfo), binds);

        return planet;
    }

    public override void StartNewGame()
    {
        CurrentGame = GameProperties.StartSpaceStageGame(new WorldGenerationSettings());

        // Spawn an initial planet
        AddPlanet(Transform.Identity, true);

        // TODO: initial spaceship like when coming from industrial
        throw new NotImplementedException();

        base.StartNewGame();
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
            "Research and build space energy structures, then an ascension gate and activate it",
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
}
