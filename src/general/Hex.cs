using System;
using Godot;

/// <summary>
///   2D axial coordinate pair.
///   As well as some helper functions for converting to cartesian
/// </summary>
public struct Hex
{
    public int Q;
    public int R;

    public Hex(int q, int r)
    {
        Q = q;
        R = r;
    }

    /// <summary>
    ///   Converts axial hex coordinates to cartesian coordinates.
    /// </summary>
    /// <returns>Cartesian coordinates of the hex's center.</return>
    public static Vector3 AxialToCartesian(Hex hex)
    {
        float x = hex.Q * Constants.DEFAULT_HEX_SIZE * 3.0f / 2.0f;
        float z = Constants.DEFAULT_HEX_SIZE * Mathf.Sqrt(3) * (hex.R + hex.Q / 2.0f);
        return new Vector3(x, 0, z);
    }

    /// <summary>
    ///   Converts cartesian coordinates to axial hex coordinates.
    /// </summary>
    /// <returns>Hex position.</return>
    public static Hex CartesianToAxial(Vector3 pos)
    {
        // Getting the cube coordinates.
        float cx = pos.x * (2.0f / 3.0f) / Constants.DEFAULT_HEX_SIZE;
        float cy = pos.z / (Constants.DEFAULT_HEX_SIZE * Mathf.Sqrt(3)) - cx / 2.0f;
        float cz = -(cx + cy);

        // Rounding the result.
        float rx = Mathf.Round(cx);
        float ry = Mathf.Round(cy);
        float rz = Mathf.Round(cz);

        float xDiff = Mathf.Abs(rx - cx);
        float yDiff = Mathf.Abs(ry - cy);
        float zDiff = Mathf.Abs(rz - cz);

        if (xDiff > yDiff && xDiff > zDiff)
        {
            rx = -(ry + rz);
        }
        else if (yDiff > zDiff)
        {
            ry = -(rx + rz);
        }

        // Returning the axial coordinates.
        return CubeToAxial(new Int3((int)rx, (int)ry, (int)rz));
    }

    /// <summary>
    ///   Converts axial hex coordinates to coordinates in the cube based hex model
    /// </summary>
    public static Int3 AxialToCube(Hex hex)
    {
        return new Int3(hex.Q, hex.R, -(hex.Q + hex.R));
    }

    /// <summary>
    ///   Converts cube based hex coordinates to axial hex
    ///   coordinates. Basically just seems to discard the z value.
    /// </summary>
    /// <returns>hex coordinates.</return>
    public static Hex CubeToAxial(Int3 cube)
    {
        return new Hex(cube.x, cube.y);
    }

    /// <summary>
    ///   Correctly rounds fractional hex cube coordinates to the
    ///   correct integer coordinates.
    /// </summary>
    public static Int3 CubeHexRound(Vector3 pos)
    {
        float rx = Mathf.Round(pos.x);
        float ry = Mathf.Round(pos.y);
        float rz = Mathf.Round(pos.z);

        float xDiff = Mathf.Abs(rx - pos.x);
        float yDiff = Mathf.Abs(ry - pos.y);
        float zDiff = Mathf.Abs(rz - pos.z);

        if (xDiff > yDiff && xDiff > zDiff)
        {
            rx = -(ry + rz);
        }
        else if (yDiff > zDiff)
        {
            ry = -(rx + rz);
        }
        else
        {
            rz = -(ry + rx);
        }

        return new Int3((int)rx, (int)ry, (int)rz);
    }

    /// <summary>
    ///   Rotates a hex by 60 degrees about the origin clock-wise.
    /// </summary>
    public static Hex
        RotateAxial(Hex hex)
    {
        return new Hex(-hex.R, hex.Q + hex.R);
    }

    /// <summary>
    ///   Rotates a hex by (60 * n) degrees about the origin clock-wise.
    /// </summary>
    public static Hex
        RotateAxialNTimes(Hex original, int n)
    {
        Hex result = original;

        for (int i = 0; i < n % 6; i++)
            result = RotateAxial(result);

        return result;
    }

    /// <summary>
    ///   Symmetrizes a hex horizontally about the (0,x) axis.
    /// </summary>
    public static Hex
        FlipHorizontally(Hex hex)
    {
        return new Hex(-hex.Q, hex.Q + hex.R);
    }
}
