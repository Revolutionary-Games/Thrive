using Godot;

public static class InputEventKeyUtils
{
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
            return DisplayServer.KeyboardGetLabelFromPhysical(key.PhysicalKeycode);
        }

        return key.Keycode;
    }
}
