using System;
using SharedBase.Archive;

/// <summary>
///   Classes implementing this interface store organelle component specific upgrade data
/// </summary>
public interface IComponentSpecificUpgrades : ICloneable, IEquatable<IComponentSpecificUpgrades>, IArchivable
{
    public int GetHashCode();

    /// <summary>
    ///   Generates a hash code for the *visuals* of this upgrade. Many upgrades don't affect the visuals, so they
    ///   return a constant value.
    /// </summary>
    /// <returns>A hash representing the visual state of this upgrade component</returns>
    public ulong GetVisualHashCode();

    /// <summary>
    ///   Calculates the cost of this upgrade in reference to another one. Used to make sure auto-evo doesn't
    ///   overspend MP
    /// </summary>
    /// <param name="previousUpgrades">Previous upgrades (should be the same type) or null</param>
    /// <returns>Cost of the upgrade</returns>
    public double CalculateCost(IComponentSpecificUpgrades? previousUpgrades);
}
