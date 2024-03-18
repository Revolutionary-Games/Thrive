using Godot;

/// <summary>
///   Button for entering editor in creature stages
/// </summary>
public partial class EditorEntryButton : TextureButton
{
    [Export]
    private TextureRect highlight = null!;

    [Export]
    private AnimationPlayer buttonAnimationPlayer = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (highlight != null)
            {
                highlight.Dispose();
                buttonAnimationPlayer.Dispose();
            }
        }
    }

    private void OnEditorButtonMouseEnter()
    {
        if (Disabled)
            return;

        highlight.Hide();
        buttonAnimationPlayer.Stop();
    }

    private void OnEditorButtonMouseExit()
    {
        if (Disabled)
            return;

        highlight.Show();
        buttonAnimationPlayer.Play();
    }
}
