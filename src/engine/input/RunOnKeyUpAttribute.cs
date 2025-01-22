using Godot;

/// <summary>
///   Attribute for a method, that gets called when the defined key is released. Can be applied multiple times.
///   Note that this is always run, even for otherwise consumed input. So care must be taken when writing any code
///   using this attribute.
/// </summary>
/// <remarks>
///   <para>
///     This should mostly be used to react as an additional step to input by using another input attribute as the
///     "trigger" that sets this up to do something. That way the problem with this always triggering (even on consumed
///     input) can be avoided.
///   </para>
/// </remarks>
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

    public override void OnProcess(double delta)
    {
    }
}
