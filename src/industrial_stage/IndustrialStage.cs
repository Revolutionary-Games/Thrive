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

#pragma warning disable CA2213
    private PackedScene cityScene = null!;

    private StrategicEntityNameLabelSystem nameLabelSystem = null!;
#pragma warning restore CA2213

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CitySystem citySystem = null!;

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
            // TODO: AI civilizations tech web's
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
            "To advance research rocketry and then select your city to build it to be able to go to space",
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
            NameLabelSystemPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OpenCityInfo(PlacedCity city)
    {
        HUD.OpenCityScreen(city);
    }
}
