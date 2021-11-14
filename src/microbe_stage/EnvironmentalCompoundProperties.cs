using System;

public struct EnvironmentalCompoundProperties : IEquatable<EnvironmentalCompoundProperties>
{
    public float Amount;
    public float Density;
    public float Dissolved;

    public static bool operator ==(EnvironmentalCompoundProperties left, EnvironmentalCompoundProperties right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EnvironmentalCompoundProperties left, EnvironmentalCompoundProperties right)
    {
        return !(left == right);
    }

    public override bool Equals(object obj)
    {
        if (obj is EnvironmentalCompoundProperties other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return (int)(Amount + Density + Dissolved);
    }

    public bool Equals(EnvironmentalCompoundProperties other)
    {
        return Amount == other.Amount && Density == other.Density && Dissolved == other.Dissolved;
    }

    public void AddDissolved(float dissolved)
    {
        Dissolved += dissolved;
    }
}
