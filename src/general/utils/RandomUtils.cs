using System;

/// <summary>
///   Random extensions for Thrive
/// </summary>
public static class RandomUtils
{
    /// <summary>
    ///   Returns a random float in range
    /// </summary>
    /// <returns>The next random float in range (min, max)</returns>
    /// <param name="random">Random</param>
    /// <param name="min">Minimum value</param>
    /// <param name="max">Maximum value</param>
    public static float Next(this Random random, float min, float max)
    {
        return random.NextSingle() * (max - min) + min;
    }
}
