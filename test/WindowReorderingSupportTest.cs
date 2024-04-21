using Godot;
using Godot.Collections;

/// <summary>
///   Test for our custom window reordering
/// </summary>
public partial class WindowReorderingSupportTest : Control
{
    public override void _Ready()
    {
        EnableAllWindows(GetChildren());
    }

    private void EnableAllWindows(Array<Node> children)
    {
        foreach (Node child in children)
        {
            (child as TopLevelContainer)?.Show();
            EnableAllWindows(child.GetChildren());
        }
    }
}
