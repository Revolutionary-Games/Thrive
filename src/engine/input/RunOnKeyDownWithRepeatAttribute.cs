﻿using Godot;

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
        if (@event.IsActionPressed(InputName, true))
        {
            Prime();
            if (SetKeyDown)
                HeldDown = true;

            return CallMethod();
        }

        return false;
    }

    public override void OnProcess(float delta)
    {
    }
}
