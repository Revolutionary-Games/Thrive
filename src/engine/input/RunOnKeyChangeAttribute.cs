using Godot;

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

        CallMethod();
        return true;
    }

    public override void OnProcess(float delta)
    {
    }
}
