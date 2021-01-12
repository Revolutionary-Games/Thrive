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
    /// <remarks>
    ///   <para>
    ///     Also used by <see cref="RunOnKeyDownWithRepeatAttribute"/> to report to axis that it was down or repeated.
    ///   </para>
    /// </remarks>
    private bool primed;

    public RunOnKeyAttribute(string inputName)
    {
        InputName = inputName;
    }

    /// <summary>
    ///   Whether the key is currently held down or not
    /// </summary>
    public bool HeldDown { get; protected set; }

    /// <summary>
    ///   The internal godot input name
    /// </summary>
    /// <example>ui_select</example>
    public string InputName { get; }

    /// <summary>
    ///   If this is set to false the callback method is allowed to be called without the delta value (using 0.0f)
    /// </summary>
    public bool CallbackRequiresElapsedTime { get; set; } = true;

    /// <summary>
    ///   Should OnInput run the callback method instantly
    /// </summary>
    protected virtual bool CallMethodInOnInput => true;

    /// <summary>
    ///   Reads the current primed or held state and resets the primed state
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
        bool result = false;

        if (@event.IsActionPressed(InputName))
        {
            if (CallMethodInOnInput && !CallbackRequiresElapsedTime)
            {
                result = CallMethod(0.0f);
            }
            else
            {
                result = true;
            }

            Prime();
            HeldDown = true;
        }

        if (@event.IsActionReleased(InputName))
        {
            result = true;
            HeldDown = false;
        }

        return result;
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

    protected void Prime()
    {
        primed = true;
    }
}
