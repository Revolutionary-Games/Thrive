using Godot;

/// <summary>
///   Input that is toggled on key press
/// </summary>
public class InputToggle : InputBool
{
    public InputToggle(string actionName) : base(actionName)
    {
    }

    /// <summary>
    ///   True when the input has been toggled on. Note that you shouldn't check Pressed with this input type
    /// </summary>
    public bool ToggledOn { get; set; }

    public override bool CheckInput(InputEvent inputEvent)
    {
        bool wasPressed = Pressed;

        if (base.CheckInput(inputEvent))
        {
            if (!wasPressed && Pressed)
            {
                // Just became pressed, toggle this
                ToggledOn = !ToggledOn;
            }

            return true;
        }

        return false;
    }

    // This doesn't actually reset the on status, but will reset detection for being pressed currently
    // public override void FocusLost()
}
