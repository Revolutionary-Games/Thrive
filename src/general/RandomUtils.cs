using System;

/// <summary>
///   Random extensions for Thrive
/// </summary>
public static class RandomUtils
{
    /// <summary>
    ///   Next float (similar to NextDouble)
    /// </summary>
    /// <returns>The float between 0 and 1.</returns>
    public static float NextFloat(this Random random)
    {
        return (float)random.NextDouble();
    }

    /// <summary>
    ///   Returns a random float in range
    /// </summary>
    /// <returns>The next random float in range (min, max)</returns>
    /// <param name="random">Random</param>
    /// <param name="min">Minimum value</param>
    /// <param name="max">Maximum value</param>
    public static float Next(this Random random, float min, float max)
    {
        return random.NextFloat() * (max - min) + min;
    }
}
