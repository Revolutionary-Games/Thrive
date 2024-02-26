using System;
using Godot;

/// <summary>
///   A customized check box that changes icon when hovered / clicked.
/// </summary>
public partial class CustomCheckBox : Button
{
#pragma warning disable CA2213
    private Texture2D unpressedNormal = null!;
    private Texture2D unpressedHovered = null!;
    private Texture2D unpressedClicked = null!;
    private Texture2D pressedNormal = null!;
    private Texture2D pressedHovered = null!;
    private Texture2D pressedClicked = null!;
    private Texture2D radioUnpressedNormal = null!;
    private Texture2D radioUnpressedHovered = null!;
    private Texture2D radioUnpressedClicked = null!;
    private Texture2D radioPressedNormal = null!;
    private Texture2D radioPressedHovered = null!;
    private Texture2D radioPressedClicked = null!;
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

    public bool Radio => ButtonGroup != null;

    public override void _Ready()
    {
        unpressedNormal = GetThemeIcon("UnpressedNormal", "CheckBox");
        unpressedHovered = GetThemeIcon("UnpressedHovered", "CheckBox");
        unpressedClicked = GetThemeIcon("UnpressedClicked", "CheckBox");
        pressedNormal = GetThemeIcon("PressedNormal", "CheckBox");
        pressedHovered = GetThemeIcon("PressedHovered", "CheckBox");
        pressedClicked = GetThemeIcon("PressedClicked", "CheckBox");
        radioUnpressedNormal = GetThemeIcon("RadioUnpressedNormal", "CheckBox");
        radioUnpressedHovered = GetThemeIcon("RadioUnpressedHovered", "CheckBox");
        radioUnpressedClicked = GetThemeIcon("RadioUnpressedClicked", "CheckBox");
        radioPressedNormal = GetThemeIcon("RadioPressedNormal", "CheckBox");
        radioPressedHovered = GetThemeIcon("RadioPressedHovered", "CheckBox");
        radioPressedClicked = GetThemeIcon("RadioPressedClicked", "CheckBox");

        normalColor = GetThemeColor("font_color");
        focusColor = GetThemeColor("font_color_focus");
        pressedColor = GetThemeColor("font_color_pressed");

        UpdateIcon();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if ((long)what is NotificationFocusEnter or NotificationFocusExit)
        {
            focused = what == NotificationFocusEnter;

            // Update font colour based on focused state to make things more clear which box is focused (and more
            // consistent with mouse hover)
            AddThemeColorOverride("font_color", focused ? focusColor : normalColor);
            AddThemeColorOverride("font_color_pressed", focused ? focusColor : pressedColor);

            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (pressing && !Disabled)
        {
            currentState = ButtonPressed ? State.PressedClicked : State.UnpressedClicked;
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
            QueueRedraw();
        }

        base._GuiInput(@event);
    }

    private void UpdateIcon()
    {
        if (Radio)
        {
            Icon = currentState switch
            {
                State.Disabled => ButtonPressed ? radioPressedNormal : radioUnpressedNormal,
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
                State.Disabled => ButtonPressed ? pressedNormal : unpressedNormal,
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
