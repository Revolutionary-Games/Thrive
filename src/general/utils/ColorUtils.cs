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
        var luminance = (0.299f * color.r8 + 0.587f * color.g8 + 0.114f * color.b8) / 255.0f;

        if (luminance > 0.5f)
            return true;

        return false;
    }

    /// <summary>
    ///   Check if the colour is a raw one (have values greater than 1.0)
    /// </summary>
    /// <param name="colour">Current colour</param>
    /// <returns>If the current colour is a raw one</returns>
    public static bool IsRaw(this Color colour)
    {
        return colour.r > 1 || colour.g > 1 || colour.b > 1;
    }
}
