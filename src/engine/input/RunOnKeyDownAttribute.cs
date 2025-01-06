using Godot;

/// <summary>
///   Attribute for a method, that gets called when the defined key is pressed down.
///   Can be applied multiple times.
/// </summary>
/// <example>
///   [RunOnKeyDown("screenshot")]
///   public void TakeScreenshotPressed()
/// </example>
public class RunOnKeyDownAttribute : RunOnKeyAttribute
{
    public RunOnKeyDownAttribute(string inputName) : base(inputName)
    {
    }

    protected override bool CallMethodInOnInput => false;

    public override bool OnInput(InputEvent @event)
    {
        // Only trigger if the input was pressed this frame. It isn't possible to use !HeldDown since its value is
        // incorrect if a mouse release event is marked as handled e.g. when opening a popup.
        // https://github.com/Revolutionary-Games/Thrive/issues/5217
        var justPressed = Input.IsActionJustPressed(InputName);
        if (base.OnInput(@event) && justPressed && HeldDown)
        {
            if (TrackInputMethod)
            {
                PrepareMethodParameters(ref cachedMethodCallParameters, 1, LastUsedInputMethod);
            }
            else
            {
                PrepareMethodParametersEmpty(ref cachedMethodCallParameters);
            }

            return CallMethod(cachedMethodCallParameters!);
        }

        return false;
    }

    public override void OnProcess(double delta)
    {
    }
}
