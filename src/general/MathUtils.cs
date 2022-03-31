using System;
using System.Linq;
using Godot;

/// <summary>
///   Math related utility functions for Thrive
/// </summary>
public static class MathUtils
{
    public const float EPSILON = 0.00000001f;
    public const float DEGREES_TO_RADIANS = Mathf.Pi / 180;
    public const double FULL_CIRCLE = Math.PI * 2;

    public static T Clamp<T>(this T val, T min, T max)
        where T : IComparable<T>
    {
        if (val.CompareTo(min) < 0)
        {
            return min;
        }

        if (val.CompareTo(max) > 0)
        {
            return max;
        }

        return val;
    }

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
    ///     Rotation is now the number of 60 degree rotations
    ///   </para>
    /// </remarks>
    public static Quat CreateRotationForOrganelle(float rotation)
    {
        return new Quat(new Vector3(0, -1, 0), rotation * 60 * DEGREES_TO_RADIANS);
    }

    /// <summary>
    ///   This still takes the angle in degrees as this is used from
    ///   places that calculate the angle in degrees.
    /// </summary>
    public static Quat CreateRotationForExternal(float angle)
    {
        return new Quat(new Vector3(0, 1, 0), 180 * DEGREES_TO_RADIANS) *
            new Quat(new Vector3(0, 1, 0), angle * DEGREES_TO_RADIANS);
    }

    /// <summary>
    ///   Rotation for the pilus physics cone
    /// </summary>
    public static Quat CreateRotationForPhysicsOrganelle(float angle)
    {
        return new Quat(new Vector3(-1, 0, 0), 90 * DEGREES_TO_RADIANS) *
            new Quat(new Vector3(0, 0, -1), (180 - angle) * DEGREES_TO_RADIANS);
    }

    /// <summary>
    ///   Returns a Lerped value, and snaps to the target value if current and target
    ///   value is approximately equal by the specified tolerance value.
    /// </summary>
    public static float Lerp(float from, float to, float weight, float tolerance = Mathf.Epsilon)
    {
        if (Mathf.IsEqualApprox(from, to, tolerance))
            return to;

        return Mathf.Lerp(from, to, weight);
    }

    /// <summary>
    ///   Standard modulo for negative values in C# produces negative results.
    ///   This function returns modulo values between 0 and mod-1.
    /// </summary>
    /// <returns>The positive modulo</returns>
    public static int PositiveModulo(this int val, int mod)
    {
        int result = val % mod;
        return (result < 0) ? result + mod : result;
    }

    public static int Factorial(int f)
    {
        if (f == 0)
            return 1;

        return f * Factorial(f - 1);
    }

    public static int NCr(int n, int r)
    {
        return NCr(n, r, Factorial(n));
    }

    /// <remarks>
    ///   <para>
    ///     Use this overload if you have already calculated the factorial of n
    ///   </para>
    /// </remarks>
    public static int NCr(int n, int r, int nFactorial)
    {
        return nFactorial / (Factorial(r) * Factorial(n - r));
    }

    /// <summary>
    ///   Calculates binomial values.
    /// </summary>
    /// <param name="n">number of trials</param>
    /// <param name="p">success probability for each trial</param>
    /// <returns>An array of the binomial values of length n</returns>
    /// <remarks>
    ///   <para>
    ///     Learn about the binomial distribution
    ///     <a href="https://www.investopedia.com/terms/b/binomialdistribution.asp">here</a>.
    ///     Tl;dr: Binomial values are a series of numbers calculated from an amount "n" and a probability "p".
    ///     n defines the amount of possible outcomes and p defines the "probability" of a higher result.
    ///     A n of 4 and a p of 0.5 produces these values: [0.0625, 0.25, 0.375, 0.25, 0.0625].
    ///     The array means that there is a 6.25% chance of the result 0, 25% of the result 1, etc...
    ///     The numbers calculate up to 1.
    ///   </para>
    /// </remarks>
    public static float[] BinomialValues(int n, float p)
    {
        var nFactorial = Factorial(n);

        return Enumerable.Range(0, n).Select(k =>
            {
                // Pr(X = k) = nCr(n, k) * p^k * (1 - p)^(n - k)
                float result = NCr(n, k, nFactorial);
                result *= Mathf.Pow(p, k);
                result *= Mathf.Pow(1 - p, n - k);
                return result;
            })
            .ToArray();
    }
}
