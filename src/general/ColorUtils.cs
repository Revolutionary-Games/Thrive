using Godot;

/// <summary>
///   Helper methods and extensions for Godot's Color class.
/// </summary>
public static class ColorUtils
{
    /// <summary>
    ///   True if the given color is on a brighter shade, false otherwise.
    ///   From https://stackoverflow.com/a/1855903.
    /// </summary>
    public static bool IsLuminuous(this Color color)
    {
        var luminance = (0.299 * color.r8 + 0.587 * color.g8 + 0.114 * color.b8) / 255;

        if (luminance > 0.5)
            return true;

        return false;
    }
}
