using System;
using Godot;

/// <summary>
///   Attribute for a method, that gets repeatedly called when the defined key is pressed.
///   Can be applied multiple times.
/// </summary>
/// <example>
///   [RunOnKey("g_cheat_glucose")]
///   public void CheatGlucose(float delta)
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RunOnKeyAttribute : InputAttribute
{
    /// <summary>
    ///   Priming comes to effect when an input gets pressed for less than one frame
    ///   (when the release input gets detected before OnProcess could be called)
    /// </summary>
    private bool primed;

    public RunOnKeyAttribute(string inputName)
    {
        InputName = inputName;
    }

    /// <summary>
    ///   Whether the key is currently held down or not
    /// </summary>
    public bool HeldDown { get; private set; }

    /// <summary>
    ///   The internal godot input name
    /// </summary>
    /// <example>ui_select</example>
    private string InputName { get; }

    /// <summary>
    ///   Reads the current prime or held state and resets the primed state
    /// </summary>
    /// <returns>True when the key is held down or primed</returns>
    public bool ReadHeldOrPrimedAndResetPrimed()
    {
        var result = HeldDown || primed;
        primed = false;
        return result;
    }

    public override bool OnInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputName))
        {
            primed = true;
            HeldDown = true;
        }

        if (@event.IsActionReleased(InputName))
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

        return string.Equals(InputName, key.InputName, StringComparison.InvariantCulture);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (base.GetHashCode() * 397) ^ InputName.GetHashCode();
        }
    }
}
