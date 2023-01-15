﻿using System;
using System.Linq;
using Godot;
using Array = Godot.Collections.Array;

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
    /// <returns>Path to the icon for the action</returns>
    public static string GetPathForAction(string actionName)
    {
        return GetPathForAction(InputMap.GetActionList(actionName));
    }

    /// <summary>
    ///   Returns a path to an icon for the action
    /// </summary>
    /// <param name="actionName">Name of the action</param>
    /// <returns>An icon for the action</returns>
    public static Texture GetTextureForAction(string actionName)
    {
        return GD.Load<Texture>(GetPathForAction(actionName));
    }

    /// <summary>
    ///   Returns a path to an icon for the already resolved action
    /// </summary>
    /// <param name="actionList">The list of buttons for the action</param>
    /// <returns>Path to the icon for the action</returns>
    public static string GetPathForAction(Array actionList)
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
                        return GetPathForKeyboardKey(OS.GetScancodeString(key.Scancode));
                    }

                    if (action is InputEventMouseButton button)
                    {
                        return GetPathForMouseButton((ButtonList)button.ButtonIndex);
                    }

                    break;
                }

                case ActiveInputMethod.Controller:
                {
                    if (action is InputEventJoypadButton joypadButton)
                    {
                        return GetPathForControllerButton((JoystickList)joypadButton.ButtonIndex);
                    }

                    if (action is InputEventJoypadMotion joypadMotion)
                    {
                        return GetPathForControllerAxis((JoystickList)joypadMotion.Axis);
                    }

                    break;
                }
            }
        }

        return GetPathForInvalidKey();
    }

    /// <summary>
    ///   Returns an image to be used as fallback when no prompt icon exists
    /// </summary>
    public static string GetPathForInvalidKey()
    {
        return $"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/Blanks/Blank_{BlankTheme}_Normal.png";
    }

    /// <summary>
    ///   Returns a key image for a keyboard key
    /// </summary>
    public static string GetPathForKeyboardKey(string name)
    {
        if (!AvailableKeys.Contains(name))
            return GetPathForInvalidKey();

        return $"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/{name}_Key_{Theme}.png";
    }

    /// <summary>
    ///   Returns an icon for a mouse button
    /// </summary>
    public static string GetPathForMouseButton(ButtonList button)
    {
        switch (button)
        {
            case ButtonList.Left:
                return $"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/Mouse_Left_Key_{Theme}.png";
            case ButtonList.Middle:
                return $"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/Mouse_Middle_Key_{Theme}.png";
            case ButtonList.Right:
                return $"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/{Theme}/Mouse_Right_Key_{Theme}.png";
        }

        return GetPathForInvalidKey();
    }

    public static string GetPathForControllerButton(JoystickList button)
    {
        switch (activeControllerType)
        {
            case ControllerType.Xbox360:
                return GetXboxControllerButton("Xbox 360", "360_", button);
            case ControllerType.XboxOne:
                return GetXboxControllerButton("Xbox One", "XboxOne_", button);
            default:
            case ControllerType.XboxSeriesX:
                return GetXboxControllerButton("Xbox Series X", "XboxSeriesX_", button);

            case ControllerType.PlayStation3:
                return GetPlayStationControllerButton("PS3", button);
            case ControllerType.PlayStation4:
                return GetPlayStationControllerButton("PS4", button);
            case ControllerType.PlayStation5:
                return GetPlayStationControllerButton("PS5", button);
        }
    }

    public static string GetPathForControllerAxis(JoystickList axis)
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

    private static string GetXboxControllerButton(string folder, string typePrefix, JoystickList button)
    {
        string buttonName;

        switch (button)
        {
            case JoystickList.XboxA:
                buttonName = "A";
                break;
            case JoystickList.XboxB:
                buttonName = "B";
                break;
            case JoystickList.XboxX:
                buttonName = "X";
                break;
            case JoystickList.XboxY:
                buttonName = "Y";
                break;
            case JoystickList.L:
                buttonName = "LB";
                break;
            case JoystickList.R:
                buttonName = "RB";
                break;
            case JoystickList.L2:
                buttonName = "LT";
                break;
            case JoystickList.R2:
                buttonName = "RT";
                break;
            case JoystickList.L3:
                buttonName = "Left_Stick_Click";
                break;
            case JoystickList.R3:
                buttonName = "Right_Stick_Click";
                break;
            case JoystickList.Select:
                buttonName = "Back";
                break;
            case JoystickList.Start:
                buttonName = "Start";
                break;
            case JoystickList.DpadUp:
                buttonName = "Dpad_Up";
                break;
            case JoystickList.DpadDown:
                buttonName = "Dpad_Down";
                break;
            case JoystickList.DpadLeft:
                buttonName = "Dpad_Left";
                break;
            case JoystickList.DpadRight:
                buttonName = "Dpad_Right";
                break;
            default:
                return GetPathForInvalidKey();
        }

        return $"res://assets/textures/gui/xelu_prompts/{folder}/{typePrefix}{buttonName}.png";
    }

    private static string GetXboxControllerAxis(string folder, string typePrefix, JoystickList axis)
    {
        // TODO: direction indicator for the axes
        string buttonName;

        switch (axis)
        {
            case JoystickList.AnalogLy:
            case JoystickList.AnalogLx:
                buttonName = "Left_Stick";
                break;
            case JoystickList.AnalogRy:
            case JoystickList.AnalogRx:
                buttonName = "Right_Stick";
                break;
            case JoystickList.AnalogL2:
                buttonName = "LT";
                break;
            case JoystickList.AnalogR2:
                buttonName = "RT";
                break;
            default:
                return GetPathForInvalidKey();
        }

        return $"res://assets/textures/gui/xelu_prompts/{folder}/{typePrefix}{buttonName}.png";
    }

    private static string GetPlayStationControllerButton(string type, JoystickList button)
    {
        string buttonName;

        switch (button)
        {
            case JoystickList.SonyX:
                buttonName = "Cross";
                break;
            case JoystickList.SonyCircle:
                buttonName = "Circle";
                break;
            case JoystickList.SonySquare:
                buttonName = "Square";
                break;
            case JoystickList.SonyTriangle:
                buttonName = "Triangle";
                break;
            case JoystickList.L:
                buttonName = "L1";
                break;
            case JoystickList.R:
                buttonName = "R1";
                break;
            case JoystickList.L2:
                buttonName = "L2";
                break;
            case JoystickList.R2:
                buttonName = "R2";
                break;
            case JoystickList.L3:
                buttonName = "Left_Stick_Click";
                break;
            case JoystickList.R3:
                buttonName = "Right_Stick_Click";
                break;
            case JoystickList.Select:
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

            case JoystickList.Start:
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

            case JoystickList.DpadUp:
                buttonName = "Dpad_Up";
                break;
            case JoystickList.DpadDown:
                buttonName = "Dpad_Down";
                break;
            case JoystickList.DpadLeft:
                buttonName = "Dpad_Left";
                break;
            case JoystickList.DpadRight:
                buttonName = "Dpad_Right";
                break;
            case JoystickList.Touchpad:
                buttonName = "Touch_Pad";
                break;
            default:
                return GetPathForInvalidKey();
        }

        return $"res://assets/textures/gui/xelu_prompts/{type}/{type}_{buttonName}.png";
    }

    private static string GetPlayStationControllerAxis(string type, JoystickList axis)
    {
        // TODO: direction indicator for the axes
        string buttonName;

        switch (axis)
        {
            case JoystickList.AnalogLy:
            case JoystickList.AnalogLx:
                buttonName = "Left_Stick";
                break;
            case JoystickList.AnalogRy:
            case JoystickList.AnalogRx:
                buttonName = "Right_Stick";
                break;
            case JoystickList.AnalogL2:
                buttonName = "L2";
                break;
            case JoystickList.AnalogR2:
                buttonName = "R2";
                break;
            default:
                return GetPathForInvalidKey();
        }

        return $"res://assets/textures/gui/xelu_prompts/{type}/{type}_{buttonName}.png";
    }
}
