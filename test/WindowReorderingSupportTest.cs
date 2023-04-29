using Godot;
using Godot.Collections;

public class WindowReorderingSupportTest : Control
{
    public override void _Ready()
    {
        EnableAllWindows(GetChildren());
    }

    private void EnableAllWindows(Array children)
    {
        foreach (var child in children)
        {
            (child as CustomWindow)?.Show();
            EnableAllWindows((child as Node)!.GetChildren());
        }
    }
}
