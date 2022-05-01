using Godot;

/// <summary>
///   Shows FPS at top left of the screen. Toggled with F3 (default keybinding)
/// </summary>
public class FPSCounter : Control
{
    private Label label = null!;

    public override void _Ready()
    {
        label = GetNode<Label>("Label");
    }

    public override void _Process(float delta)
    {
        if (Visible)
            label.Text = new LocalizedString("FPS", Engine.GetFramesPerSecond()).ToString();
    }

    public void ToggleFps(bool state)
    {
        Visible = state;
    }
}
