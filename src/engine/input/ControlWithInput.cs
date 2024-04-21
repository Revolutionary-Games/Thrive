using Godot;

/// <summary>
///   Is like a <see cref="Godot.Control"/>, but handles instance management for the input system.
/// </summary>
public partial class ControlWithInput : Control
{
    public override void _EnterTree()
    {
        base._EnterTree();

        InputManager.RegisterReceiver(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        InputManager.UnregisterReceiver(this);
    }
}
