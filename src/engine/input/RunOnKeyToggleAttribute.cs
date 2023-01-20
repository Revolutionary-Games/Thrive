using Godot;

public class RunOnKeyToggleAttribute : RunOnKeyAttribute
{
    public RunOnKeyToggleAttribute(string inputName) : base(inputName)
    {
    }

    public bool ToggleState { get; set; }

    protected override bool CallMethodInOnInput => false;

    public override bool OnInput(InputEvent @event)
    {
        var before = HeldDown;
        if (base.OnInput(@event) && !before && HeldDown)
        {
            ToggleState = !ToggleState;

            // Base doesn't react to key up triggering the input method type change, so we do our own extra check here
            if (TrackInputMethod)
            {
                LastUsedInputMethod = InputManager.InputMethodFromInput(@event);
                return CallMethod(ToggleState, LastUsedInputMethod);
            }

            return CallMethod(ToggleState);
        }

        return false;
    }

    public override void OnProcess(float delta)
    {
    }
}
