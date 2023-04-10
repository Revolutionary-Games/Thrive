/// <summary>
///   Microbe patch map GUI
/// </summary>
[IgnoreNoMethodsTakingInput]
[SceneLoadedClass("res://src/microbe_stage/editor/MicrobeEditorPatchMap.tscn", UsesEarlyResolve = false)]
public class MicrobeEditorPatchMap : PatchMapEditorComponent<IEditorWithPatches>
{
    public override void UpdateShownPatchDetails()
    {
        base.UpdateShownPatchDetails();

        var patch = mapDrawer.SelectedPatch;

        if (patch == null)
            return;

        Editor.CurrentGame.TutorialState.SendEvent(TutorialEventType.MicrobeEditorPatchSelected,
            new PatchEventArgs(patch), this);
    }
}
