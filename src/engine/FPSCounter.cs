using Godot;

/// <summary>
///   Shows FPS at top left of the screen
///   Toggled with F3
/// </summary>
public class FPSCounter : Control
{
    private Label label;

    public FPSCounter()
    {
        RunOnInputAttribute.InputClasses.Add(this);
    }

    public override void _Ready()
    {
        label = GetNode<Label>("Label");
    }

    [RunOnKey("toggle_FPS", RunOnKeyAttribute.InputType.Press)]
    public void ToggleFPS()
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
