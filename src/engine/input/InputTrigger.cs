using Godot;

/// <summary>
///   Input that is moved to triggered state on press
/// </summary>
public class InputTrigger : InputBool
{
    private bool triggered;

    public InputTrigger(string actionName) : base(actionName)
    {
    }

    /// <summary>
    ///   Reads the trigger status and resets it
    /// </summary>
    /// <returns>True if has been triggered</returns>
    public bool ReadTrigger()
    {
        if (triggered)
        {
            triggered = false;
            return true;
        }

        return false;
    }

    public override bool CheckInput(InputEvent inputEvent)
    {
        bool wasPressed = Pressed;

        if (base.CheckInput(inputEvent))
        {
            if (!wasPressed && Pressed)
            {
                // Just became pressed, trigger
                triggered = true;
            }

            return true;
        }

        return false;
    }

    // This doesn't actually reset the trigger status, but will reset detection for being pressed currently
    // public override void FocusLost()
}
