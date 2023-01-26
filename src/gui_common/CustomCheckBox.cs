using System;
using Godot;

/// <summary>
///   A customized check box that changes icon when hovered / clicked.
/// </summary>
public class CustomCheckBox : Button
{
#pragma warning disable CA2213
    private Texture unpressedNormal = null!;
    private Texture unpressedHovered = null!;
    private Texture unpressedClicked = null!;
    private Texture pressedNormal = null!;
    private Texture pressedHovered = null!;
    private Texture pressedClicked = null!;
    private Texture radioUnpressedNormal = null!;
    private Texture radioUnpressedHovered = null!;
    private Texture radioUnpressedClicked = null!;
    private Texture radioPressedNormal = null!;
    private Texture radioPressedHovered = null!;
    private Texture radioPressedClicked = null!;
#pragma warning restore CA2213

    private Color normalColor;
    private Color pressedColor;
    private Color focusColor;

    private bool pressing;
    private bool focused;
    private State currentState;

    private enum State
    {
        UnpressedNormal,
        UnpressedHovered,
        UnpressedClicked,
        PressedNormal,
        PressedHovered,
        PressedClicked,
        Disabled,
    }

    public bool Radio => Group != null;

    public override void _Ready()
    {
        unpressedNormal = GetIcon("UnpressedNormal", "CheckBox");
        unpressedHovered = GetIcon("UnpressedHovered", "CheckBox");
        unpressedClicked = GetIcon("UnpressedClicked", "CheckBox");
        pressedNormal = GetIcon("PressedNormal", "CheckBox");
        pressedHovered = GetIcon("PressedHovered", "CheckBox");
        pressedClicked = GetIcon("PressedClicked", "CheckBox");
        radioUnpressedNormal = GetIcon("RadioUnpressedNormal", "CheckBox");
        radioUnpressedHovered = GetIcon("RadioUnpressedHovered", "CheckBox");
        radioUnpressedClicked = GetIcon("RadioUnpressedClicked", "CheckBox");
        radioPressedNormal = GetIcon("RadioPressedNormal", "CheckBox");
        radioPressedHovered = GetIcon("RadioPressedHovered", "CheckBox");
        radioPressedClicked = GetIcon("RadioPressedClicked", "CheckBox");

        normalColor = GetColor("font_color");
        focusColor = GetColor("font_color_focus");
        pressedColor = GetColor("font_color_pressed");

        UpdateIcon();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what is NotificationFocusEnter or NotificationFocusExit)
        {
            focused = what == NotificationFocusEnter;

            // Update font colour based on focused state to make things more clear which box is focused (and more
            // consistent with mouse hover)
            AddColorOverride("font_color", focused ? focusColor : normalColor);
            AddColorOverride("font_color_pressed", focused ? focusColor : pressedColor);

            Update();
        }
    }

    public override void _Draw()
    {
        if (pressing && !Disabled)
        {
            currentState = Pressed ? State.PressedClicked : State.UnpressedClicked;
        }
        else
        {
            currentState = GetDrawMode() switch
            {
                DrawMode.Disabled => State.Disabled,
                DrawMode.Normal => focused ? State.UnpressedHovered : State.UnpressedNormal,
                DrawMode.Hover => State.UnpressedHovered,
                DrawMode.Pressed => focused ? State.PressedHovered : State.PressedNormal,
                DrawMode.HoverPressed => State.PressedHovered,
                _ => throw new ArgumentOutOfRangeException(GetDrawMode().ToString()),
            };
        }

        // Set icon instantly will cause Godot not to update it, and apply it at the next update
        // which will happen when there's another input event. So we queue it to be run the next frame.
        Invoke.Instance.Queue(UpdateIcon);

        base._Draw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        // Only when button's press state changes does Godot call _Pressed(), so to show a different icon when clicked,
        // we have to capture mouse event.
        if (@event is InputEventMouseButton mouseEvent)
        {
            pressing = (mouseEvent.ButtonMask & ButtonMask) != 0;
            Update();
        }

        base._GuiInput(@event);
    }

    private void UpdateIcon()
    {
        if (Radio)
        {
            Icon = currentState switch
            {
                State.Disabled => Pressed ? radioPressedNormal : radioUnpressedNormal,
                State.UnpressedNormal => radioUnpressedNormal,
                State.UnpressedHovered => radioUnpressedHovered,
                State.UnpressedClicked => radioUnpressedClicked,
                State.PressedNormal => radioPressedNormal,
                State.PressedHovered => radioPressedHovered,
                State.PressedClicked => radioPressedClicked,
                _ => throw new ArgumentOutOfRangeException(nameof(currentState)),
            };
        }
        else
        {
            Icon = currentState switch
            {
                State.Disabled => Pressed ? pressedNormal : unpressedNormal,
                State.UnpressedNormal => unpressedNormal,
                State.UnpressedHovered => unpressedHovered,
                State.UnpressedClicked => unpressedClicked,
                State.PressedNormal => pressedNormal,
                State.PressedHovered => pressedHovered,
                State.PressedClicked => pressedClicked,
                _ => throw new ArgumentOutOfRangeException(nameof(currentState)),
            };
        }
    }
}
