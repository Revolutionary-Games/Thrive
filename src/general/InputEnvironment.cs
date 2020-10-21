using Godot;

/// <summary>
///     Handles key input for an environment
/// </summary>
/// <typeparam name="T">The environment to handle</typeparam>
public abstract class InputEnvironment<T> : Node
    where T : Node
{
    protected abstract InputGroup Inputs { get; }
    protected T Environment { get; private set; }

    public override void _Ready()
    {
        Environment = (T)GetParent();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Inputs.CheckInput(@event))
        {
            GetTree().SetInputAsHandled();
        }
    }

    public override void _Notification(int focus)
    {
        // If the window goes out of focus, we don't receive the key released events
        // We reset our held down keys if the player tabs out while pressing a key
        if (focus == MainLoop.NotificationWmFocusOut)
        {
            Inputs.FocusLost();
        }
    }

    public override void _Process(float delta)
    {
        Inputs.OnFrameChanged();
    }
}
