using Godot;

public class RunOnKeyUpAttribute : RunOnKeyAttribute
{
    public RunOnKeyUpAttribute(string godotInputName) : base(godotInputName)
    {
    }

    public override bool OnInput(InputEvent @event)
    {
        var before = HeldDown;
        if (!base.OnInput(@event) && before)
        {
            return CallMethod();
        }

        return false;
    }

    public override void OnProcess(float delta)
    {
    }
}
