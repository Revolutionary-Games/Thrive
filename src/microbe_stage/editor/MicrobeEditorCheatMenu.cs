using Godot;

public class MicrobeEditorCheatMenu : CheatMenu
{
    [Export]
    public NodePath? InfiniteMpPath;

    [Export]
    public NodePath? RevealEntirePatchMapPath;

#pragma warning disable CA2213
    private CustomCheckBox infiniteMp = null!;
    private Button revealEntirePatchMap = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        infiniteMp = GetNode<CustomCheckBox>(InfiniteMpPath);
        revealEntirePatchMap = GetNode<Button>(RevealEntirePatchMapPath);

        revealEntirePatchMap.Connect("pressed", this, nameof(OnRevealEntirePatchMapClicked));
        base._Ready();
    }

    public override void ReloadGUI()
    {
        infiniteMp.Pressed = CheatManager.InfiniteMP;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            InfiniteMpPath?.Dispose();
            RevealEntirePatchMapPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnRevealEntirePatchMapClicked()
    {
        CheatManager.RevealEntirePatchMap();
    }
}
