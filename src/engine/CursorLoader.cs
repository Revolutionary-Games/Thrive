using Godot;

/// <summary>
///   Replaces default cursor shapes with custom made
/// </summary>
public class CursorLoader : Node
{
    private Resource hoverCursor = GD.Load<Resource>("res://assets/textures/gui/cursors/cursor_hover.png");
    public override void _Ready()
    {
        Input.SetCustomMouseCursor(hoverCursor, Input.CursorShape.PointingHand);
    }
}
