using System;
using System.Linq;
using Godot;
using Godot.Collections;

/// <summary>
///   Resolves input actions to icons for them
/// </summary>
public static class KeyPromptHelper
{
    private static readonly string[] AvailableKeys =
    {
        "0", "Arrow_Left", "End", "F9", "Shift_Alt",
        "10", "Arrow_Right", "Enter_Alt", "F", "Shift",
        "11", "Arrow_Up", "Enter", "G", "N", "S",
        "12", "Asterisk", "Enter_Tall", "H", "Num_Lock", "Slash",
        "1", "Backspace_Alt", "Esc", "Home", "O", "Space",
        "2", "Backspace", "F10", "I", "Page_Down", "Tab",
        "3", "B", "F11", "Insert", "Page_Up", "Tilda",
        "4", "Bracket_Left", "F12", "J", "P", "T",
        "5", "Bracket_Right", "F1", "K", "Plus", "U",
        "6", "Caps_Lock", "F2", "L", "Plus_Tall", "V",
        "7", "C", "F3", "Mark_Left", "Print_Screen", "Win",
        "8", "Command", "F4", "Mark_Right", "Q", "W",
        "9", "Ctrl", "F5", "Minus", "Question", "X",
        "A", "Del", "F6", "M", "Quote", "Y",
        "Alt", "D", "F7", "R", "Z",
        "Arrow_Down", "E", "F8", "Semicolon",
    };

    private static string theme = "Dark";
    private static ActiveInputMethod inputMethod = ActiveInputMethod.Keyboard;
    private static ControllerType activeControllerType = Constants.DEFAULT_CONTROLLER_TYPE;

    /// <summary>
    ///   Event triggered when the key icons change, any GUI
    /// </summary>
    public static event EventHandler? IconsChanged;

    /// <summary>
    ///   Event triggered when controller type changes (sent even when input type is not a controller). Usually most
    ///   places should listen for <see cref="IconsChanged"/> instead
    /// </summary>
    public static event EventHandler? ControllerTypeChanged;

    /// <summary>
    ///   Button theme for keyboard, valid values: "Dark", "Light"
    /// </summary>
    public static string Theme
    {
        get => theme;
        set
        {
            if (theme == value)
                return;

            theme = value;
            var handler = IconsChanged;
            handler?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>
    ///   Mapping of current Theme to the names of blank images, valid values: "Black", "White"
    /// </summary>
    public static string BlankTheme => theme switch
    {
        "Dark" => "Black",
        "Light" => "White",
        _ => theme,
    };

    /// <summary>
    ///   The current primary input method for showing button prompts with
    /// </summary>
    public static ActiveInputMethod InputMethod
    {
        get => inputMethod;
        set
        {
            if (inputMethod == value)
                return;

            inputMethod = value;
            var handler = IconsChanged;
            handler?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>
    ///   The type of controller to show input prompt buttons with
    /// </summary>
    public static ControllerType ActiveControllerType
    {
        get => activeControllerType;
        set
        {
            if (value == ControllerType.Automatic)
                throw new ArgumentException("This property can only have concrete valid controller types");

            if (activeControllerType == value)
                return;

            activeControllerType = value;

            var controllerHandler = ControllerTypeChanged;
            controllerHandler?.Invoke(null, EventArgs.Empty);

            // Only need to send the update signal if using controller input currently as otherwise the buttons don't
            // need to change
            if (InputMethod == ActiveInputMethod.Controller)
            {
                var handler = IconsChanged;
                handler?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    ///   Returns a path to an icon for the action
    /// </summary>
    /// <param name="actionName">Name of the action</param>
    /// <returns>
    ///   A tuple of path to the icon for the action and potentially an overlay image to be drawn on top
    /// </returns>
    public static (string Primary, string? Overlay) GetPathForAction(string actionName)
    {
        return GetPathForAction(InputMap.ActionGetEvents(actionName));
    }

    /// <summary>
    ///   Returns a path to an icon for the action
    /// </summary>
    /// <param name="actionName">Name of the action</param>
    /// <returns>A tuple of icon for the action and a potential overlay that should be drawn on top</returns>
    public static (Texture2D Primary, Texture2D? Overlay) GetTextureForAction(string actionName)
    {
        var (primaryPath, overlayPath) = GetPathForAction(actionName);

        Texture2D? overlay = null;

        if (overlayPath != null)
            overlay = GD.Load<Texture2D>(overlayPath);

        return (GD.Load<Texture2D>(primaryPath), overlay);
    }

    /// <summary>
    ///   Returns a path to an icon for the already resolved action
    /// </summary>
    /// <param name="actionList">The list of buttons for the action</param>
    /// <returns>
    ///   A tuple of path to the icon for the action and potentially an overlay image to be drawn on top
    /// </returns>
    public static (string Primary, string? Overlay) GetPathForAction(Array<InputEvent> actionList)
    {
        // Find the first action matching InputMethod
        foreach (var action in actionList)
        {
            switch (InputMethod)
            {
                case ActiveInputMethod.Keyboard:
                {
                    if (action is InputEventKey key)
                    {
                        return (GetPathForKeyboardKey(OS.GetKeycodeString(key.Keycode)), null);
                    }

                    if (action is InputEventMouseButton button)
                    {
                        return GetPathForMouseButton(button.ButtonIndex);
                    }

                    break;
                }

                case ActiveInputMethod.Controller:
                {
                    if (action is InputEventJoypadButton joypadButton)
                    {
                        return (GetPathForControllerButton(joypadButton.ButtonIndex), null);
                    }

                    if (action is InputEventJoypadMotion joypadMotion)
                    {
                        return (GetPathForControllerAxis(joypadMotion.Axis),
                            GetPathForControllerAxisDirection(joypadMotion.Axis, joypadMotion.AxisValue));
                    }

                    break;
                }
            }
        }

        return (GetPathForInvalidKey(), null);
    }

    /// <summary>
    ///   Returns an image to be used as fallback when no prompt icon exists
    /// </summary>
    public static string GetPathForInvalidKey()
    {
        return $"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/Unknown_Key_{Theme}.png";
    }

    /// <summary>
    ///   Returns a key image for a keyboard key
    /// </summary>
    public static string GetPathForKeyboardKey(string name)
    {
        // Map some key names to match the icon set used key names
        switch (name)
        {
            case "Escape":
                name = "Esc";
                break;
        }

        if (!AvailableKeys.Contains(name))
            return GetPathForInvalidKey();

        return $"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/{name}_Key_{Theme}.png";
    }

    /// <summary>
    ///   Returns an icon for a mouse button (directions require an overlay, which when not null needs to be opened
    ///   as a separate layer for correct display)
    /// </summary>
    /// <returns>
    ///   A tuple of the primary key texture and an optional overlay texture that should be drawn on top of the primary
    ///   one. If not drawn the icon will not be clear
    /// </returns>
    public static (string Primary, string? Overlay) GetPathForMouseButton(MouseButton button)
    {
        switch (button)
        {
            case MouseButton.Left:
                return ($"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/Mouse_Left_Key_{Theme}.png",
                    null);
            case MouseButton.Middle:
                return ($"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/Mouse_Middle_Key_{Theme}.png",
                    null);
            case MouseButton.Right:
                return ($"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/Mouse_Right_Key_{Theme}.png",
                    null);
            case MouseButton.WheelUp:
                return ($"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/Mouse_Middle_Key_{Theme}.png",
                    "res://assets/textures/gui/xelu_prompts/Customized/Directional_Arrow_Up.png");
            case MouseButton.WheelDown:
                return ($"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/Mouse_Middle_Key_{Theme}.png",
                    "res://assets/textures/gui/xelu_prompts/Customized/Directional_Arrow_Down.png");
            case MouseButton.WheelLeft:
                return ($"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/Mouse_Middle_Key_{Theme}.png",
                    "res://assets/textures/gui/xelu_prompts/Customized/Directional_Arrow_Left.png");
            case MouseButton.WheelRight:
                return ($"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/Mouse_Middle_Key_{Theme}.png",
                    "res://assets/textures/gui/xelu_prompts/Customized/Directional_Arrow_Right.png");

            // TODO: handle the extra mouse buttons 1 and 2 (need custom images for them)
        }

        return (GetPathForInvalidKey(), null);
    }

    public static string GetPathForControllerButton(JoyButton button)
    {
        switch (activeControllerType)
        {
            case ControllerType.Xbox360:
                return GetXboxControllerButton("Xbox 360", "360_", button);
            case ControllerType.XboxOne:
                return GetXboxOneControllerButton("Xbox One", "XboxOne_", button);
            default:
            case ControllerType.XboxSeriesX:
                return GetXboxSeriesControllerButton("Xbox Series X", "XboxSeriesX_", button);

            case ControllerType.PlayStation3:
                return GetPlayStationControllerButton("PS3", button);
            case ControllerType.PlayStation4:
                return GetPlayStationControllerButton("PS4", button);
            case ControllerType.PlayStation5:
                return GetPlayStationControllerButton("PS5", button);
        }
    }

    public static string GetPathForControllerAxis(JoyAxis axis)
    {
        switch (activeControllerType)
        {
            case ControllerType.Xbox360:
                return GetXboxControllerAxis("Xbox 360", "360_", axis);
            case ControllerType.XboxOne:
                return GetXboxControllerAxis("Xbox One", "XboxOne_", axis);
            default:
            case ControllerType.XboxSeriesX:
                return GetXboxControllerAxis("Xbox Series X", "XboxSeriesX_", axis);

            case ControllerType.PlayStation3:
                return GetPlayStationControllerAxis("PS3", axis);
            case ControllerType.PlayStation4:
                return GetPlayStationControllerAxis("PS4", axis);
            case ControllerType.PlayStation5:
                return GetPlayStationControllerAxis("PS5", axis);
        }
    }

    public static string? GetPathForControllerAxisDirection(JoyAxis axis, float direction, bool large = true)
    {
        var suffix = large ? string.Empty : "_Unscaled";

        string directionName;

        switch (axis)
        {
            // Handling both sticks at once here assumes the direction mappings are the same
            // TODO: the above needs confirming
            case JoyAxis.LeftY:
            case JoyAxis.RightY:
                directionName = direction < 0 ? "Up" : "Down";

                break;

            case JoyAxis.LeftX:
            case JoyAxis.RightX:
                directionName = direction < 0 ? "Left" : "Right";

                break;

            // These don't really have "directions" so we return empty for them
            case JoyAxis.TriggerLeft:
            case JoyAxis.TriggerRight:
                return null;

            // But unknown value is still error
            default:
                return GetPathForInvalidKey();
        }

        return $"res://assets/textures/gui/xelu_prompts/Customized/Directional_Arrow_{directionName}{suffix}.png";
    }

    private static string GetXboxControllerButton(string folder, string typePrefix, JoyButton button)
    {
        string buttonName;

        switch (button)
        {
            case JoyButton.A:
                buttonName = "A";
                break;
            case JoyButton.B:
                buttonName = "B";
                break;
            case JoyButton.X:
                buttonName = "X";
                break;
            case JoyButton.Y:
                buttonName = "Y";
                break;
            case JoyButton.LeftShoulder:
                buttonName = "LB";
                break;
            case JoyButton.RightShoulder:
                buttonName = "RB";
                break;

            // NOTE: bumpers no longer part of the button list
            /*case JoyButton.L2:
                buttonName = "LT";
                break;
            case JoyButton.R2:
                buttonName = "RT";
                break;*/
            case JoyButton.LeftStick:
                buttonName = "Left_Stick_Click";
                break;
            case JoyButton.RightStick:
                buttonName = "Right_Stick_Click";
                break;
            case JoyButton.Back:
                buttonName = "Back";
                break;
            case JoyButton.Start:
                buttonName = "Start";
                break;
            case JoyButton.DpadUp:
                buttonName = "Dpad_Up";
                break;
            case JoyButton.DpadDown:
                buttonName = "Dpad_Down";
                break;
            case JoyButton.DpadLeft:
                buttonName = "Dpad_Left";
                break;
            case JoyButton.DpadRight:
                buttonName = "Dpad_Right";
                break;
            default:
                return GetPathForInvalidKey();
        }

        return $"res://assets/textures/gui/xelu_prompts/{folder}/{typePrefix}{buttonName}.png";
    }

    private static string GetXboxOneControllerButton(string folder, string typePrefix, JoyButton button)
    {
        string buttonName;

        switch (button)
        {
            case JoyButton.Back:
                buttonName = "Windows";
                break;
            case JoyButton.Start:
                buttonName = "Menu";
                break;

            default:
                return GetXboxControllerButton(folder, typePrefix, button);
        }

        return $"res://assets/textures/gui/xelu_prompts/{folder}/{typePrefix}{buttonName}.png";
    }

    private static string GetXboxSeriesControllerButton(string folder, string typePrefix, JoyButton button)
    {
        string buttonName;

        switch (button)
        {
            case JoyButton.Back:
                buttonName = "View";
                break;

            default:
                return GetXboxOneControllerButton(folder, typePrefix, button);
        }

        return $"res://assets/textures/gui/xelu_prompts/{folder}/{typePrefix}{buttonName}.png";
    }

    private static string GetXboxControllerAxis(string folder, string typePrefix, JoyAxis axis)
    {
        // TODO: direction indicator for the axes
        string buttonName;

        switch (axis)
        {
            case JoyAxis.LeftY:
            case JoyAxis.LeftX:
                buttonName = "Left_Stick";
                break;
            case JoyAxis.RightY:
            case JoyAxis.RightX:
                buttonName = "Right_Stick";
                break;
            case JoyAxis.TriggerLeft:
                buttonName = "LT";
                break;
            case JoyAxis.TriggerRight:
                buttonName = "RT";
                break;
            default:
                return GetPathForInvalidKey();
        }

        return $"res://assets/textures/gui/xelu_prompts/{folder}/{typePrefix}{buttonName}.png";
    }

    private static string GetPlayStationControllerButton(string type, JoyButton button)
    {
        string buttonName;

        switch (button)
        {
            case JoyButton.A:
                buttonName = "Cross";
                break;
            case JoyButton.B:
                buttonName = "Circle";
                break;
            case JoyButton.X:
                buttonName = "Square";
                break;
            case JoyButton.Y:
                buttonName = "Triangle";
                break;
            case JoyButton.LeftShoulder:
                buttonName = "L1";
                break;
            case JoyButton.RightShoulder:
                buttonName = "R1";
                break;

            // No longer present in button list
            /*case JoyButton.L2:
                buttonName = "L2";
                break;
            case JoyButton.R2:
                buttonName = "R2";
                break;*/

            case JoyButton.LeftStick:
                buttonName = "Left_Stick_Click";
                break;
            case JoyButton.RightStick:
                buttonName = "Right_Stick_Click";
                break;
            case JoyButton.Back:
            {
                if (type == "PS3")
                {
                    buttonName = "Select";
                }
                else if (type == "PS4")
                {
                    buttonName = "Share";
                }
                else
                {
                    buttonName = "Share_Alt";
                }

                break;
            }

            case JoyButton.Start:
            {
                if (type == "PS3")
                {
                    buttonName = "Start";
                }
                else if (type == "PS4")
                {
                    buttonName = "Options";
                }
                else
                {
                    buttonName = "Options_Alt";
                }

                break;
            }

            case JoyButton.DpadUp:
                buttonName = "Dpad_Up";
                break;
            case JoyButton.DpadDown:
                buttonName = "Dpad_Down";
                break;
            case JoyButton.DpadLeft:
                buttonName = "Dpad_Left";
                break;
            case JoyButton.DpadRight:
                buttonName = "Dpad_Right";
                break;
            case JoyButton.Touchpad:
                buttonName = "Touch_Pad";
                break;
            default:
                return GetPathForInvalidKey();
        }

        return $"res://assets/textures/gui/xelu_prompts/{type}/{type}_{buttonName}.png";
    }

    private static string GetPlayStationControllerAxis(string type, JoyAxis axis)
    {
        // TODO: direction indicator for the axes
        string buttonName;

        switch (axis)
        {
            case JoyAxis.LeftY:
            case JoyAxis.LeftX:
                buttonName = "Left_Stick";
                break;
            case JoyAxis.RightY:
            case JoyAxis.RightX:
                buttonName = "Right_Stick";
                break;
            case JoyAxis.TriggerLeft:
                buttonName = "L2";
                break;
            case JoyAxis.TriggerRight:
                buttonName = "R2";
                break;
            default:
                return GetPathForInvalidKey();
        }

        return $"res://assets/textures/gui/xelu_prompts/{type}/{type}_{buttonName}.png";
    }
}
