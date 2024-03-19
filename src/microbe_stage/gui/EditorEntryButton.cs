using Godot;

/// <summary>
///   Button for entering editor in creature stages
/// </summary>
public partial class EditorEntryButton : TextureButton
{
#pragma warning disable CA2213
    [Export]
    private TextureRect highlight = null!;

    [Export]
    private AnimationPlayer buttonAnimationPlayer = null!;
#pragma warning restore CA2213

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
