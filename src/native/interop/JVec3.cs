using System;
using System.Runtime.InteropServices;
using Godot;

// This file has all of the interop structs
// This is named after the first one to avoid having to have a bunch of small files for everything

[StructLayout(LayoutKind.Sequential)]
public struct JVec3
{
    public double X;
    public double Y;
    public double Z;

    public JVec3(Vector3 vector)
    {
        X = vector.X;
        Y = vector.Y;
        Z = vector.Z;
    }

    public static implicit operator Vector3(JVec3 d)
    {
        return new Vector3((float)d.X, (float)d.Y, (float)d.Z);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct JQuat
{
    public static JQuat Identity = new() { X = 0, Y = 0, Z = 0, W = 1 };

    public float X;
    public float Y;
    public float Z;
    public float W;

    public JQuat(Quaternion quat)
    {
        X = quat.X;
        Y = quat.Y;
        Z = quat.Z;
        W = quat.W;
    }

    public static implicit operator Quaternion(JQuat d)
    {
        return new Quaternion(d.X, d.Y, d.Z, d.W);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct JVecF3 : IEquatable<JVecF3>
{
    public float X;
    public float Y;
    public float Z;

    public JVecF3(Vector3 vector)
    {
        X = vector.X;
        Y = vector.Y;
        Z = vector.Z;
    }

    public JVecF3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static implicit operator Vector3(JVecF3 d)
    {
        return new Vector3(d.X, d.Y, d.Z);
    }

    public static bool operator ==(JVecF3 left, JVecF3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(JVecF3 left, JVecF3 right)
    {
        return !left.Equals(right);
    }

    public static int GetCompatibleHashCode(float x, float y, float z)
    {
        unchecked
        {
            var hashCode = x.GetHashCode();
            hashCode = hashCode * 397 ^ y.GetHashCode();
            hashCode = hashCode * 401 ^ z.GetHashCode();
            return hashCode;
        }
    }

    public bool Equals(JVecF3 other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    }

    public override bool Equals(object? obj)
    {
        return obj is JVecF3 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return GetCompatibleHashCode(X, Y, Z);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct JColour
{
    public float R;
    public float G;
    public float B;
    public float A;

    public JColour(Color color)
    {
        R = color.R;
        G = color.G;
        B = color.B;
        A = color.A;
    }

    public JColour(float r, float g, float b, float a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static implicit operator Color(JColour d)
    {
        return new Color(d.R, d.G, d.B, d.A);
    }
}

/// <summary>
///   Sub-shape data for the native side methods
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SubShapeDefinition
{
    public JQuat Rotation;

    public JVecF3 Position;

    public uint UserData;

    public IntPtr ShapeNativePtr;

    public SubShapeDefinition(Vector3 position, Quaternion rotation, IntPtr shapePtr, uint userData = 0)
    {
        Position = new JVecF3(position);
        Rotation = new JQuat(rotation);
        ShapeNativePtr = shapePtr;
        UserData = userData;
    }
}
