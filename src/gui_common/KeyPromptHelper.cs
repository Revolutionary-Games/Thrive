using System;
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

    /// <summary>
    ///   Event triggered when the key icons change, any GUI
    /// </summary>
    public static event EventHandler IconsChanged;

    public enum ActiveInputMethod
    {
        /// <summary>
        ///   Keyboard and mouse is used for input
        /// </summary>
        Keyboard,

        /// <summary>
        ///   A controller (xbox style buttons) is used for input
        /// </summary>
        Controller,
    }

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
                    if (action is InputEventKey key)
                    {
                        return GetPathForKeyboardKey(OS.GetScancodeString(key.Scancode));
                    }
                    else if (action is InputEventMouseButton button)
                    {
                        return GetPathForMouseButton((ButtonList)button.ButtonIndex);
                    }

                    break;
                case ActiveInputMethod.Controller:
                    // TODO: implement
                    break;
            }
        }

        return GetPathForInvalidKey();
    }

    /// <summary>
    ///   Returns an image to be used as fallback when no prompt icon exists
    /// </summary>
    public static string GetPathForInvalidKey()
    {
        return $"res://assets/textures/gui/xelu_prompts/Keyboard_Mouse/Blanks/Blank_{Theme}_Normal.png";
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
}
