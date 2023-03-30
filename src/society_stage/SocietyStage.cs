using Godot;
using Newtonsoft.Json;

/// <summary>
///   The main class handling the society stage functions
/// </summary>
public class SocietyStage : StrategyStageBase
{
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public SocietyHUD HUD { get; private set; } = null!;

    [JsonIgnore]
    protected override IStrategyStageHUD BaseHUD => HUD;

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        HUD.Init(this);

        // TODO: init systems

        SetupStage();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        HUD = GetNode<SocietyHUD>("HUD");

        // TODO: systems
    }

    public override void StartMusic()
    {
        // TODO: society stage music
    }

    public PlacedStructure AddBuilding(StructureDefinition structureDefinition, Transform location)
    {
        throw new System.NotImplementedException();
    }

    protected override void OnGameStarted()
    {
        throw new System.NotImplementedException();
    }

    protected override void StartGUIStageTransition(bool longDuration, bool returnFromEditor)
    {
        throw new System.NotImplementedException();
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
