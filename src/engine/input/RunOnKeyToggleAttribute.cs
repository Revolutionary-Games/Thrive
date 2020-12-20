using System;
using Godot;

public class RunOnKeyToggleAttribute : RunOnKeyAttribute
{
    public RunOnKeyToggleAttribute(string inputName) : base(inputName)
    {
    }

    public bool ToggleState { get; set; }

    /// <summary>
    ///   Not supported in RunOnKeyToggle
    /// </summary>
    public override bool CallMethodWithZeroOnInput
    {
        get => false;
        set => throw new NotSupportedException("Only RunOnKey supports setting CallMethodWithZeroOnInput");
    }

    public override bool OnInput(InputEvent @event)
    {
        var before = HeldDown;
        if (base.OnInput(@event) && !before && HeldDown)
        {
            ToggleState = !ToggleState;
            return CallMethod(ToggleState);
        }

        return false;
    }

    public override void OnProcess(float delta)
    {
    }
}
