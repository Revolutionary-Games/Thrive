using Godot;

public static class LabelSettingsUtils
{
    /// <summary>
    ///   Duplicates label settings but with a different colour
    /// </summary>
    /// <param name="settings">Settings to duplicate</param>
    /// <param name="newColour">New colour for the font</param>
    /// <returns>The duplicated font</returns>
    public static LabelSettings CloneWithDifferentColour(this LabelSettings settings, Color newColour)
    {
        return new LabelSettings
        {
            Font = settings.Font,
            FontSize = settings.FontSize,
            FontColor = newColour,
            OutlineSize = 0,
            ShadowColor = settings.ShadowColor,
            ShadowSize = settings.ShadowSize,
            ShadowOffset = settings.ShadowOffset,
        };
    }
}
