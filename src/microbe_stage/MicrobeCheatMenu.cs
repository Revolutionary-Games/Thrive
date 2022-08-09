using Godot;

/// <summary>
///   Handles the microbe cheat menu
/// </summary>
public class MicrobeCheatMenu : CheatMenu
{
    [Export]
    public NodePath InfiniteCompoundsPath = null!;

    [Export]
    public NodePath GodModePath = null!;

    [Export]
    public NodePath DisableAIPath = null!;

    [Export]
    public NodePath SpeedSliderPath = null!;

    [Export]
    public NodePath PlayerDividePath = null!;

    [Export]
    public NodePath SpawnEnemyPath = null!;

    [Export]
    public NodePath DespawnAllEntitiesPath = null!;

    private CustomCheckBox infiniteCompounds = null!;
    private CustomCheckBox godMode = null!;
    private CustomCheckBox disableAI = null!;
    private Slider speed = null!;
    private Button playerDivide = null!;
    private Button spawnEnemy = null!;
    private Button despawnAllEntities = null!;

    public override void _Ready()
    {
        infiniteCompounds = GetNode<CustomCheckBox>(InfiniteCompoundsPath);
        godMode = GetNode<CustomCheckBox>(GodModePath);
        disableAI = GetNode<CustomCheckBox>(DisableAIPath);
        speed = GetNode<Slider>(SpeedSliderPath);
        playerDivide = GetNode<Button>(PlayerDividePath);
        despawnAllEntities = GetNode<Button>(DespawnAllEntitiesPath);
        spawnEnemy = GetNode<Button>(SpawnEnemyPath);

        playerDivide.Connect("pressed", this, nameof(OnPlayerDivideClicked));
        spawnEnemy.Connect("pressed", this, nameof(OnSpawnEnemyClicked));
        despawnAllEntities.Connect("pressed", this, nameof(OnDespawnAllEntitiesClicked));
        base._Ready();
    }

    public override void ReloadGUI()
    {
        infiniteCompounds.Pressed = CheatManager.InfiniteCompounds;
        godMode.Pressed = CheatManager.GodMode;
        disableAI.Pressed = CheatManager.NoAI;
        speed.Value = CheatManager.Speed;
    }

    private void OnPlayerDivideClicked()
    {
        CheatManager.PlayerDuplication();
    }

    private void OnSpawnEnemyClicked()
    {
        CheatManager.SpawnEnemy();
    }

    private void OnDespawnAllEntitiesClicked()
    {
        CheatManager.DespawnAllEntities();
    }
}
