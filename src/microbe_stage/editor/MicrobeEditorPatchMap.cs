using System;
/// <summary>
///   Microbe patch map GUI
/// </summary>
[IgnoreNoMethodsTakingInput]
[SceneLoadedClass("res://src/microbe_stage/editor/MicrobeEditorPatchMap.tscn", UsesEarlyResolve = false)]
public class MicrobeEditorPatchMap : PatchMapEditorComponent<IEditorWithPatches>
{
    public override void _EnterTree()
    {
        base._EnterTree();
        CheatManager.OnRevealEntirePatchMapCheatUsed += OnRevealEntirePatchMapCheatUsed;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        CheatManager.OnRevealEntirePatchMapCheatUsed -= OnRevealEntirePatchMapCheatUsed;
    }

    protected override void UpdateShownPatchDetails()
    {
        base.UpdateShownPatchDetails();
        mapDrawer.IgnoreFogOfWar =
            Editor.FreeBuilding || mapDrawer.IgnoreFogOfWar;

        var patch = mapDrawer.SelectedPatch;

        if (patch == null)
            return;

        Editor.CurrentGame.TutorialState.SendEvent(TutorialEventType.MicrobeEditorPatchSelected,
            new PatchEventArgs(patch), this);
    }

    private void OnRevealEntirePatchMapCheatUsed(object sender, EventArgs args)
    {
        mapDrawer.IgnoreFogOfWar = true;
        mapDrawer.MarkDirty();
    }
}
