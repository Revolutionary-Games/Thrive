using Godot;

/// <summary>
///   Helper class for using an input to hold a boolean while the input is pressed
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
