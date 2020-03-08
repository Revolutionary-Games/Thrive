// This class is borrowed from the guy below, as long as we don't take credit
// and modify it, we're allowed to use it. This code is under GPL v3 Copyright
// 2012 Sol from www.solarianprogrammer.com

// THIS CLASS IS A TRANSLATION TO C++11 FROM THE REFERENCE
// JAVA IMPLEMENTATION OF THE IMPROVED PERLIN FUNCTION (see
// http://mrl.nyu.edu/~perlin/noise/) THE ORIGINAL JAVA IMPLEMENTATION IS
// COPYRIGHT 2002 KEN PERLIN

// I ADDED AN EXTRA METHOD THAT GENERATES A NEW PERMUTATION VECTOR (THIS IS NOT
// PRESENT IN THE ORIGINAL IMPLEMENTATION)
// Rewritten for C# by Henri Hyyryläinen 2020-03-08

using System;

public class PerlinNoise
{
    /// <summary>
    ///   The permutation vector
    /// </summary>
    private int[] p;

    /// <summary>
    ///   Generate a new permutation vector based on the value of seed
    /// </summary>
    public PerlinNoise(int seed)
    {
        var temp = new int[256];

        Random random = new Random(seed);

        // Fill temp with values from 0 to 255
        for (int i = 0; i < 256; ++i)
        {
            temp[i] = random.Next(0, 256);
        }

        // Copy the temp twice to p
        p = new int[256 * 2];

        for (int i = 0; i < 256 * 2; ++i)
        {
            p[i] = temp[i / 2];
        }
    }

    /// <summary>
    ///   Get a noise value, for 2D images z can have any value
    /// </summary>
    public double Noise(double x, double y, double z)
    {
        // Find the unit cube that contains the point
        int x1 = (int)Math.Floor(x) & 255;
        int y1 = (int)Math.Floor(y) & 255;
        int z1 = (int)Math.Floor(z) & 255;

        // Find relative x, y,z of point in cube
        x -= Math.Floor(x);
        y -= Math.Floor(y);
        z -= Math.Floor(z);

        // Compute fade curves for each of x, y, z
        double u = Fade(x);
        double v = Fade(y);
        double w = Fade(z);

        // Hash coordinates of the 8 cube corners
        int a = p[x1] + y1;
        int aA = p[a] + z1;
        int aB = p[a + 1] + z1;
        int b = p[x1 + 1] + y1;
        int bA = p[b] + z1;
        int bB = p[b + 1] + z1;

        // Add blended results from 8 corners of cube
        double res = Lerp(w,
            Lerp(v, Lerp(u, Grad(p[aA], x, y, z), Grad(p[bA], x - 1, y, z)),
                Lerp(u, Grad(p[aB], x, y - 1, z), Grad(p[bB], x - 1, y - 1, z))),
            Lerp(v,
                Lerp(u, Grad(p[aA + 1], x, y, z - 1),
                    Grad(p[bA + 1], x - 1, y, z - 1)),
                Lerp(u, Grad(p[aB + 1], x, y - 1, z - 1),
                    Grad(p[bB + 1], x - 1, y - 1, z - 1))));
        return (res + 1.0) / 2.0;
    }

    private double Fade(double t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private double Lerp(double t, double a, double b)
    {
        return a + t * (b - a);
    }

    private double Grad(int hash, double x, double y, double z)
    {
        int h = hash & 15;

        // Convert lower 4 bits of hash inot 12 gradient directions
        double u = h < 8 ? x : y, v = h < 4 ? y : h == 12 || h == 14 ? x : z;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
