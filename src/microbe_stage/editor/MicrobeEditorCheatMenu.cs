using Godot;

/// <summary>
///   Cheat menu for the <see cref="MicrobeEditor"/>
/// </summary>
public partial class MicrobeEditorCheatMenu : CheatMenu
{
#pragma warning disable CA2213
    [Export]
    private CheckBox infiniteMp = null!;

    [Export]
    private CheckBox moveToAnyPatch = null!;
#pragma warning restore CA2213

    public override void ReloadGUI()
    {
        infiniteMp.ButtonPressed = CheatManager.InfiniteMP;
        moveToAnyPatch.ButtonPressed = CheatManager.MoveToAnyPatch;
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
