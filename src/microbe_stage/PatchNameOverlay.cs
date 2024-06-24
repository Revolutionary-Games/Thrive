using Godot;

/// <summary>
///   Shows the current patch name on screen briefly before fading out
/// </summary>
public partial class PatchNameOverlay : PanelContainer
{
    [Export]
    public NodePath? PatchLabelPath;

    [Export]
    public NodePath PatchOverlayAnimatorPath = null!;

#pragma warning disable CA2213
    private Label patchLabel = null!;
    private AnimationPlayer patchOverlayAnimator = null!;
#pragma warning restore CA2213

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PatchLabelPath != null)
            {
                PatchLabelPath.Dispose();
                PatchOverlayAnimatorPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
