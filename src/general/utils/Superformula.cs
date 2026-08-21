using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
///   A simplified instance of a cached Superformula evaluator.
/// </summary>
public readonly struct Superformula
{
    private const double FiniteDifferenceEpsilon = 10e-4;
    private const double FiniteDifferenceEpsilonSquared = FiniteDifferenceEpsilon * FiniteDifferenceEpsilon;
    private const double MaxStepSize = double.Pi / 32.0;
    private const double MinStepSize = double.Pi / 256.0;

    // The threshold to make the algorithm suspect we have skipped an infinite (or very high) curvature point.
    // Maybe we should use a more rigorous value, as this is purely empirical.
    private const double SuspiciousStepThreshold = 0.1;

    private readonly (double Rho, double Theta)[] polarCache;
    private readonly List<(double X, double Y)> cartesianCache;

    public Superformula(int n1, int n2, int n3, int m)
    {
        var samples = new List<(double, double)>();

        double theta = -double.Pi;
        double previousTheta = double.NaN;
        double previousRho = double.NaN;
        while (theta < double.Pi)
        {
            double rho = EvalDirect(theta, n1, n2, n3, m);
            double currentTheta = theta;

            // A bisection method to increment the precision around a high curvature or non-differentiable point.
            // This won't yield a good result if in the current interval there are multiple such points, but this case
            // should be too rare to even be considered.
            while (Math.Abs(rho - previousRho) >= SuspiciousStepThreshold)
            {
                // We have probably skipped a discontinuity in the first derivative, aka a high curvature point, so we
                // want to increase the precision around here.

                double newTheta = (theta + previousTheta) / 2.0;

                if (Math.Abs(theta - previousTheta) < MinStepSize)
                {
                    // We are below the required tolerance, so let's break out of this loop.
                    // First we need to revert to the theta value before the loop.
                    theta = currentTheta;

                    break;
                }

                double newRho = EvalDirect(newTheta, n1, n2, n3, m);

                if (Math.Abs(newRho - previousRho) >= SuspiciousStepThreshold)
                {
                    // We are still past the suspected non-differentiable point, so let's make this the actual current
                    // theta.
                    theta = newTheta;
                    rho = newRho;

                    samples.Add((rho, theta));

                    continue;
                }

                // The last case is if we are now behind the suspected non-differentiable point, so we make this the
                // previous theta.
                previousTheta = theta;
                previousRho = rho;

                samples.Add((rho, theta));
            }

            samples.Add((rho, theta));

            // Use the curvature radius as a dynamic step.
            double kappa = Curvature(theta, n1, n2, n3, m);
            double step = double.IsNaN(kappa) || kappa <= 0.0001 ? MaxStepSize : 1.0 / kappa;
            theta += Math.Clamp(step, MinStepSize, MaxStepSize);

            previousRho = rho;
            previousTheta = theta;

            if (theta > double.Pi && previousTheta < double.Pi)
            {
                theta = double.Pi;
            }
        }

        // Ensure all the samples are ordered wrt theta.
        samples.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        polarCache = samples.ToArray();

        // Convert to cartesian in-place and accumulate curve length.
        double dx, dy;
        for (int i = 0; i < samples.Count; ++i)
        {
            (double Rho, double Theta) polar = samples[i];
            samples[i] = PolarToCartesian(polar.Rho, polar.Theta);

            if (i <= 0)
                continue;

            (double X, double Y) currentPoint = samples[i];
            (double X, double Y) previousPoint = samples[i - 1];

            dx = currentPoint.X - previousPoint.X;
            dy = currentPoint.Y - previousPoint.Y;

            Length += Math.Sqrt(dx * dx + dy * dy);
        }

        (double X, double Y) firstPoint = samples[0];
        (double X, double Y) lastPoint = samples[^1];

        dx = firstPoint.X - lastPoint.X;
        dy = firstPoint.Y - lastPoint.Y;

        Length += Math.Sqrt(dx * dx + dy * dy);

        cartesianCache = samples;
    }

    public ReadOnlySpan<(double X, double Y)> CartesianData => CollectionsMarshal.AsSpan(cartesianCache);
    public ReadOnlySpan<(double Rho, double Theta)> PolarData => polarCache;

    public double Length { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double EvalDirect(double theta, int n1, int n2, int n3, int m)
    {
        double cos = Math.Cos(theta * m / 4.0);
        double sin = Math.Sin(theta * m / 4.0);

        cos = Math.Pow(Math.Abs(cos), n2);
        sin = Math.Pow(Math.Abs(sin), n3);

        double value = Math.Pow(cos + sin, 1 / (double)n1);

        return 1 / value;
    }

    /// <summary>
    ///   Finite difference method to numerically approximate curvature.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Curvature(double theta, int n1, int n2, int n3, int m)
    {
        double radiusCenter = EvalDirect(theta, n1, n2, n3, m);
        double radiusPlus = EvalDirect(theta + FiniteDifferenceEpsilon, n1, n2, n3, m);
        double radiusMinus = EvalDirect(theta - FiniteDifferenceEpsilon, n1, n2, n3, m);

        double firstDerivative = (radiusPlus - radiusMinus) / (2 * FiniteDifferenceEpsilon);
        double secondDerivative = (radiusPlus - 2 * radiusCenter + radiusMinus) / FiniteDifferenceEpsilonSquared;

        double radiusCenterSquared = radiusCenter * radiusCenter;
        double firstDerivativeSquared = firstDerivative * firstDerivative;

        double curvatureNumerator = Math.Abs(radiusCenterSquared + 2 * firstDerivativeSquared -
            radiusCenter * secondDerivative);
        double curvatureDenominator = Math.Pow(radiusCenterSquared + firstDerivativeSquared, 1.5);

        if (curvatureDenominator < FiniteDifferenceEpsilon)
            return double.PositiveInfinity;

        return curvatureNumerator / curvatureDenominator;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (float X, float Y) PolarToCartesian(double rho, double theta)
    {
        double x = rho * Math.Cos(theta);
        double y = rho * Math.Sin(theta);

        return ((float)x, (float)y);
    }
}
