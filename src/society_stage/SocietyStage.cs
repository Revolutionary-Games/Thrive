using Godot;
using Newtonsoft.Json;

/// <summary>
///   The main class handling the society stage functions
/// </summary>
public class SocietyStage : StrategyStageBase
{
#pragma warning disable CA2213
    private PackedScene structureScene = null!;
#pragma warning restore CA2213

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public SocietyHUD HUD { get; private set; } = null!;

    [JsonIgnore]
    protected override IStrategyStageHUD BaseHUD => HUD;

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        structureScene = SpawnHelpers.LoadStructureScene();

        HUD.Init(this);

        // TODO: init systems

        SetupStage();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        HUD = GetNode<SocietyHUD>("SocietyHUD");

        // TODO: systems
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

    [RunOnKeyDown("g_pause")]
    public void PauseKeyPressed()
    {
        // Check nothing else has keyboard focus and pause the game
        if (HUD.GetFocusOwner() == null)
        {
            HUD.PauseButtonPressed(!HUD.Paused);
        }
    }

    protected override void OnGameStarted()
    {
        // Apparently there's nothing to do here for now...
    }

    protected override bool IsGameOver()
    {
        // TODO: lose condition
        return false;
    }

    protected override void OnGameOver()
    {
    }

    protected override void AutoSave()
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    protected override void PerformQuickSave()
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }
}
