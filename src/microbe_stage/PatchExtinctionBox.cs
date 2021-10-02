using System;
using Godot;

public class PatchExtinctionBox : PanelContainer
{
    [Export]
    public NodePath PatchMapDrawerPath;

    [Export]
    public NodePath PatchPanelPath;

    [Export]
    public NodePath AnimationPlayer;

    private PatchMapDrawer patchMapDrawer;
    private PatchPanel patchPanel;
    private AnimationPlayer animationPlayer;

    public PatchMap Map { get; set; }
    public Species PlayerSpecies { get; set; }

    public override void _Ready()
    {
        patchMapDrawer = GetNode<PatchMapDrawer>(PatchMapDrawerPath);
        patchPanel = GetNode<PatchPanel>(PatchPanelPath);
        animationPlayer = GetNode<AnimationPlayer>(AnimationPlayer);

        patchMapDrawer.Map = Map;
        patchPanel.CurrentPatch = Map.CurrentPatch;
        patchPanel.Patch = null;
        patchPanel.OnMoveToPatchClicked = NewPatchSelected;

        patchMapDrawer.OnSelectedPatchChanged = SelectedPatchChanged;

        foreach (var patch in Map.Patches.Values)
        {
            patchMapDrawer.SetPatchEnabledStatus(patch, patch.GetSpeciesPopulation(PlayerSpecies) > 0);
        }
    }

    private void NewPatchSelected(Patch patch)
    {
        animationPlayer.PlayBackwards();
    }

    private void SelectedPatchChanged(PatchMapDrawer drawer)
    {
        patchPanel.IsPatchMoveValid = drawer.SelectedPatch != null;
        patchPanel.Patch = drawer.SelectedPatch;
    }
}
