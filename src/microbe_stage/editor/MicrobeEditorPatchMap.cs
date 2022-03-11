/// <summary>
///   Microbe patch map GUI
/// </summary>
[IgnoreNoMethodsTakingInputAttribute]
[SceneLoadedClass("res://src/microbe_stage/editor/MicrobeEditorPatchMap.tscn", UsesEarlyResolve = false)]
public class MicrobeEditorPatchMap : PatchMapEditorComponent<MicrobeEditor>
{
    protected override void UpdateShownPatchDetails()
    {
        base.UpdateShownPatchDetails();

        var patch = mapDrawer.SelectedPatch;

        if (patch == null)
            return;

        Editor.TutorialState.SendEvent(TutorialEventType.MicrobeEditorPatchSelected, new PatchEventArgs(patch), this);
    }
}
