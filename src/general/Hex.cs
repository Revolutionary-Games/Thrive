﻿using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   2D axial coordinate pair.
///   As well as some helper functions for converting to cartesian
/// </summary>
public struct Hex : IEquatable<Hex>
{
    /// <summary>
    ///   Maps a hex side to its direct opposite
    /// </summary>
    public static readonly Dictionary<HexSide, HexSide> OppositeHexSide =
        new Dictionary<HexSide, HexSide>
        {
            { HexSide.TOP, HexSide.BOTTOM },
            { HexSide.TOP_RIGHT, HexSide.BOTTOM_LEFT },
            { HexSide.BOTTOM_RIGHT, HexSide.TOP_LEFT },
            { HexSide.BOTTOM, HexSide.TOP },
            { HexSide.BOTTOM_LEFT, HexSide.TOP_RIGHT },
            { HexSide.TOP_LEFT, HexSide.BOTTOM_RIGHT },
        };

    /// <summary>
    ///   Each hex has six neighbours, one for each side. This table
    ///   maps the hex side to the coordinate offset of the neighbour
    ///   adjacent to that side.
    /// </summary>
    public static readonly Dictionary<HexSide, Hex> HexNeighbourOffset =
        new Dictionary<HexSide, Hex>
        {
            { HexSide.TOP, new Hex(0, 1) },
            { HexSide.TOP_RIGHT, new Hex(1, 0) },
            { HexSide.BOTTOM_RIGHT, new Hex(1, -1) },
            { HexSide.BOTTOM, new Hex(0, -1) },
            { HexSide.BOTTOM_LEFT, new Hex(-1, 0) },
            { HexSide.TOP_LEFT, new Hex(-1, 1) },
        };

    public int Q;
    public int R;

    public Hex(int q, int r)
    {
        Q = q;
        R = r;
    }

    /// <summary>
    ///   Enumeration of the hex sides, clock-wise
    /// </summary>
    public enum HexSide
    {
        /// <summary>
        ///   Directly up
        /// </summary>
        TOP = 1,

        /// <summary>
        ///   Up and to the right
        /// </summary>
        TOP_RIGHT = 2,

        /// <summary>
        ///   Down and to the right
        /// </summary>
        BOTTOM_RIGHT = 3,

        /// <summary>
        ///   Directly down
        /// </summary>
        BOTTOM = 4,

        /// <summary>
        ///   Down and left
        /// </summary>
        BOTTOM_LEFT = 5,

        /// <summary>
        ///   Up and left
        /// </summary>
        TOP_LEFT = 6,
    }

    public static Hex operator +(Hex a, Hex b)
    {
        return new Hex(a.Q + b.Q, a.R + b.R);
    }

    public static Hex operator -(Hex a, Hex b)
    {
        return new Hex(a.Q - b.Q, a.R - b.R);
    }

    public static Hex operator *(Hex a, int b)
    {
        return new Hex(a.Q * b, a.R * b);
    }

    public static bool operator ==(Hex left, Hex right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Hex left, Hex right)
    {
        return !(left == right);
    }

    /// <summary>
    ///   Converts axial hex coordinates to cartesian coordinates.
    /// </summary>
    /// <returns>Cartesian coordinates of the hex's center.</returns>
    public static Vector3 AxialToCartesian(Hex hex)
    {
        float x = hex.Q * Constants.DEFAULT_HEX_SIZE * 3.0f / 2.0f;
        float z = Constants.DEFAULT_HEX_SIZE * Mathf.Sqrt(3) * (hex.R + hex.Q / 2.0f);
        return new Vector3(x, 0, z);
    }

    /// <summary>
    ///   Converts cartesian coordinates to axial hex coordinates.
    /// </summary>
    /// <returns>Hex position.</returns>
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
    /// <returns>hex coordinates.</returns>
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
    public static Hex RotateAxial(Hex hex)
    {
        return new Hex(-hex.R, hex.Q + hex.R);
    }

    /// <summary>
    ///   Rotates a hex by (60 * n) degrees about the origin clock-wise.
    /// </summary>
    public static Hex RotateAxialNTimes(Hex original, int n)
    {
        Hex result = original;

        for (int i = 0; i < n % 6; i++)
        {
            result = RotateAxial(result);
        }

        return result;
    }

    /// <summary>
    ///   Symmetrizes a hex horizontally about the (0,x) axis.
    /// </summary>
    public static Hex FlipHorizontally(Hex hex)
    {
        return new Hex(-hex.Q, hex.Q + hex.R);
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Hex))
            return false;

        return Equals((Hex)obj);
    }

    public bool Equals(Hex other)
    {
        return Q == other.Q && R == other.R;
    }

    public override int GetHashCode()
    {
        var hashCode = -1997189103;

        // ReSharper disable NonReadonlyMemberInGetHashCode
        hashCode = (hashCode * -1521134295) + Q.GetHashCode();
        hashCode = (hashCode * -1521134295) + R.GetHashCode();

        // ReSharper restore NonReadonlyMemberInGetHashCode
        return hashCode;
    }

    public override string ToString()
    {
        return Q + ", " + R;
    }
}
