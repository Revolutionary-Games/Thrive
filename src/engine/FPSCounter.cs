using Godot;

/// <summary>
///   Shows FPS at top left of the screen
///   Toggled with F3
/// </summary>
public class FPSCounter : ControlWithInput
{
    private Label label;

    public override void _Ready()
    {
        label = GetNode<Label>("Label");
    }

    [RunOnKeyToggle("toggle_FPS", OnlyUnhandled = false)]
    public void ToggleFps(bool state)
    {
        Visible = state;
    }

    public override void _Process(float delta)
    {
        if (Visible)
            label.Text = $"FPS: {Engine.GetFramesPerSecond()}";
    }
}
