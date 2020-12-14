using Godot;

/// <summary>
///   Shows FPS at top left of the screen
///   Toggled with F3
/// </summary>
public class FPSCounter : Control
{
    private Label label;

    public override void _Ready()
    {
        label = GetNode<Label>("Label");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_FPS"))
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
    }

    public override void _Process(float delta)
    {
        if (Visible)
            label.Text = $"FPS: {Engine.GetFramesPerSecond()}";
    }
}
