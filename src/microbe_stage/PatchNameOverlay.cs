using Godot;

public class PatchNameOverlay : PanelContainer
{
    [Export]
    public NodePath PatchLabelPath = null!;

    [Export]
    public NodePath PatchOverlayAnimatorPath = null!;

    private Label patchLabel = null!;
    private AnimationPlayer patchOverlayAnimator = null!;

    public override void _Ready()
    {
        patchLabel = GetNode<Label>(PatchLabelPath);
        patchOverlayAnimator = GetNode<AnimationPlayer>(PatchOverlayAnimatorPath);
    }

    public void ShowName(string patchName)
    {
        patchLabel.Text = patchName;
        patchOverlayAnimator.Play("FadeInOut");
    }
}
