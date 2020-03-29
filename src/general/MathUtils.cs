﻿using System;
using Godot;

/// <summary>
///   Math related utility functions for Thrive
/// </summary>
public static class MathUtils
{
    public static float EPSILON = 0.00000001f;
    public static float DEGREES_TO_RADIANS = Mathf.Pi / 180;

    public static T Clamp<T>(this T val, T min, T max)
        where T : IComparable<T>
    {
        if (val.CompareTo(min) < 0)
        {
            return min;
        }
        else if (val.CompareTo(max) > 0)
        {
            return max;
        }
        else
        {
            return val;
        }
    }

    public static double
       Sigmoid(double x)
    {
        return 1 / (1 + Math.Exp(-x));
    }

    /// <summary>
    ///   Rotation is now the number of 60 degree rotations
    /// </summary>
    public static Quat CreateRotationForOrganelle(float rotation)
    {
        return new Quat(new Vector3(0, 1, 0), 180 * DEGREES_TO_RADIANS) *
            new Quat(new Vector3(0, -1, 0), rotation * 60 * DEGREES_TO_RADIANS);
    }

    public static Quat CreateRotationForExternal(float angle)
    {
        return new Quat(new Vector3(0, 1, 0), 180 * DEGREES_TO_RADIANS) *
            new Quat(new Vector3(0, 1, 0), angle * DEGREES_TO_RADIANS);
    }
}
