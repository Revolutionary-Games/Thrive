using Godot;

/// <summary>
///   Utility for screen and view-related effects
/// </summary>
public static class ScreenUtils
{
    /// <summary>
    ///   Applies barrel distortion to the given position
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The math here should be the same as in chromatic.gdshader
    ///   </para>
    /// </remarks>
    public static Vector2 BarrelDistortion(Vector2 screenPos, float distortion, Vector2 viewportSize)
    {
        Vector2 resolution = DisplayServer.WindowGetSize();

        // The constant from shader calculations
        const float distortionMultiplier = 75.0f;

        distortion /= resolution.X;
        distortion *= distortionMultiplier;

        // Convert from [0, size] to [0; 1]
        screenPos /= viewportSize;

        Vector2 oversizeVector = Distort(Vector2.One, distortion);
        oversizeVector = (oversizeVector + Vector2.One) / 2.0f;
        screenPos = RemapVector(screenPos, Vector2.One - oversizeVector, oversizeVector);

        // Convert from [0, 1] to [-1; 1]
        screenPos = (screenPos * 2.0f) - Vector2.One;
        screenPos = Distort(screenPos, distortion * 0.75f);

        // Return to [0, size]
        screenPos = (screenPos + Vector2.One) / 2.0f;
        screenPos *= viewportSize;

        return screenPos;
    }

    public static Vector2 ReverseBarrelDistortion(Vector2 screenPos, float distortion, Vector2 viewportSize)
    {
        Vector2 resolution = DisplayServer.WindowGetSize();

        // The constant from shader calculations
        const float distortionMultiplier = 75.0f;

        distortion /= resolution.X;
        distortion *= distortionMultiplier;

        // Convert from [0, size] to [-1; 1]
        screenPos /= viewportSize;
        screenPos = screenPos * 2.0f - Vector2.One;

        screenPos = ReverseDistort(screenPos, distortion * 0.75f);

        // Convert from [-1; 1] to [0, 1]
        screenPos = (screenPos + Vector2.One) * 0.5f;

        Vector2 oversizeVector = Distort(Vector2.One, distortion);
        oversizeVector = (oversizeVector + Vector2.One) / 2.0f;
        screenPos = ReverseRemapVector(screenPos, Vector2.One - oversizeVector, oversizeVector);

        // Return to [0, size]
        screenPos *= viewportSize;

        return screenPos;
    }

    private static Vector2 Distort(Vector2 pos, float distortion)
    {
        float barrelDistortion1 = 0.1f * distortion;
        float barrelDistortion2 = -0.025f * distortion;

        // Replaces shader's dot(Vector2, Vector2) function
        float r2 = pos.X * pos.X + pos.Y * pos.Y;

        pos *= 1.0f + barrelDistortion1 * r2 + barrelDistortion2 * r2 * r2;

        return pos;
    }

    private static Vector2 RemapVector(Vector2 t, Vector2 a, Vector2 b)
    {
        t = (t - a) / (b - a);
        t.X = float.Clamp(t.X, 0.0f, 1.0f);
        t.Y = float.Clamp(t.Y, 0.0f, 1.0f);
        return t;
    }

    private static Vector2 ReverseDistort(Vector2 pos, float distortion)
    {
        float barrelDistortion1 = 0.1f * distortion;
        float barrelDistortion2 = -0.025f * distortion;

        // This is roughly equal to the original position's dot product
        float r2 = pos.X * pos.X + pos.Y * pos.Y;

        pos /= 1.0f + barrelDistortion1 * r2 + barrelDistortion2 * r2 * r2;

        return pos;
    }

    private static Vector2 ReverseRemapVector(Vector2 t, Vector2 a, Vector2 b)
    {
        return t * (b - a) + a;
    }
}
