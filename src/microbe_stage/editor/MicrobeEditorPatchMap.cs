using System;
using Godot;

/// <summary>
///   Microbe patch map GUI
/// </summary>
[IgnoreNoMethodsTakingInput]
[SceneLoadedClass("res://src/microbe_stage/editor/MicrobeEditorPatchMap.tscn", UsesEarlyResolve = false)]
public partial class MicrobeEditorPatchMap : PatchMapEditorComponent<IEditorWithPatches>
{
    private readonly Action<Patch> micheSelectDelegate;

#pragma warning disable CA2213
    [Export]
    private PopupMicheViewer micheViewer = null!;
#pragma warning restore CA2213

    public MicrobeEditorPatchMap()
    {
        micheSelectDelegate = OnShowMiche;
    }

    public void MarkDrawerDirty()
    {
        mapDrawer.MarkDirty();
    }

    public void UpdatePatchEvents()
    {
        mapDrawer.UpdatePatchEvents();
    }

    protected override void UpdateShownPatchDetails()
    {
        base.UpdateShownPatchDetails();

        var patch = mapDrawer.SelectedPatch;

        if (patch == null)
            return;

        Editor.CurrentGame.TutorialState.SendEvent(TutorialEventType.MicrobeEditorPatchSelected,
            new PatchEventArgs(patch), this);
    }

    protected override Action<Patch> GetMicheSelectionCallback()
    {
        return micheSelectDelegate;
    }

    private void OnShowMiche(Patch patch)
    {
        if (Editor.PreviousAutoEvoResults == null)
        {
            GD.PrintErr("Missing auto-evo results, can't show miches");
            return;
        }

        micheViewer.ShowMiches(patch.Name.ToString(), Editor.PreviousAutoEvoResults.GetMicheForPatch(patch));
    }
}
