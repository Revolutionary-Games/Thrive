using Godot;

/// <summary>
///   Attribute for a method, that gets called when the defined key is pressed down.
///   And also when the key repeats
/// </summary>
/// <example>
///   [RunOnKeyDown("zoom_out")]
///   public void ZoomOutPressed()
/// </example>
public class RunOnKeyDownWithRepeatAttribute : RunOnKeyAttribute
{
    public RunOnKeyDownWithRepeatAttribute(string inputName) : base(inputName)
    {
    }

    /// <summary>
    ///   If false, doesn't set key down state
    /// </summary>
    public bool SetKeyDown { get; set; }

    protected override bool CallMethodInOnInput => false;

    public override bool OnInput(InputEvent @event)
    {
        // Check key or echo from key being down
        if (@event.IsActionPressed(InputName, true, false))
        {
            Prime();
            if (SetKeyDown)
                HeldDown = true;

            if (TrackInputMethod)
            {
                LastUsedInputMethod = InputManager.InputMethodFromInput(@event);
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

    public override void OnProcess(float delta)
    {
    }
}
