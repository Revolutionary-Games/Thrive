using Godot;

/// <summary>
///   Places the ScreenFilter scene after the main scene in the root
///   Filter must be after the main scene to take any effect
/// </summary>
public class FilterLoader : Node
{
    private Node textureFilter;

    public override void _Ready()
    {
        textureFilter = GetTree().Root.GetNode("ScreenFilter");
    }

    public override void _Process(float delta)
    {
        if (textureFilter != GetTree().Root.GetChild(GetTree().Root.GetChildCount() - 1))
            GetTree().Root.MoveChild(textureFilter, GetTree().Root.GetChildCount() - 1);
    }
}
