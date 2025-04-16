using Godot;

/// <summary>
///   Handles the microbe cheat menu
/// </summary>
public partial class MicrobeCheatMenu : CheatMenu
{
#pragma warning disable CA2213
    [Export]
    private CheckBox infiniteCompounds = null!;
    [Export]
    private CheckBox godMode = null!;
    [Export]
    private CheckBox disableAI = null!;
    [Export]
    private CheckBox unlimitGrowthSpeed = null!;
    [Export]
    private CheckBox lockTime = null!;
    [Export]
    private Slider speed = null!;
    [Export]
    private Button playerDivide = null!;
    [Export]
    private Button spawnEnemy = null!;
    [Export]
    private Button despawnAllEntities = null!;
    [Export]
    private CheckBox manuallySetTime = null!;
    [Export]
    private Slider targetTime = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
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
            {
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
