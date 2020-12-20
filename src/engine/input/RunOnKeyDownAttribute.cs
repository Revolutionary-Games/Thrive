using System;
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

    /// <summary>
    ///   Not supported in RunOnKeyDown
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
            return CallMethod();
        }

        return false;
    }

    public override void OnProcess(float delta)
    {
    }
}
