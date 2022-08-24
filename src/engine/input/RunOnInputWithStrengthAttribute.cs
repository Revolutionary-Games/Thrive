using Godot;

/// <summary>
///   A variant of running on an action, but this variant also detects the "strength" of the action. Passes extra
///   parameter of the strength of the action as a float.
/// </summary>
/// <remarks>
///   <para>
///     The strength of the action for mouse axes is how far the mouse was moved, and for controllers it is how far
///     along the stick is pushed.
///   </para>
/// </remarks>
public class RunOnInputWithStrengthAttribute : RunOnKeyAttribute
{
    public RunOnInputWithStrengthAttribute(string inputName) : base(inputName)
    {
    }

    // TODO: we need some kind of strength scaling for controller input

    /// <summary>
    ///   Strength of the action. Range is [0, Inf[ for mouse input. For controller input it is [0, 1].
    /// </summary>
    public float Strength { get; protected set; }

    protected override bool CallMethodInOnInput => false;

    public override bool OnInput(InputEvent @event)
    {
        var baseResult = base.OnInput(@event);
        if (baseResult)
        {
            if (HeldDown)
            {
                Strength = @event.GetActionStrength(InputName);
            }
            else
            {
                Strength = 0;
            }
        }

        return baseResult;
    }

    public override void OnProcess(float delta)
    {
        if (ReadHeldOrPrimedAndResetPrimed())
        {
            CallMethod(delta, Strength);
        }
    }

    public override void FocusLost()
    {
        base.FocusLost();
        Strength = 0;
    }
}
