using Godot;

public class MicrobeEditorCheatMenu : CheatMenu
{
    [Export]
    public NodePath InfiniteMpPath;

    private CustomCheckBox infiniteMp;

    public override void _Ready()
    {
        infiniteMp = GetNode<CustomCheckBox>(InfiniteMpPath);
        base._Ready();
    }

    public override void ReloadGUI()
    {
        infiniteMp.Pressed = CheatManager.InfiniteMP;
    }
}
