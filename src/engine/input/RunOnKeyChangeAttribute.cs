using Godot;

/// <summary>
///   Attribute for a method, that gets called when the defined key is pressed or released.
/// </summary>
public class RunOnKeyChangeAttribute : RunOnKeyAttribute
{
    public RunOnKeyChangeAttribute(string godotInputName) : base(godotInputName)
    {
    }

    public override bool OnInput(InputEvent @event)
    {
        var before = HeldDown;
        if (base.OnInput(@event) == before)
            return false;

        return CallMethod();
    }

    public override void OnProcess(float delta)
    {
    }
}
