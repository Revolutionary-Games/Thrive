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

    private CheckBox infiniteCompounds;
    private CheckBox godMode;
    private CheckBox disableAI;
    private Slider speed;

    public MicrobeCheatMenu()
    {
        infiniteCompounds = GetNode<CheckBox>(InfiniteCompoundsPath);
        godMode = GetNode<CheckBox>(GodModePath);
        disableAI = GetNode<CheckBox>(DisableAIPath);
        speed = GetNode<Slider>(SpeedSliderPath);
    }

    public override void ReloadGUI()
    {
        infiniteCompounds.Pressed = CheatManager.InfiniteCompounds;
        godMode.Pressed = CheatManager.GodMode;
        disableAI.Pressed = CheatManager.NoAI;
        speed.Value = CheatManager.Speed;
    }
}
