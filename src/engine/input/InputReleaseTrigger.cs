using Godot;

/// <summary>
///   Input that is moved to triggered state on release
/// </summary>
public class InputReleaseTrigger : InputBool
{
    private bool triggered;

    public InputReleaseTrigger(string actionName) : base(actionName)
    {
    }

    public override object GetValueForCallback()
    {
        return ReadTrigger();
    }

    public override bool CheckInput(InputEvent inputEvent)
    {
        var wasPressed = Pressed;

        if (base.CheckInput(inputEvent))
            return false;

        if (wasPressed && !Pressed)
        {
            // Just became pressed, trigger
            triggered = true;
        }

        return true;
    }

    public override bool ShouldTriggerCallbacks()
    {
        return (bool)GetValueForCallback();
    }

    /// <summary>
    ///   Reads the trigger status and resets it
    /// </summary>
    /// <returns>True if has been triggered</returns>
    private bool ReadTrigger()
    {
        if (!triggered)
            return false;

        triggered = false;
        return true;
    }

    // This doesn't actually reset the trigger status, but will reset detection for being pressed currently
    // public override void FocusLost()
}
