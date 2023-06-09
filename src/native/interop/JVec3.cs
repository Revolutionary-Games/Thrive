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
        X = vector.x;
        Y = vector.y;
        Z = vector.z;
    }

    public static implicit operator Vector3(JVec3 d) => new((float)d.X, (float)d.Y, (float)d.Z);
}

[StructLayout(LayoutKind.Sequential)]
public struct JQuat
{
    public static JQuat Identity = new() { X = 0, Y = 0, Z = 0, W = 1 };

    public float X;
    public float Y;
    public float Z;
    public float W;

    public JQuat(Quat quat)
    {
        X = quat.x;
        Y = quat.y;
        Z = quat.z;
        W = quat.w;
    }

    public static implicit operator Quat(JQuat d) => new(d.X, d.Y, d.Z, d.W);
}

[StructLayout(LayoutKind.Sequential)]
public struct JVecF3
{
    public float X;
    public float Y;
    public float Z;

    public JVecF3(Vector3 vector)
    {
        X = vector.x;
        Y = vector.y;
        Z = vector.z;
    }

    public JVecF3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static implicit operator Vector3(JVecF3 d) => new(d.X, d.Y, d.Z);
}
