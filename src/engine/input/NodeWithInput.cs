using Godot;

/// <summary>
///   Is like a <see cref="Godot.Node"/>, but handles instance management for the input system.
/// </summary>
public partial class NodeWithInput : Node
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
