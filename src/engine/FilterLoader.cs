using Godot;

/// <summary>
///   Places the ScreenFilter scene after the main scene in the root
///   Filter must be after the main scene to take any effect
///   Places the FPS counter in front of the filter
/// </summary>
public class FilterLoader : Node
{
    private Node textureFilter;
    private Node fpsCounter;

    public override void _Ready()
    {
        textureFilter = GetTree().Root.GetNode("ScreenFilter");
        fpsCounter = GetTree().Root.GetNode("FPSCounter");
    }

    public override void _Process(float delta)
    {
        if (textureFilter != GetTree().Root.GetChild(GetTree().Root.GetChildCount() - 2))
        {
            GetTree().Root.MoveChild(textureFilter, GetTree().Root.GetChildCount() - 2);
            GetTree().Root.MoveChild(fpsCounter, GetTree().Root.GetChildCount() - 1);
        }
    }
}
