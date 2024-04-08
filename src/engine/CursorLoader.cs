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
#pragma warning restore CA2213

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;

        hoverCursor = GD.Load<Resource>("res://assets/textures/gui/cursors/cursor_hover.png");
        Input.SetCustomMouseCursor(hoverCursor, Input.CursorShape.PointingHand);
    }
}
