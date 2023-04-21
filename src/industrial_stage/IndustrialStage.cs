using Godot;
using Newtonsoft.Json;

/// <summary>
///   The main class handling the industrial stage functions
/// </summary>
public class IndustrialStage : StrategyStageBase, ISocietyStructureDataAccess
{
#pragma warning disable CA2213
    private PackedScene cityScene = null!;
#pragma warning restore CA2213

    private WorldResource foodResource = null!;

    private long population = 1;

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

        // TODO: systems

        HUD.Init(this);

        SetupStage();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        HUD = GetNode<IndustrialHUD>("IndustrialHUD");

        // TODO: Systems
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!IsGameOver())
        {
            // TODO: city processing system to get resources and research

            // TODO: actual calculations from cities
            resourceStorage.Capacity = 1000;

            // TODO: make population consume food

            HandlePopulationGrowth();
        }

        HUD.UpdatePopulationDisplay(population);
    }

    public override void StartMusic()
    {
        Jukebox.Instance.PlayCategory("IndustrialStage");
    }

    public PlacedCity AddCity(Transform location)
    {
        // TODO: Proper storing of created structures for easier processing
        return SpawnHelpers.SpawnCity(location, rootOfDynamicallySpawned, cityScene);
    }

    public override void StartNewGame()
    {
        CurrentGame = GameProperties.StartIndustrialStageGame(new WorldGenerationSettings());

        // Spawn an initial city
        AddCity(Transform.Identity);

        base.StartNewGame();
    }

    protected override void SetupStage()
    {
        base.SetupStage();

        foodResource = SimulationParameters.Instance.GetWorldResource("food");

        // TODO: capacity from cities
        resourceStorage.Capacity = 1000;

        // TODO: systems
    }

    protected override void OnGameStarted()
    {
        // Intentionally not translated prototype message
        HUD.HUDMessages.ShowMessage(
            "You have reached the end of the prototypes. For now...",
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

    private void HandlePopulationGrowth()
    {
        // TODO: housing calculation
        var housing = 1000;

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
}
