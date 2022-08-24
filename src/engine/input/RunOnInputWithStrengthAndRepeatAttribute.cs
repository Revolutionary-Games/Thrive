using Godot;

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
        if (@event.IsActionPressed(InputName, true))
        {
            Prime();
            if (SetKeyDown)
                HeldDown = true;

            Strength = @event.GetActionStrength(InputName);

            return CallMethod(Strength);
        }

        // It's probably faster to just set this to always zero here than spend another Godot call on checking if
        // the action was released
        Strength = 0;

        return false;
    }

    public override void OnProcess(float delta)
    {
    }
}
