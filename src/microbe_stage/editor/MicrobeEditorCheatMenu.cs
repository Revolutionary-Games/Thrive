using Godot;

public class MicrobeEditorCheatMenu : CheatMenu
{
    [Export]
    public NodePath InfiniteMpPath;

    private CheckBox infiniteMp;

    public override void _Ready()
    {
        infiniteMp = GetNode<CheckBox>(InfiniteMpPath);
        base._Ready();
    }

    public override void ReloadGUI()
    {
        infiniteMp.Pressed = CheatManager.InfiniteMP;
    }
}
