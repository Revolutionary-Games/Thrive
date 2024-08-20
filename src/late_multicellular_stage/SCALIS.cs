using System;
using System.Collections.Generic;
using Godot;

public class Scalis : IMeshGeneratingFunction
{
    public float CutoffPointMultiplier = 1.5f;

    public float InnerCutoffPointMultiplier = 2.0f;

    public bool Cutoff = true;

    /// <summary>
    ///   Every two consecutive points form a bone
    /// </summary>
    public Metaball[]? Points;

    private static int[] coefficients = new int[] { 1, 2, 1 };

    private float surfaceValue = 1.0f;

    public float SurfaceValue
    {
        get => surfaceValue;
        set => surfaceValue = value;
    }

    public void FindBones(IReadOnlyCollection<MulticellularMetaball> layout)
    {
        var newPoints = new List<Metaball>();

        foreach (var metaball in layout)
        {
            if (metaball.Parent == null)
                continue;

            newPoints.Add(metaball);
            newPoints.Add(metaball.Parent);
        }

        Points = newPoints.ToArray();
    }

    public float GetValue(Vector3 pos)
    {
        const float sigma = 1.0f;

        float value = 0.0f;

        pos /= sigma;

        const int i = 3;

        var boneDistances = new float[Points!.Length / 2];

        // "Additive value" is a hack that allows us to approximate close-by bones "fusing" together, allowing a more
        // precise cutoff.
        float additiveValue = 0.0f;

        if (Cutoff)
        {
            for (int j = 0; j < Points.Length; j += 2)
            {
                boneDistances[j / 2] = SquareCutoffValue(pos, Points[j].Position / sigma, Points[j + 1].Position
                    / sigma);
                additiveValue += 0.75f / (boneDistances[j / 2] * Mathf.Max(Points[j].Radius, Points[j + 1].Radius));

                float minRadius = Mathf.Min(Points[j].Radius, Points[j + 1].Radius);

                if (minRadius > 0.5f && boneDistances[j / 2] < InnerCutoffPointMultiplier * minRadius / SurfaceValue)
                {
                    return 10.0f;
                }
            }
        }

        for (int j = 0; j < Points.Length; j += 2)
        {
            float aRadius = Points[j].Radius;
            float bRadius = Points[j + 1].Radius;

            if (Cutoff && boneDistances[j / 2] - additiveValue > Mathf.Pow(Mathf.Max(aRadius, bRadius)
                    * CutoffPointMultiplier * 2.0f / SurfaceValue, 2.0f))
            {
                continue;
            }

            Vector3 a = Points[j].Position / sigma;
            Vector3 b = Points[j + 1].Position / sigma;

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
                value += coefficients[k] * Mathf.Pow(deltaTau, k) * Mathf.Pow(tau0, i - k - 1) * Convolution(k, i, a, b, pos);
            }
        }

        return value / NormalizationFactor(i, sigma);
    }

    public Color GetColour(Vector3 pos)
    {
        if (Points == null)
            return Colors.Black;

        if (Points.Length == 1)
            return Points[0].Colour;

        Color colourSum = Colors.Black;

        float contributionSum = 0.0f;

        for (int j = 0; j < Points.Length; j += 2)
        {
            float abDistanceSquared = Points[j].Position.DistanceSquaredTo(Points[j + 1].Position);

            Vector3 linePos = ClosestPoint(pos, Points[j].Position, Points[j + 1].Position);

            float contribution = 1.0f / linePos.DistanceSquaredTo(pos);

            contributionSum += contribution;

            colourSum += Points[j].Colour.Lerp(Points[j + 1].Colour, Points[j].Position.DistanceSquaredTo(linePos)
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

    private Vector3 VectorFromTo(Vector3 from, Vector3 to)
    {
        return to - from;
    }

    private float Convolution(int k, int i, Vector3 pointA, Vector3 pointB, Vector3 pointP)
    {
        float discriminant = pointA.DistanceSquaredTo(pointB) * pointA.DistanceSquaredTo(pointP)
            - Mathf.Pow(VectorFromTo(pointA, pointB).Dot(VectorFromTo(pointA, pointP)), 2.0f);
        if (discriminant <= 0)
        {
            return 0;
        }

        if (k == 0)
        {
            if (i == 1)
            {
                return Mathf.Log((pointB.DistanceTo(pointA) * pointB.DistanceTo(pointP)
                        + VectorFromTo(pointB, pointA).Dot(VectorFromTo(pointB, pointP)))
                    / (pointA.DistanceTo(pointB) * pointA.DistanceTo(pointP) -
                        VectorFromTo(pointA, pointB).Dot(VectorFromTo(pointA, pointP))));
            }

            if (i == 2)
            {
                return Mathf.Atan(VectorFromTo(pointB, pointA).Dot(VectorFromTo(pointB, pointP))
                        / Mathf.Sqrt(discriminant)) + Mathf.Atan(VectorFromTo(pointA, pointB).Dot(VectorFromTo(pointA, pointP)) /
                        Mathf.Sqrt(discriminant)) * pointA.DistanceTo(pointB)
                    / Mathf.Sqrt(discriminant);
            }

            return pointA.DistanceTo(pointB) / (i - 2) / discriminant
                * ((i - 3) * pointA.DistanceTo(pointB) *
                    Convolution(0, i - 2, pointA, pointB, pointP)
                    + VectorFromTo(pointB, pointA).Dot(VectorFromTo(pointB, pointP)) /
                    Mathf.Pow(pointB.DistanceTo(pointP), i - 2)
                    + VectorFromTo(pointA, pointB).Dot(VectorFromTo(pointA, pointP)) /
                    Mathf.Pow(pointA.DistanceTo(pointP), i - 2));
        }

        if (k == 1)
        {
            if (i == 2)
            {
                return VectorFromTo(pointA, pointB).Dot(VectorFromTo(pointA, pointP)) /
                    pointA.DistanceSquaredTo(pointB) * Convolution(0, 2, pointA, pointB, pointP)
                    + Mathf.Log(pointB.DistanceTo(pointP) / pointA.DistanceTo(pointP))
                    / pointA.DistanceTo(pointB);
            }

            return VectorFromTo(pointA, pointB).Dot(VectorFromTo(pointA, pointP)) /
                pointA.DistanceSquaredTo(pointB) * Convolution(0, i, pointA, pointB, pointP)
                + (Mathf.Pow(pointB.DistanceTo(pointP), 2 - i)
                    - Mathf.Pow(pointA.DistanceTo(pointP), 2 - i)) /
                pointA.DistanceTo(pointB) / (2 - i);
        }

        if (k == i - 1)
        {
            return VectorFromTo(pointA, pointB).Dot(VectorFromTo(pointA, pointP)) / pointA.DistanceSquaredTo(pointB)
                * Convolution(i - 2, i, pointA, pointB, pointP)
                + Convolution(i - 3, i - 2, pointA, pointB, pointP)
                / (pointA - pointB).LengthSquared() + Mathf.Pow(pointB.DistanceTo(pointP), 2 - i) / (2 - i)
                / pointA.DistanceTo(pointB);
        }

        return (float)(i - 2 * k) / (i - k - 1) * VectorFromTo(pointA, pointB).Dot(VectorFromTo(pointA, pointP))
            / (pointA - pointB).LengthSquared() * Convolution(k - 2, i, pointA, pointB, pointP)
            - Mathf.Pow(pointB.DistanceTo(pointP), 2 - 1) / pointA.DistanceTo(pointB) / (i - k - 1);
    }
}
