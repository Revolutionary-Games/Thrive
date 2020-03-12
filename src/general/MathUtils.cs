using System;

public static class MathUtils
{
    public static float EPSILON = 0.00000001f;

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
}
