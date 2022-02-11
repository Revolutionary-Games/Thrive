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
    public NodePath GenerateSpawnMapPath = null!;

    [Export]
    public NodePath CurrentSectorPath = null!;

    [Export]
    public NodePath MicrobeStagePath = null!;

    private CustomCheckBox infiniteCompounds = null!;
    private CustomCheckBox godMode = null!;
    private CustomCheckBox disableAI = null!;
    private Slider speed = null!;
    private Button playerDivide = null!;
    private Button spawnEnemy = null!;
    private Button generateSpawnMap = null!;
    private Label currentSector = null!;
    private MicrobeStage microbeStage = null!;

    public override void _Ready()
    {
        infiniteCompounds = GetNode<CustomCheckBox>(InfiniteCompoundsPath);
        godMode = GetNode<CustomCheckBox>(GodModePath);
        disableAI = GetNode<CustomCheckBox>(DisableAIPath);
        speed = GetNode<Slider>(SpeedSliderPath);
        playerDivide = GetNode<Button>(PlayerDividePath);
        spawnEnemy = GetNode<Button>(SpawnEnemyPath);
        generateSpawnMap = GetNode<Button>(GenerateSpawnMapPath);
        currentSector = GetNode<Label>(CurrentSectorPath);
        microbeStage = GetNode<MicrobeStage>(MicrobeStagePath);

        playerDivide.Connect("pressed", this, nameof(OnPlayerDivideClicked));
        spawnEnemy.Connect("pressed", this, nameof(OnSpawnEnemyClicked));
        generateSpawnMap.Connect("pressed", this, nameof(OnGenerateMapClicked));
        base._Ready();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        if (microbeStage.Player == null)
            return;

        var sector = microbeStage.Player.CurrentSector;
        var density = microbeStage.Spawner.GetSectorDensity(sector);
        currentSector.Text = $"{sector}: {density}";
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

    private void OnGenerateMapClicked()
    {
        microbeStage.Spawner.GenerateNoiseImage();
    }
}
