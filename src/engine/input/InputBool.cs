using Godot;

/// <summary>
///   Input that just tracks if key is down or not
/// </summary>
public class InputBool : IInputReceiver
{
    private readonly string action;

    public InputBool(string actionName)
    {
        action = actionName;
    }

    /// <summary>
    ///   True when the input is down
    /// </summary>
    public bool Pressed { get; private set; }

    public virtual bool CheckInput(InputEvent inputEvent)
    {
        if (!Pressed && inputEvent.IsActionPressed(action))
        {
            Pressed = true;
            return true;
        }

        if (Pressed && inputEvent.IsActionReleased(action))
        {
            Pressed = false;
        }

        return false;
    }

    public virtual void FocusLost()
    {
        Pressed = false;
    }
}
