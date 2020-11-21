using System;
using Godot;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RunOnKeyAttribute : InputAttribute
{
    private bool primed;

    public RunOnKeyAttribute(string godotInputName)
    {
        GodotInputName = godotInputName;
    }

    public bool HeldDown { get; private set; }
    private string GodotInputName { get; }

    public bool ReadHeldOrPrimedAndResetPrimed()
    {
        var result = HeldDown || primed;
        primed = false;
        return result;
    }

    public override bool OnInput(InputEvent @event)
    {
        if (@event.IsActionPressed(GodotInputName))
        {
            primed = true;
            HeldDown = true;
        }

        if (@event.IsActionReleased(GodotInputName))
            HeldDown = false;

        return HeldDown;
    }

    public override void OnProcess(float delta)
    {
        if (HeldDown || primed)
        {
            primed = false;
            CallMethod(delta);
        }
    }

    public override void FocusLost()
    {
        HeldDown = false;
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj) || !(obj is RunOnKeyAttribute key))
            return false;

        return string.Equals(GodotInputName, key.GodotInputName, StringComparison.InvariantCulture);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (base.GetHashCode() * 397) ^ GodotInputName.GetHashCode();
        }
    }
}
