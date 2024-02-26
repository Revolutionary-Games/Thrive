using Godot;
using Godot.Collections;

public partial class WindowReorderingSupportTest : Control
{
    public override void _Ready()
    {
        EnableAllWindows(GetChildren());
    }

    private void EnableAllWindows(Array children)
    {
        foreach (Node child in children)
        {
            (child as TopLevelContainer)?.Show();
            EnableAllWindows(child.GetChildren());
        }
    }
}
