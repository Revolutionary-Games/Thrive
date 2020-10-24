using Godot;

/// <summary>
///   Input that is toggled on key press
/// </summary>
public class InputHoldToggle : InputBool
{
    public InputHoldToggle(string actionName) : base(actionName)
    {
    }

    /// <summary>
    ///   True when the input has been toggled on. Note that you shouldn't check Pressed with this input type
    /// </summary>
    private bool ToggledOn { get; set; }

    public override bool CheckInput(InputEvent inputEvent)
    {
        var wasPressed = Pressed;

        if (!base.CheckInput(inputEvent))
            return false;

        if (!wasPressed && Pressed)
        {
            // Just became pressed, toggle this
            ToggledOn = !ToggledOn;
        }

        return true;
    }

    public override object ReadInput() => ToggledOn;

    public override bool HasInput() => ToggledOn;

    // This doesn't actually reset the on status, but will reset detection for being pressed currently
    // public override void FocusLost()
}
