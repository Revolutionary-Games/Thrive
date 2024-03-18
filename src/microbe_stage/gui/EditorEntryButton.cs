using Godot;

/// <summary>
///   Button for entering editor in creature stages
/// </summary>
public partial class EditorEntryButton : TextureButton
{
    [Export]
    public TextureRect Highlight = null!;

    [Export]
    public AnimationPlayer ButtonAnimationPlayer = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Highlight != null)
            {
                Highlight.Dispose();
                ButtonAnimationPlayer.Dispose();
            }
        }
    }

    private void OnEditorButtonMouseEnter()
    {
        if (Disabled)
            return;

        Highlight.Hide();
        ButtonAnimationPlayer.Stop();
    }

    private void OnEditorButtonMouseExit()
    {
        if (Disabled)
            return;

        Highlight.Show();
        ButtonAnimationPlayer.Play();
    }
}
