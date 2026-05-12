using Arch.Core;
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
    private NodeWithInput editorNode = null!;
#pragma warning restore CA2213

    public override void ReloadGUI()
    {
        infiniteMp.ButtonPressed = CheatManager.InfiniteMP;
    }

    private void OnRevealAllPatchesPressed()
    {
        CheatManager.RevealAllPatches();
    }

    private void OnUnlockAllOrganellesPressed()
    {
        CheatManager.UnlockAllOrganelles();
    }

    private void InfiniteMPToggled(bool value)
    {
        SetInfiniteMP(value);

        if (editorNode is IEditor editor)
            editor.DirtyMutationPointsCache();
    }
}
