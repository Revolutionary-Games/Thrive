using Godot;

/// <summary>
///   Attribute for a method, that gets called when the defined key is pressed or released.
///   Can be applied multiple times.
/// </summary>
public class RunOnKeyChangeAttribute : RunOnKeyAttribute
{
    public RunOnKeyChangeAttribute(string inputName) : base(inputName)
    {
    }

    protected override bool CallMethodInOnInput => false;

    public override bool OnInput(InputEvent @event)
    {
        var before = HeldDown;
        if (!base.OnInput(@event) || HeldDown == before)
            return false;

        // Base doesn't react to key up triggering the input method type change, so we do our own extra check here
        if (TrackInputMethod)
        {
            LastUsedInputMethod = InputManager.InputMethodFromInput(@event);
            PrepareMethodParameters(ref cachedMethodCallParameters, 2, HeldDown);
            cachedMethodCallParameters![1] = LastUsedInputMethod;
        }
        else
        {
            PrepareMethodParameters(ref cachedMethodCallParameters, 1, HeldDown);
        }

        return CallMethod(cachedMethodCallParameters!);
    }

    public override void OnProcess(double delta)
    {
    }
}
