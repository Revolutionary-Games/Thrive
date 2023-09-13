using System;

// ReSharper disable InconsistentNaming
/// <summary>
///   Just a basic 3 component integer vector for use before we get Godot.Vector3i
/// </summary>
public struct Int3 : IEquatable<Int3>
{
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
    public int x;
    public int y;
    public int z;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

    public Int3(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static bool operator ==(Int3 left, Int3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Int3 left, Int3 right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Int3 other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return x ^ y ^ z;
    }

    public bool Equals(Int3 other)
    {
        return x == other.x && y == other.y && z == other.z;
    }
}
