using Godot;

/// <summary>
///   Handles the microbe cheat menu
/// </summary>
public partial class MicrobeCheatMenu : CheatMenu
{
    [Export]
    public NodePath? InfiniteCompoundsPath;

    [Export]
    public NodePath GodModePath = null!;

    [Export]
    public NodePath DisableAIPath = null!;

    [Export]
    public NodePath UnlimitGrowthSpeedPath = null!;

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
    private CheckBox infiniteCompounds = null!;
    private CheckBox godMode = null!;
    private CheckBox disableAI = null!;
    private CheckBox unlimitGrowthSpeed = null!;
    private CheckBox lockTime = null!;
    private Slider speed = null!;
    private Button playerDivide = null!;
    private Button spawnEnemy = null!;
    private Button despawnAllEntities = null!;
    private CheckBox manuallySetTime = null!;
    private Slider targetTime = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        infiniteCompounds = GetNode<CheckBox>(InfiniteCompoundsPath);
        godMode = GetNode<CheckBox>(GodModePath);
        disableAI = GetNode<CheckBox>(DisableAIPath);
        unlimitGrowthSpeed = GetNode<CheckBox>(UnlimitGrowthSpeedPath);
        lockTime = GetNode<CheckBox>(LockTimePath);
        speed = GetNode<Slider>(SpeedSliderPath);
        playerDivide = GetNode<Button>(PlayerDividePath);
        despawnAllEntities = GetNode<Button>(DespawnAllEntitiesPath);
        spawnEnemy = GetNode<Button>(SpawnEnemyPath);
        manuallySetTime = GetNode<CheckBox>(ManuallySetTimePath);
        targetTime = GetNode<Slider>(TargetTimePath);

        playerDivide.Connect(BaseButton.SignalName.Pressed, new Callable(this, nameof(OnPlayerDivideClicked)));
        spawnEnemy.Connect(BaseButton.SignalName.Pressed, new Callable(this, nameof(OnSpawnEnemyClicked)));
        despawnAllEntities.Connect(BaseButton.SignalName.Pressed,
            new Callable(this, nameof(OnDespawnAllEntitiesClicked)));

        base._Ready();
    }

    public override void ReloadGUI()
    {
        infiniteCompounds.ButtonPressed = CheatManager.InfiniteCompounds;
        godMode.ButtonPressed = CheatManager.GodMode;
        disableAI.ButtonPressed = CheatManager.NoAI;
        unlimitGrowthSpeed.ButtonPressed = CheatManager.UnlimitedGrowthSpeed;
        lockTime.ButtonPressed = CheatManager.LockTime;
        speed.Value = CheatManager.Speed;
        manuallySetTime.ButtonPressed = CheatManager.ManuallySetTime;
        targetTime.Value = CheatManager.DayNightFraction;
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
                UnlimitGrowthSpeedPath.Dispose();
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

    private void SetUnlimitGrowthSpeed(bool isOn)
    {
        CheatManager.UnlimitedGrowthSpeed = isOn;
    }
}
