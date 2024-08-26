using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Mathematical function based on convolution surfaces. Uses bones, each defined by two metaballs, to calculate
///   value of an arbitrary point in space.
/// </summary>
public class Scalis : IMeshGeneratingFunction
{
    public float CutoffPointMultiplier = 1.2f;

    public float InnerCutoffPointMultiplier = 2.0f;

    public bool Cutoff = true;

    private static readonly int[] Coefficients = { 1, 2, 1 };

    /// <summary>
    ///   Every metaball represents a point in creature's skeleton.
    ///   Each bone is formed between a point and its parent.
    /// </summary>
    private readonly MulticellularMetaball[] points;

    /// <summary>
    ///   Max squared distance from a bone's center for a point to be calculated.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     i-th element here refers to the i-th point's bone in points[]. If that point has no parent, then it has no
    ///     bone, so the distance in this array is meaningless for the point.
    ///   </para>
    /// </remarks>
    private readonly float[] boneMaxSquareDistanceCache;

    private float surfaceValue = 1.0f;

    public Scalis(IReadOnlyCollection<MulticellularMetaball> metaballs)
    {
        points = new MulticellularMetaball[metaballs.Count];

        int i = 0;

        foreach (var point in metaballs)
        {
            points[i] = point;
            ++i;
        }

        boneMaxSquareDistanceCache = new float[points.Length];

        for (i = 0; i < points.Length; ++i)
        {
            var point = points[i];

            var pointParent = point.Parent;
            if (pointParent == null)
                continue;

            float maxDistanceFromCenter = (point.Position - pointParent.Position).Length() * 0.5f
                + point.Radius + pointParent.Radius;
            maxDistanceFromCenter *= CutoffPointMultiplier;

            boneMaxSquareDistanceCache[i] = Mathf.Pow(maxDistanceFromCenter, 2.0f);
        }

        if (points.Length == 1)
        {
            boneMaxSquareDistanceCache[0] = MathF.Pow(points[0].Radius * 2.0f, 2.0f);
        }
    }

    public float SurfaceValue
    {
        get => surfaceValue;
        set => surfaceValue = value;
    }

    public float GetValue(Vector3 pos)
    {
        const float sigma = 1.0f;
        const float inverseSigma = 1 / sigma;

        float value = 0.0f;

        pos *= inverseSigma;

        const int i = 3;

        for (int j = 0; j < points.Length; ++j)
        {
            var point = points[j];

            if (point.Parent == null && points.Length > 1)
                continue;

            float aRadius = point.Radius;
            float bRadius;

            if (points.Length == 1)
            {
                bRadius = point.Radius;
            }
            else
            {
                bRadius = point.Parent!.Radius;
            }

            Vector3 a = point.Position * inverseSigma;
            Vector3 b;

            if (points.Length == 1)
            {
                b = point.Position * inverseSigma;
                a.X -= aRadius;
                b.X += aRadius;
            }
            else
            {
                b = point.Parent!.Position * inverseSigma;
            }

            if (Cutoff && boneMaxSquareDistanceCache[j] < (pos - (a + b) * 0.5f).LengthSquared())
            {
                continue;
            }

            float tau0 = aRadius > bRadius ? bRadius : aRadius;
            float deltaTau = Mathf.Abs(aRadius - bRadius);

            if (aRadius > bRadius)
            {
                (a, b) = (b, a);
            }

            // Since "i" is always 3, we can cut down on some calculations and make static coeff array
            // var coeff = BinomialExpansion(i - 1);

            for (int k = 0; k < Coefficients.Length; ++k)
            {
                value += Coefficients[k] * Mathf.Pow(deltaTau, k) * Mathf.Pow(tau0, i - k - 1)
                    * Convolution(k, i, a, b, pos);
            }
        }

        return value / NormalizationFactor(i, sigma);
    }

    public Color GetColour(Vector3 pos)
    {
        if (points.Length == 1)
            return points[0].Colour;

        Color colourSum = Colors.Black;

        float contributionSum = 0.0f;

        foreach (var point in points)
        {
            if (point.Parent == null)
                continue;

            float abDistanceSquared = point.Position.DistanceSquaredTo(point.Parent.Position);

            Vector3 linePos = ClosestPoint(pos, point.Position, point.Parent.Position);

            float contribution = 1.0f / linePos.DistanceSquaredTo(pos);

            contributionSum += contribution;

            colourSum += point.Colour.Lerp(point.Parent.Colour, point.Position.DistanceSquaredTo(linePos)
                / abDistanceSquared) * contribution;
        }

        colourSum /= contributionSum;

        // Make result colour always opaque
        colourSum.A = 1.0f;

        return colourSum;
    }

    private float NormalizationFactor(int i, float sigma)
    {
        if (i == 2)
        {
            return sigma * sigma * 3.14159265f;
        }

        if (i == 3)
        {
            return sigma * sigma * sigma * 2;
        }

        if (i < 0)
        {
            throw new ArgumentException("i is less that 0");
        }

        return sigma * sigma * (i - 3) / (i - 2) * NormalizationFactor(i - 2, sigma);
    }

    private Vector3 ClosestPoint(Vector3 pos, Vector3 a, Vector3 b)
    {
        var ab = b - a;
        var ap = pos - a;

        float cos = ab.Dot(ap) / Mathf.Sqrt(ab.LengthSquared() * ap.LengthSquared());

        if (cos < 0)
            return a;

        Vector3 linePos = a + ab.Normalized() * cos * ap.Length();

        if ((linePos - a).LengthSquared() > ab.LengthSquared())
            return b;

        return linePos;
    }

    private float Convolution(int k, int i, Vector3 pointA, Vector3 pointB, Vector3 pointP)
    {
        float discriminant = pointA.DistanceSquaredTo(pointB) * pointA.DistanceSquaredTo(pointP)
            - Mathf.Pow((pointB - pointA).Dot(pointP - pointA), 2.0f);
        if (discriminant <= 0)
        {
            return 0;
        }

        if (k == 0)
        {
            if (i == 1)
            {
                return Mathf.Log((pointB.DistanceTo(pointA) * pointB.DistanceTo(pointP)
                        + (pointA - pointB).Dot(pointP - pointB))
                    / (pointA.DistanceTo(pointB) * pointA.DistanceTo(pointP) -
                        (pointB - pointA).Dot(pointP - pointA)));
            }

            if (i == 2)
            {
                var inverseOfSquareOfDiscriminant = 1.0f / Mathf.Sqrt(discriminant);
                return Mathf.Atan((pointA - pointB).Dot(pointP - pointB)
                        * inverseOfSquareOfDiscriminant) +
                    Mathf.Atan((pointB - pointA).Dot(pointP - pointA) * inverseOfSquareOfDiscriminant) *
                    pointA.DistanceTo(pointB)
                    * inverseOfSquareOfDiscriminant;
            }

            return pointA.DistanceTo(pointB) / (i - 2) / discriminant
                * ((i - 3) * pointA.DistanceTo(pointB) *
                    Convolution(0, i - 2, pointA, pointB, pointP)
                    + (pointA - pointB).Dot(pointP - pointB) /
                    Mathf.Pow(pointB.DistanceTo(pointP), i - 2)
                    + (pointB - pointA).Dot(pointP - pointA) /
                    Mathf.Pow(pointA.DistanceTo(pointP), i - 2));
        }

        if (k == 1)
        {
            if (i == 2)
            {
                return (pointB - pointA).Dot(pointP - pointA) /
                    pointA.DistanceSquaredTo(pointB) * Convolution(0, 2, pointA, pointB, pointP)
                    + Mathf.Log(pointB.DistanceTo(pointP) / pointA.DistanceTo(pointP))
                    / pointA.DistanceTo(pointB);
            }

            return (pointB - pointA).Dot(pointP - pointA) /
                pointA.DistanceSquaredTo(pointB) * Convolution(0, i, pointA, pointB, pointP)
                + (Mathf.Pow(pointB.DistanceTo(pointP), 2 - i)
                    - Mathf.Pow(pointA.DistanceTo(pointP), 2 - i)) /
                pointA.DistanceTo(pointB) / (2 - i);
        }

        if (k == i - 1)
        {
            return (pointB - pointA).Dot(pointP - pointA) / pointA.DistanceSquaredTo(pointB)
                * Convolution(i - 2, i, pointA, pointB, pointP)
                + Convolution(i - 3, i - 2, pointA, pointB, pointP)
                / (pointA - pointB).LengthSquared() + Mathf.Pow(pointB.DistanceTo(pointP), 2 - i) / (2 - i)
                / pointA.DistanceTo(pointB);
        }

        return (float)(i - 2 * k) / (i - k - 1) * (pointB - pointA).Dot(pointP - pointA)
            / (pointA - pointB).LengthSquared() * Convolution(k - 2, i, pointA, pointB, pointP)
            - Mathf.Pow(pointB.DistanceTo(pointP), 2 - 1) / pointA.DistanceTo(pointB) / (i - k - 1);
    }
}
