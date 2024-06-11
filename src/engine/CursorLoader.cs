using Godot;

/// <summary>
///   Replaces default cursor shapes with custom made. Default cursor is set in Project Settings (this sets other
///   cursor variants not possible to set in project settings)
/// </summary>
[GodotAutoload]
public partial class CursorLoader : Node
{
#pragma warning disable CA2213
    private Resource? hoverCursor;
    private Resource? canDropCursor;
    private Resource? noDropCursor;
#pragma warning restore CA2213
    private Vector2 iconHotspot;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;

        iconHotspot = new Vector2(24, 24);

        hoverCursor = GD.Load<Resource>("res://assets/textures/gui/cursors/cursor_hover.png");
        Input.SetCustomMouseCursor(hoverCursor, Input.CursorShape.PointingHand);
        canDropCursor = GD.Load<Resource>("res://assets/textures/gui/cursors/cursor_hand_drop.png");
        Input.SetCustomMouseCursor(canDropCursor, Input.CursorShape.CanDrop, new Vector2(24, 24));
        noDropCursor = GD.Load<Resource>("res://assets/textures/gui/cursors/cursor_hand.png");
        Input.SetCustomMouseCursor(noDropCursor, Input.CursorShape.Forbidden, new Vector2(24, 24));
    }
}
