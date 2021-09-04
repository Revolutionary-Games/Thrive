using System;
using Godot;

/// <summary>
///   A customized check box that changes icon when hovered / clicked.
/// </summary>
public class CustomCheckBox : Button
{
    private Texture unpressedNormal;
    private Texture unpressedHovered;
    private Texture unpressedClicked;
    private Texture pressedNormal;
    private Texture pressedHovered;
    private Texture pressedClicked;

    private bool pressing;

    public override void _Ready()
    {
        unpressedNormal = GetIcon("UnpressedNormal", "CheckBox");
        unpressedHovered = GetIcon("UnpressedHovered", "CheckBox");
        unpressedClicked = GetIcon("UnpressedClicked", "CheckBox");
        pressedNormal = GetIcon("PressedNormal", "CheckBox");
        pressedHovered = GetIcon("PressedHovered", "CheckBox");
        pressedClicked = GetIcon("PressedClicked", "CheckBox");
    }

    public override void _Process(float delta)
    {
        if (!Visible)
            return;

        UpdateIcon();
        base._Process(delta);
    }

    public override void _GuiInput(InputEvent @event)
    {
        // Only when button's press state changes does Godot call _Pressed(), so to show a different icon when clicked,
        // we have to capture mouse event.
        if (@event is InputEventMouseButton { ButtonIndex: (int)ButtonList.Left } mouseEvent)
        {
            pressing = mouseEvent.Pressed;
        }

        base._GuiInput(@event);
    }

    private void UpdateIcon()
    {
        if (pressing && ActionMode == ActionModeEnum.Release)
        {
            Icon = Pressed ? pressedClicked : unpressedClicked;
            return;
        }

        Icon = GetDrawMode() switch
        {
            DrawMode.Disabled => Pressed ? pressedNormal : unpressedNormal,
            DrawMode.Normal => unpressedNormal,
            DrawMode.Hover => unpressedHovered,
            DrawMode.Pressed => pressedNormal,
            DrawMode.HoverPressed => pressedHovered,
            _ => throw new NotImplementedException(),
        };
    }
}
