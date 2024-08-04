﻿using System;
using Godot;

/// <summary>
///   Utility for screen and view-related effects
/// </summary>
public static class ScreenUtils
{
    /// <summary>
    ///   Applies barrel distortion to the given position
    /// </summary>
    /// <param name="pos">
    ///   Position in the range of [0; resolution.X] for x and [0; resolution.Y] for y
    /// </param>
    public static Vector2 BarrelDistortion(Vector2 pos, float distortion, Vector2 viewportSize)
    {
        Vector2 resolution = DisplayServer.WindowGetSize();

        // Constants from shader calculations
        const float distortionMultiplier = 75.0f * 0.5f;

        distortion /= resolution.X;
        distortion *= distortionMultiplier;

        // Convert from [0, size] to [0; 1]
        pos /= viewportSize;

        Vector2 oversizeVector = Distort(new Vector2(1.0f, 1.0f), distortion);
        pos = RemapVector(pos, new Vector2(1.0f, 1.0f) - oversizeVector, oversizeVector);

        // Convert from [0, 1] to [-1; 1]
        pos = (pos * 2.0f) - new Vector2(1.0f, 1.0f);
        pos = Distort(pos, distortion * 1.25f);

        // Return to [0, size]
        pos = (pos + new Vector2(1.0f, 1.0f)) / 2.0f;
        pos *= viewportSize;

        return pos;
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
}
