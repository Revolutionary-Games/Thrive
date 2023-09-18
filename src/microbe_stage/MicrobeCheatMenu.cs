using Godot;

/// <summary>
///   Handles the microbe cheat menu
/// </summary>
public class MicrobeCheatMenu : CheatMenu
{
    [Export]
    public NodePath? InfiniteCompoundsPath;

    [Export]
    public NodePath GodModePath = null!;

    [Export]
    public NodePath DisableAIPath = null!;

    [Export]
    public NodePath LockTimePath = null!;

    [Export]
    public NodePath SpeedSliderPath = null!;

    [Export]
    public NodePath PlayerDividePath = null!;

    [Export]
    public NodePath SpawnEnemyPath = null!;

    [Export]
    public NodePath DespawnAllEntitiesPath = null!;

    [Export]
    public NodePath ManuallySetTimePath = null!;

    [Export]
    public NodePath TargetTimePath = null!;

#pragma warning disable CA2213
    private CustomCheckBox infiniteCompounds = null!;
    private CustomCheckBox godMode = null!;
    private CustomCheckBox disableAI = null!;
    private CustomCheckBox lockTime = null!;
    private Slider speed = null!;
    private Button playerDivide = null!;
    private Button spawnEnemy = null!;
    private Button despawnAllEntities = null!;
    private CustomCheckBox manuallySetTime = null!;
    private Slider targetTime = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        infiniteCompounds = GetNode<CustomCheckBox>(InfiniteCompoundsPath);
        godMode = GetNode<CustomCheckBox>(GodModePath);
        disableAI = GetNode<CustomCheckBox>(DisableAIPath);
        lockTime = GetNode<CustomCheckBox>(LockTimePath);
        speed = GetNode<Slider>(SpeedSliderPath);
        playerDivide = GetNode<Button>(PlayerDividePath);
        despawnAllEntities = GetNode<Button>(DespawnAllEntitiesPath);
        spawnEnemy = GetNode<Button>(SpawnEnemyPath);
        manuallySetTime = GetNode<CustomCheckBox>(ManuallySetTimePath);
        targetTime = GetNode<Slider>(TargetTimePath);

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
        lockTime.Pressed = CheatManager.LockTime;
        speed.Value = CheatManager.Speed;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (InfiniteCompoundsPath != null)
            {
                InfiniteCompoundsPath.Dispose();
                GodModePath.Dispose();
                DisableAIPath.Dispose();
                LockTimePath.Dispose();
                SpeedSliderPath.Dispose();
                PlayerDividePath.Dispose();
                SpawnEnemyPath.Dispose();
                DespawnAllEntitiesPath.Dispose();
                ManuallySetTimePath.Dispose();
                TargetTimePath.Dispose();
            }
        }

        base.Dispose(disposing);
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
