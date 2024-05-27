using Godot;

public static class FontUtils
{
    /// <summary>
    ///   Calculates the space a string of text needs with the given font and size. This helper exists to make calling
    ///   this simpler.
    /// </summary>
    /// <returns>The length of the rendered string</returns>
    public static Vector2 GetStringSizeWithSize(this Font font, string text, int fontSize)
    {
        return font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
    }
}
