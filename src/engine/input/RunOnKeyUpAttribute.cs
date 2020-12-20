using System;
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

    /// <summary>
    ///   Not supported in RunOnKeyUp.
    /// </summary>
    public override bool CallMethodWithZeroOnInput
    {
        get => false;
        set => throw new NotSupportedException("Only RunOnKey supports setting CallMethodWithZeroOnInput");
    }

    public override bool OnInput(InputEvent @event)
    {
        var before = HeldDown;
        if (base.OnInput(@event) && before && !HeldDown)
        {
            return CallMethod();
        }

        return false;
    }

    public override void OnProcess(float delta)
    {
    }
}
