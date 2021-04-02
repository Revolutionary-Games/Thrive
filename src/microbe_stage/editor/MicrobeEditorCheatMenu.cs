using Godot;

/// <summary>
///   Handles the microbe editor cheat menu
/// </summary>
public class MicrobeEditorCheatMenu : CheatMenu
{
    [Export]
    public NodePath InfiniteMPPath;

    private CheckBox infiniteMP;

    public MicrobeEditorCheatMenu()
    {
        infiniteMP = GetNode<CheckBox>(InfiniteMPPath);
    }

    public override void ReloadGUI()
    {
        infiniteMP.Pressed = CheatManager.InfiniteMP;
    }
}
