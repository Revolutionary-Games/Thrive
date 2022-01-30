using Godot;

/// <summary>
///   Replaces default cursor shapes with custom made
///   Default cursor is set in Project Settings
/// </summary>
public class CursorLoader : Node
{
    private Resource? hoverCursor;

    public override void _Ready()
    {
        hoverCursor = GD.Load<Resource>("res://assets/textures/gui/cursors/cursor_hover.png");
        Input.SetCustomMouseCursor(hoverCursor, Input.CursorShape.PointingHand);
    }
}
