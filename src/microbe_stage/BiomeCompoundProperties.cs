using System;

public struct BiomeCompoundProperties : IEquatable<BiomeCompoundProperties>
{
    public float Amount;
    public float Density;
    public float Ambient;

    public static bool operator ==(BiomeCompoundProperties left, BiomeCompoundProperties right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BiomeCompoundProperties left, BiomeCompoundProperties right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is BiomeCompoundProperties other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return (int)(Amount + Density + Ambient);
    }

    public bool Equals(BiomeCompoundProperties other)
    {
        return Amount == other.Amount && Density == other.Density && Ambient == other.Ambient;
    }
}
