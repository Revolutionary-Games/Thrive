using Godot;

/// <summary>
///   Shows FPS at top left of the screen
///   Toggled with F3
/// </summary>
public class FPSCounter : InputControl
{
    private Label label;

    public override void _Ready()
    {
        label = GetNode<Label>("Label");
    }

    [RunOnKeyDown("toggle_FPS")]
    public void ToggleFps()
    {
        if (Visible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public override void _Process(float delta)
    {
        if (Visible)
            label.Text = $"FPS: {Engine.GetFramesPerSecond()}";
    }
}
