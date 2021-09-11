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
    private CheckState lastCheckState;
    private CheckState currentCheckState;

    private enum CheckState
    {
        UnpressedNormal,
        UnpressedHovered,
        UnpressedClicked,
        PressedNormal,
        PressedHovered,
        PressedClicked,
        Disabled,
    }

    public override void _Ready()
    {
        unpressedNormal = GetIcon("UnpressedNormal", "CheckBox");
        unpressedHovered = GetIcon("UnpressedHovered", "CheckBox");
        unpressedClicked = GetIcon("UnpressedClicked", "CheckBox");
        pressedNormal = GetIcon("PressedNormal", "CheckBox");
        pressedHovered = GetIcon("PressedHovered", "CheckBox");
        pressedClicked = GetIcon("PressedClicked", "CheckBox");
        UpdateIcon();
    }

    public override void _Process(float delta)
    {
        if (!Visible)
            return;

        if (pressing)
        {
            currentCheckState = Pressed ? CheckState.PressedClicked : CheckState.UnpressedClicked;
        }
        else
        {
            currentCheckState = GetDrawMode() switch
            {
                DrawMode.Disabled => CheckState.Disabled,
                DrawMode.Normal => CheckState.UnpressedNormal,
                DrawMode.Hover => CheckState.UnpressedHovered,
                DrawMode.Pressed => CheckState.PressedNormal,
                DrawMode.HoverPressed => CheckState.PressedHovered,
                _ => throw new ArgumentOutOfRangeException(GetDrawMode().ToString()),
            };
        }

        if (lastCheckState == currentCheckState)
            return;

        UpdateIcon();

        lastCheckState = currentCheckState;

        base._Process(delta);
    }

    public override void _GuiInput(InputEvent @event)
    {
        // Only when button's press state changes does Godot call _Pressed(), so to show a different icon when clicked,
        // we have to capture mouse event.
        if (@event is InputEventMouseButton mouseEvent)
            pressing = (mouseEvent.ButtonMask & ButtonMask) != 0;

        base._GuiInput(@event);
    }

    private void UpdateIcon()
    {
        Icon = currentCheckState switch
        {
            CheckState.Disabled => Pressed ? pressedNormal : unpressedNormal,
            CheckState.UnpressedNormal => unpressedNormal,
            CheckState.UnpressedHovered => unpressedHovered,
            CheckState.UnpressedClicked => unpressedClicked,
            CheckState.PressedNormal => pressedNormal,
            CheckState.PressedHovered => pressedHovered,
            CheckState.PressedClicked => pressedClicked,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
