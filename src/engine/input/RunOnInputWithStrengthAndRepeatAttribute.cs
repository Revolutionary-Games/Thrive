﻿using Godot;

/// <summary>
///   A combination of <see cref="RunOnInputWithStrengthAttribute"/> and <see cref="RunOnKeyDownWithRepeatAttribute"/>
/// </summary>
public class RunOnInputWithStrengthAndRepeatAttribute : RunOnInputWithStrengthAttribute
{
    public RunOnInputWithStrengthAndRepeatAttribute(string inputName) : base(inputName)
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

            // TODO: this isn't verified to work fine with mouse or controller inputs
            Strength = @event.GetActionStrength(InputName, false);

            return CallMethod(Strength);
        }

        return false;
    }

    public override void OnProcess(float delta)
    {
        // It's probably faster to just set this to always zero here than spend another Godot call on checking if
        // the action was released (and we do this in process to have this down for at least a bit)
        Strength = 0;
        HeldDown = false;
    }
}
