using System;

/// <summary>
///   Properties of a compound in a given <see cref="BiomeConditions"/>. Has info on how common / available the
///   compound is.
/// </summary>
public struct BiomeCompoundProperties : IEquatable<BiomeCompoundProperties>
{
    /// <summary>
    ///   How much compound there is in each spawned cloud (<see cref="CompoundCloudSystem"/>) of this type
    /// </summary>
    public float Amount;

    /// <summary>
    ///   How often clouds of this compound spawn. Higher density means clouds of this type are more likely to spawn.
    /// </summary>
    public float Density;

    /// <summary>
    ///   When not zero, this says how much of this compound there is available as an environmental compound uniformly
    ///   distributed (available all around in this biome for all cells to use). Usually if this is non-zero
    ///   <see cref="Amount"/> and <see cref="Density"/> are zero.
    /// </summary>
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

    public override string ToString()
    {
        return $"Amount: {Amount} ({Density} density), ambient: {Ambient}";
    }
}
