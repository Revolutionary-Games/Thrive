using Godot;

public class RunOnKeyUpAttribute : RunOnKeyAttribute
{
    public RunOnKeyUpAttribute(string godotInputName) : base(godotInputName)
    {
    }

    public override bool OnInput(InputEvent @event)
    {
        var before = HeldDown;
        if (!before)
            return false;

        if (base.OnInput(@event))
            return false;

        CallMethod();
        return true;
    }

    public override void OnProcess(float delta)
    {
    }
}
