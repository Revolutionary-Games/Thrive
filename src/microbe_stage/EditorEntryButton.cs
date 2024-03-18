using Godot;

/// <summary>
///   Button for entering editor in creature stages
/// </summary>
public partial class EditorEntryButton : TextureButton
{
    private void OnEditorButtonMouseEnter()
    {
        if (Disabled)
            return;

        GetNode<TextureRect>("Highlight").Hide();
        GetNode<AnimationPlayer>("AnimationPlayer").Stop();
    }

    private void OnEditorButtonMouseExit()
    {
        if (Disabled)
            return;

        GetNode<TextureRect>("Highlight").Show();
        GetNode<AnimationPlayer>("AnimationPlayer").Play();
    }
}
