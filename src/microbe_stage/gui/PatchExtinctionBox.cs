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

    private PatchMapDrawer patchMapDrawer = null!;
    private PatchDetailsPanel patchDetailsPanel = null!;
    private AnimationPlayer animationPlayer = null!;

    public PatchMap? Map
    {
        get => patchMapDrawer.Map;
        set
        {
            patchMapDrawer.Map = value;
            patchMapDrawer.SetPatchEnabledStatuses(value!.Patches.Values,
                p => p.GetSpeciesPopulation(PlayerSpecies) > 0);
        }
    }

    public Species PlayerSpecies { get; set; } = null!;
    public Action<Patch> GoToNewPatch { get; set; } = null!;

    public override void _Ready()
    {
        patchMapDrawer = GetNode<PatchMapDrawer>(PatchMapDrawerPath);
        patchDetailsPanel = GetNode<PatchDetailsPanel>(PatchDetailsPanelPath);
        animationPlayer = GetNode<AnimationPlayer>(AnimationPlayer);

        patchDetailsPanel.CurrentPatch = Map?.CurrentPatch;
        patchDetailsPanel.Patch = null;
        patchDetailsPanel.OnMoveToPatchClicked = NewPatchSelected;

        patchMapDrawer.OnSelectedPatchChanged = SelectedPatchChanged;
    }

    public new void Show()
    {
        animationPlayer.Play();
        base.Show();
    }

    private void NewPatchSelected(Patch patch)
    {
        var animLength = animationPlayer.CurrentAnimationLength;

        animationPlayer.PlayBackwards();

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, animLength, () =>
            {
                if (patchDetailsPanel.Patch == null)
                    throw new InvalidOperationException("The patch must not be null at this point");

                GoToNewPatch.Invoke(patchDetailsPanel.Patch);

                TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, animLength);
                patchDetailsPanel.MouseFilter = MouseFilterEnum.Stop;
            });

        patchDetailsPanel.MouseFilter = MouseFilterEnum.Ignore;
    }

    private void SelectedPatchChanged(PatchMapDrawer drawer)
    {
        patchDetailsPanel.IsPatchMoveValid = drawer.SelectedPatch != null;
        patchDetailsPanel.Patch = drawer.SelectedPatch;
    }
}
