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
        var before = HeldDown;
        if (base.OnInput(@event) && !before && HeldDown)
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

    public override void OnProcess(float delta)
    {
    }
}
