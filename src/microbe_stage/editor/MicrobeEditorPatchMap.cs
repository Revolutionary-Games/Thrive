/// <summary>
///   Microbe patch map GUI
/// </summary>
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
