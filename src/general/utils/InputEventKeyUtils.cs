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
        if (key.Keycode == Key.None)
        {
            if (key.KeyLabel == Key.None)
                GD.PrintErr("Key has both key code and label as none");

            return key.KeyLabel;
        }

        return key.Keycode;
    }
}
