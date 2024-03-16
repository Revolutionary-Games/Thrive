using Godot;

/// <summary>
///   Cheat menu for the <see cref="MicrobeEditor"/>
/// </summary>
public partial class MicrobeEditorCheatMenu : CheatMenu
{
    [Export]
    public NodePath? InfiniteMpPath;

#pragma warning disable CA2213
    private CustomCheckBox infiniteMp = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        infiniteMp = GetNode<CustomCheckBox>(InfiniteMpPath);
        base._Ready();
    }

    public override void ReloadGUI()
    {
        infiniteMp.ButtonPressed = CheatManager.InfiniteMP;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            InfiniteMpPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnRevealAllPatchesPressed()
    {
        CheatManager.RevealAllPatches();
    }

    private void OnUnlockAllOrganellesPressed()
    {
        CheatManager.UnlockAllOrganelles();
    }
}
