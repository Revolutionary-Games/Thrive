using System;
using Godot;

public class PatchExtinctionBox : PanelContainer
{
    [Export]
    public NodePath PatchMapDrawerPath;

    [Export]
    public NodePath PatchDetailsPanelPath;

    [Export]
    public NodePath AnimationPlayer;

    private PatchMapDrawer patchMapDrawer;
    private PatchDetailsPanel patchDetailsPanel;
    private AnimationPlayer animationPlayer;

    public PatchMap Map { get; set; }
    public Species PlayerSpecies { get; set; }
    public Action<Patch> GoToNewPatch { get; set; }

    public override void _Ready()
    {
        patchMapDrawer = GetNode<PatchMapDrawer>(PatchMapDrawerPath);
        patchDetailsPanel = GetNode<PatchDetailsPanel>(PatchDetailsPanelPath);
        animationPlayer = GetNode<AnimationPlayer>(AnimationPlayer);

        patchMapDrawer.Map = Map;
        patchDetailsPanel.CurrentPatch = Map.CurrentPatch;
        patchDetailsPanel.Patch = null;
        patchDetailsPanel.OnMoveToPatchClicked = NewPatchSelected;

        patchMapDrawer.OnSelectedPatchChanged = SelectedPatchChanged;

        foreach (var patch in Map.Patches.Values)
        {
            patchMapDrawer.SetPatchEnabledStatus(patch, patch.GetSpeciesPopulation(PlayerSpecies) > 0);
        }
    }

    private void NewPatchSelected(Patch patch)
    {
        GoToNewPatch?.Invoke(patch);
        animationPlayer.PlayBackwards();
    }

    private void SelectedPatchChanged(PatchMapDrawer drawer)
    {
        patchDetailsPanel.IsPatchMoveValid = drawer.SelectedPatch != null;
        patchDetailsPanel.Patch = drawer.SelectedPatch;
    }
}
