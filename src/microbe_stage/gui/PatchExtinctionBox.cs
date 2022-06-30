using System;
using Godot;

public class PatchExtinctionBox : Control
{
    [Export]
    public NodePath PatchMapDrawerPath = null!;

    [Export]
    public NodePath PatchDetailsPanelPath = null!;

    [Export]
    public NodePath AnimationPlayer = null!;

    private PatchMapDrawer mapDrawer = null!;
    private PatchDetailsPanel detailsPanel = null!;
    private AnimationPlayer animationPlayer = null!;

    public PatchMap? Map
    {
        get => mapDrawer.Map;
        set
        {
            mapDrawer.Map = value;
            mapDrawer.SetPatchEnabledStatuses(value!.Patches.Values, p => p.GetSpeciesPopulation(PlayerSpecies) > 0);
        }
    }

    public Species PlayerSpecies { get; set; } = null!;

    public Action<Patch> OnMovedToNewPatch { get; set; } = null!;

    public override void _Ready()
    {
        mapDrawer = GetNode<PatchMapDrawer>(PatchMapDrawerPath);
        detailsPanel = GetNode<PatchDetailsPanel>(PatchDetailsPanelPath);
        animationPlayer = GetNode<AnimationPlayer>(AnimationPlayer);

        detailsPanel.CurrentPatch = Map?.CurrentPatch;
        detailsPanel.SelectedPatch = null;

        detailsPanel.OnMoveToPatchClicked = NewPatchSelected;
        mapDrawer.OnSelectedPatchChanged = SelectedPatchChanged;
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationVisibilityChanged && Visible)
            animationPlayer.Play();
    }

    private void NewPatchSelected(Patch patch)
    {
        var animLength = animationPlayer.CurrentAnimationLength;

        animationPlayer.PlayBackwards();

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, animLength, () =>
        {
            if (detailsPanel.SelectedPatch == null)
                throw new InvalidOperationException("The patch must not be null at this point");

            OnMovedToNewPatch.Invoke(detailsPanel.SelectedPatch);

            TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, animLength);
            detailsPanel.MouseFilter = MouseFilterEnum.Stop;
        });

        detailsPanel.MouseFilter = MouseFilterEnum.Ignore;
    }

    private void SelectedPatchChanged(PatchMapDrawer drawer)
    {
        detailsPanel.IsPatchMoveValid = drawer.SelectedPatch != null;
        detailsPanel.SelectedPatch = drawer.SelectedPatch;
    }
}
