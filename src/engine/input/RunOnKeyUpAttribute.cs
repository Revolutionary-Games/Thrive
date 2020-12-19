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

    public override bool OnInput(InputEvent @event)
    {
        if (base.OnInput(@event) && !HeldDown)
        {
            return CallMethod();
        }

        return false;
    }

    public override void OnProcess(float delta)
    {
    }
}
