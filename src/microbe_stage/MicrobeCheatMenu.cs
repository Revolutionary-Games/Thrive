using Godot;

/// <summary>
///   Handles the microbe cheat menu
/// </summary>
public class MicrobeCheatMenu : CheatMenu
{
    [Export]
    public NodePath InfiniteCompoundsPath;

    [Export]
    public NodePath GodModePath;

    [Export]
    public NodePath DisableAIPath;

    [Export]
    public NodePath SpeedSliderPath;

    [Export]
    public NodePath PlayerDividePath;

    private CustomCheckBox infiniteCompounds;
    private CustomCheckBox godMode;
    private CustomCheckBox disableAI;
    private Slider speed;
    private Button playerDivide;

    public override void _Ready()
    {
        infiniteCompounds = GetNode<CustomCheckBox>(InfiniteCompoundsPath);
        godMode = GetNode<CustomCheckBox>(GodModePath);
        disableAI = GetNode<CustomCheckBox>(DisableAIPath);
        speed = GetNode<Slider>(SpeedSliderPath);
        playerDivide = GetNode<Button>(PlayerDividePath);

        playerDivide.Connect("pressed", this, nameof(OnPlayerDivideClicked));
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
}
