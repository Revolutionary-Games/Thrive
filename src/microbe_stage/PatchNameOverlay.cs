using Godot;

/// <summary>
///   Shows the current patch name on screen briefly before fading out
/// </summary>
public partial class PatchNameOverlay : PanelContainer
{
#pragma warning disable CA2213
    [Export]
    private Label patchLabel = null!;
    [Export]
    private AnimationPlayer patchOverlayAnimator = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
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
            {
            }
        }

        base.Dispose(disposing);
    }
}
