using Godot;

/// <summary>
///   Cheat menu for the <see cref="MicrobeEditor"/>
/// </summary>
public partial class MicrobeEditorCheatMenu : CheatMenu
{
#pragma warning disable CA2213
    [Export]
    private CheckBox infiniteMp = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
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
