using Godot;

public class RunOnKeyChangeAttribute : RunOnKeyAttribute
{
    public RunOnKeyChangeAttribute(string godotInputName) : base(godotInputName)
    {
    }

    public override bool OnInput(InputEvent @event)
    {
        var before = HeldDown;
        return base.OnInput(@event) != before;
    }

    public override void OnProcess(float delta)
    {
    }
}
