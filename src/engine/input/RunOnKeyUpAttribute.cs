using Godot;

/// <summary>
///   Attribute for a method, that gets called when the defined key is released.
///   Can be applied multiple times.
/// </summary>
public class RunOnKeyUpAttribute : RunOnKeyAttribute
{
    public RunOnKeyUpAttribute(string inputName) : base(inputName)
    {
    }

    protected override bool CallMethodInOnInput => false;

    public override bool OnInput(InputEvent @event)
    {
        var before = HeldDown;
        if (base.OnInput(@event) && before && !HeldDown)
        {
            // Base doesn't react to key up triggering the input method type change
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
