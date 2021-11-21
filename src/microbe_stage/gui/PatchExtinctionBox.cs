using System;
using Godot;

public class PatchExtinctionBox : Control
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
        animationPlayer.PlayBackwards();
        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, animationPlayer.CurrentAnimationLength, false);
        TransitionManager.Instance.StartTransitions(this, nameof(OnFadedToBlack));
        patchDetailsPanel.MouseFilter = MouseFilterEnum.Ignore;
    }

    private void OnFadedToBlack()
    {
        GoToNewPatch?.Invoke(patchDetailsPanel.Patch);

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeIn, animationPlayer.CurrentAnimationLength, false);
        TransitionManager.Instance.StartTransitions();
    }

    private void SelectedPatchChanged(PatchMapDrawer drawer)
    {
        patchDetailsPanel.IsPatchMoveValid = drawer.SelectedPatch != null;
        patchDetailsPanel.Patch = drawer.SelectedPatch;
    }
}
