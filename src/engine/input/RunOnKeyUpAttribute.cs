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

        return !base.OnInput(@event);
    }

    public override void OnProcess(float delta)
    {
    }
}
