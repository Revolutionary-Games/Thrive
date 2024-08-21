using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Mathematic function based on convolution surfaces. Uses bones, each defined by two metaballs, to calculate value
///   of an arbitrary point in space.
/// </summary>
public class Scalis : IMeshGeneratingFunction
{
    public float CutoffPointMultiplier = 1.5f;

    public float InnerCutoffPointMultiplier = 2.0f;

    public bool Cutoff = true;

    private static int[] coefficients = { 1, 2, 1 };

    /// <summary>
    ///   Every metaball represents a point in creature's skeleton.
    ///   Each bone is formed between a point and its parent.
    /// </summary>
    private readonly IReadOnlyCollection<MulticellularMetaball> points;

    private float surfaceValue = 1.0f;

    public Scalis(IReadOnlyCollection<MulticellularMetaball> metaballs)
    {
        points = metaballs;
    }

    public float SurfaceValue
    {
        get => surfaceValue;
        set => surfaceValue = value;
    }

    public float GetValue(Vector3 pos)
    {
        const float sigma = 1.0f;

        float value = 0.0f;

        pos /= sigma;

        const int i = 3;

        var boneDistances = new float[points.Count - (points.Count == 1? 0 : 1)];

        // "Additive value" is a hack that allows us to approximate close-by bones "fusing" together, allowing a more
        // precise cutoff.
        float additiveValue = 0.0f;

        if (Cutoff)
        {
            if (points.Count == 1)
            {
                boneDistances[0] = SquareCutoffValue(pos, points.First().Position / sigma, points.First().Position
                        / sigma);
            }
            else
            {
                int j = 0;
                foreach (var point in points)
                {
                    if (point.Parent == null)
                        continue;

                    boneDistances[j] = SquareCutoffValue(pos, point.Position / sigma, point.Parent.Position
                        / sigma);
                    additiveValue += 0.75f / (boneDistances[j] * Mathf.Max(point.Radius, point.Parent.Radius));

                    float minRadius = Mathf.Min(point.Radius, point.Parent.Radius);

                    if (minRadius > 0.5f && boneDistances[j] < InnerCutoffPointMultiplier * minRadius / SurfaceValue)
                    {
                        return 10.0f;
                    }

                    ++j;
                }
            }
        }

        int j1 = 0;
        foreach (var point in points)
        {
            if (point.Parent == null && points.Count > 1)
                continue;

            float aRadius = point.Radius;
            float bRadius;

            if (points.Count == 1)
            {
                bRadius = point.Radius;
            }
            else
            {
                bRadius = point.Parent!.Radius;
            }

            if (Cutoff && boneDistances[j1] - additiveValue > Mathf.Pow(Mathf.Max(aRadius, bRadius)
                    * CutoffPointMultiplier * 2.0f / SurfaceValue, 2.0f))
            {
                ++j1;
                continue;
            }

            Vector3 a = point.Position / sigma;
            Vector3 b;

            if (points.Count == 1)
            {
                b = point.Position / sigma;
                a.X -= aRadius;
                b.X += aRadius;
            }
            else
            {
                b = point.Parent!.Position / sigma;
            }

            float tau0 = aRadius > bRadius ? bRadius : aRadius;
            float deltaTau = Mathf.Abs(aRadius - bRadius);

            if (aRadius > bRadius)
            {
                (a, b) = (b, a);
            }

            // Since i is always 3, we can cut down on some calculations and make static coeff array
            // var coeff = BinomialExpansion(i - 1);

            for (int k = 0; k < coefficients.Length; k++)
            {
                value += coefficients[k] * Mathf.Pow(deltaTau, k) * Mathf.Pow(tau0, i - k - 1)
                    * Convolution(k, i, a, b, pos);
            }

            ++j1;
        }

        return value / NormalizationFactor(i, sigma);
    }

    public Color GetColour(Vector3 pos)
    {
        if (points == null)
            return Colors.Black;

        if (points.Count == 1)
            return points.First().Colour;

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

        colourSum.A = 0.0f;

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
            throw new Exception("i is less that 0");
        }

        return sigma * sigma * (i - 3) / (i - 2) * NormalizationFactor(i - 2, sigma);
    }

    private List<int> BinomialExpansion(int n)
    {
        var coeff = new List<int>(n + 1);
        coeff.Add(1);
        for (int i = 0; i < n; i++)
        {
            coeff.Add(coeff[i] * (n - i) / (i + 1));
        }

        return coeff;
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

    private float SquareCutoffValue(Vector3 pos, Vector3 a, Vector3 b)
    {
        return (ClosestPoint(pos, a, b) - pos).LengthSquared();
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
                return Mathf.Atan((pointA - pointB).Dot(pointP - pointB)
                        / Mathf.Sqrt(discriminant)) +
                    Mathf.Atan((pointB - pointA).Dot(pointP - pointA) /
                        Mathf.Sqrt(discriminant)) * pointA.DistanceTo(pointB)
                    / Mathf.Sqrt(discriminant);
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
