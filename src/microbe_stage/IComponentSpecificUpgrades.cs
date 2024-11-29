using System;

/// <summary>
///   Classes implementing this interface store organelle component specific upgrade data
/// </summary>
public interface IComponentSpecificUpgrades : ICloneable, IEquatable<IComponentSpecificUpgrades>
{
    public int GetHashCode();

    /// <summary>
    ///   Generates a hash code for the *visuals* of this upgrade. Many upgrades don't affect the visuals so they
    ///   return a constant value.
    /// </summary>
    /// <returns>A hash representing the visual state of this upgrade component</returns>
    public ulong GetVisualHashCode();
}
