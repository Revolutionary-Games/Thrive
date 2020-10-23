using Godot;

public class InputHold : InputBool
{
    public InputHold(string actionName) : base(actionName)
    {
    }

    public override bool CheckInput(InputEvent inputEvent)
    {
        return inputEvent.IsAction(action);
    }
}
