using Godot;

/// <summary>
///   Is like a <see cref="Godot.Control"/>, but handles instance management for the input system.
/// </summary>
public class ControlWithInput : Control
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
