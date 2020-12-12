using Godot;

/// <summary>
///   Input class for a <see cref="Godot.Control"/>
/// </summary>
public class InputControl : Control
{
    public override void _EnterTree()
    {
        InputManager.RegisterReceiver(this);
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        InputManager.UnregisterReceiver(this);
        base._ExitTree();
    }
}
