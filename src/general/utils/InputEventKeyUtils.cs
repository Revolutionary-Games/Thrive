using Godot;

public static class InputEventKeyUtils
{
    private static bool checkedKeyboardTranslationAccess;
    private static bool canUsePhysicalConversion;

    private static bool printedWarning;

    /// <summary>
    ///   Gets the key code or label for a key based on what type it is
    /// </summary>
    /// <param name="key">The key to get the code for</param>
    /// <returns>The code or label whichever is actually used for this key</returns>
    public static Key KeyCodeOrLabel(this InputEventKey key)
    {
        if (key.KeyLabel != Key.None)
        {
            return key.KeyLabel;
        }

        if (key.PhysicalKeycode != Key.None)
        {
            if (CanGetKeyLabelFromPhysical())
            {
                return DisplayServer.KeyboardGetLabelFromPhysical(key.PhysicalKeycode);
            }

            if (!CanUseKeyCodeConversion())
            {
                if (!printedWarning)
                {
                    GD.Print("WARNING: Cannot get key code from physical key code. This is likely due to running " +
                        "in headless mode. If not there's a problem with the current platform.");
                    printedWarning = true;
                }

                return key.PhysicalKeycode;
            }

            return DisplayServer.KeyboardGetKeycodeFromPhysical(key.PhysicalKeycode);
        }

        return key.Keycode;
    }

    public static bool CanGetKeyLabelFromPhysical()
    {
        // TODO: this could additionally be a user-definable setting if we need to support users who some reason have
        // problems with the key label approach

        return CanUseKeyCodeConversion();
    }

    public static bool CanUseKeyCodeConversion()
    {
        if (!checkedKeyboardTranslationAccess)
        {
            var serverName = DisplayServer.GetName();

            // TODO: add android / ios once we support those
            canUsePhysicalConversion = serverName != "headless" && serverName != "web";
            checkedKeyboardTranslationAccess = true;
        }

        return canUsePhysicalConversion;
    }
}
