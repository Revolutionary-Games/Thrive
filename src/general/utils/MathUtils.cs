﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
///   Math-related utility functions for Thrive
/// </summary>
public static class MathUtils
{
    public const float EPSILON = 0.00000001f;
    public const float DEGREES_TO_RADIANS = MathF.PI / 180;
    public const float RADIANS_TO_DEGREES = 180 / MathF.PI;
    public const double FULL_CIRCLE = Math.PI * 2;
    public const float RIGHT_ANGLE = MathF.PI / 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RoundToInt(float value)
    {
        return (int)Math.Round(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Sigmoid(double x)
    {
        return 1 / (1 + Math.Exp(-x));
    }

    /// <summary>
    ///   Creates a rotation for an organelle. This is used by the editor, but PlacedOrganelle uses RotateY as this
    ///   didn't work there for some reason.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Rotation is now the number of 60-degree rotations
    ///   </para>
    /// </remarks>
    public static Quaternion CreateRotationForOrganelle(float rotation)
    {
        return new Quaternion(new Vector3(0, -1, 0), rotation * 60 * DEGREES_TO_RADIANS);
    }

    /// <summary>
    ///   This still takes the angle in degrees as this is used from
    ///   places that calculate the angle in degrees.
    /// </summary>
    public static Quaternion CreateRotationForExternal(float angle)
    {
        return new Quaternion(new Vector3(0, 1, 0), 180 * DEGREES_TO_RADIANS) *
            new Quaternion(new Vector3(0, 1, 0), angle * DEGREES_TO_RADIANS);
    }

    /// <summary>
    ///   Returns a Lerped value, and snaps to the target value if current and target
    ///   value is approximately equal by the specified tolerance value.
    /// </summary>
    public static float Lerp(float from, float to, float weight, float tolerance = EPSILON)
    {
        if (IsEqualApproximately(from, to, tolerance))
            return to;

        return Mathf.Lerp(from, to, weight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEqualApproximately(float a, float b, float tolerance)
    {
        // Intentional equality comparison before checking with absolute value
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        return a == b || Math.Abs(a - b) < tolerance;
    }

    /// <summary>
    ///   Standard modulo for negative values in C# produces negative results.
    ///   This function returns modulo values between 0 and mod-1.
    /// </summary>
    /// <returns>The positive modulo</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PositiveModulo(this int val, int mod)
    {
        int result = val % mod;
        return result < 0 ? result + mod : result;
    }

    public static (double Average, double StandardDeviation) CalculateAverageAndStandardDeviation(
        this IEnumerable<int> enumerable)
    {
        int count = 0;
        double sum = 0;
        double sumOfSquares = 0;

        foreach (var value in enumerable)
        {
            ++count;
            sum += value;
            sumOfSquares += value * value;
        }

        if (count == 0)
            throw new InvalidOperationException("Sequence contains no elements");

        double average = sum / count;
        double standardDeviation = Math.Sqrt(sumOfSquares / count - average * average);
        return (average, standardDeviation);
    }

    public static (double Average, double StandardDeviation) CalculateAverageAndStandardDeviation(
        this IEnumerable<double> enumerable)
    {
        int count = 0;
        double sum = 0;
        double sumOfSquares = 0;

        foreach (var value in enumerable)
        {
            ++count;
            sum += value;
            sumOfSquares += value * value;
        }

        if (count == 0)
            throw new InvalidOperationException("Sequence contains no elements");

        double average = sum / count;
        double standardDeviation = Math.Sqrt(sumOfSquares / count - average * average);
        return (average, standardDeviation);
    }

    /// <summary>
    ///   How far in a given direction a set of points can reach from a reference point
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This can be useful for finding the edge of a cell from a list of its organelle positions, for instance
    ///   </para>
    /// </remarks>
    public static float GetMaximumDistanceInDirection(Vector3 direction, Vector3 referencePoint,
        IEnumerable<Vector3> listOfPoints)
    {
        float distance = 0.0f;

        foreach (var point in listOfPoints)
        {
            if (point == referencePoint)
                continue;

            var difference = point - referencePoint;

            float angle = difference.AngleTo(direction);

            if (angle >= RIGHT_ANGLE)
                continue;

            // Get the length of the part of the vector that's parallel to the direction
            float directionalLength = difference.Length() * MathF.Cos(angle);

            if (directionalLength > distance)
                distance = directionalLength;
        }

        return distance;
    }

    public static float NormalToWithNegativesRadians(float radian)
    {
        return radian <= Math.PI ? radian : radian - (float)(2 * Math.PI);
    }

    public static float WithNegativesToNormalRadians(float radian)
    {
        return radian >= 0 ? radian : (float)(2 * Math.PI) - radian;
    }

    public static float DistanceBetweenRadians(float p1, float p2)
    {
        float distance = Math.Abs(p1 - p2);
        return distance <= Math.PI ? distance : (float)(2 * Math.PI) - distance;
    }

    public static Vector3 CalculateCameraVisiblePosition(Node3D camera, float distance = 25)
    {
        var forward = camera.Transform.Basis.GetRotationQuaternion() * Vector3.Forward;

        return forward * distance;
    }

    /// <summary>
    ///   Calculates a good camera distance from the radius of an object that is photographed
    /// </summary>
    /// <param name="radius">The radius of the object</param>
    /// <param name="fieldOfView">The camera's field of view in degrees</param>
    /// <returns>The distance to use</returns>
    public static float CameraDistanceFromRadiusOfObject(float radius, float fieldOfView)
    {
        if (radius <= 0)
            throw new ArgumentException("radius needs to be over 0");

        float angle = fieldOfView * 0.5f;

        return MathF.Tan(MathF.PI * 0.5f - DEGREES_TO_RADIANS * angle) * radius;
    }
}
