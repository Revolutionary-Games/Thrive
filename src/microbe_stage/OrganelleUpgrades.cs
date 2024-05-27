using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Upgrades for a placed or template organelle
/// </summary>
public class OrganelleUpgrades : ICloneable, IEquatable<OrganelleUpgrades>
{
    /// <summary>
    ///   A list of "feature" names that have been unlocked for this organelle. Depends on the organelle components
    ///   what names they look for.
    /// </summary>
    public List<string> UnlockedFeatures { get; set; } = new();

    /// <summary>
    ///   Organelle type specific upgrade data. Null if not configured
    /// </summary>
    public IComponentSpecificUpgrades? CustomUpgradeData { get; set; }

    public bool Equals(OrganelleUpgrades? other)
    {
        // TODO: allow default value to equal null, see: https://github.com/Revolutionary-Games/Thrive/issues/4091
        if (other == null)
            return false;

        if (!UnlockedFeatures.SequenceEqual(other.UnlockedFeatures))
            return false;

        if (CustomUpgradeData == null)
            return other.CustomUpgradeData == null;

        if (other.CustomUpgradeData == null)
            return false;

        return CustomUpgradeData.Equals(other.CustomUpgradeData);
    }

    public object Clone()
    {
        return new OrganelleUpgrades
        {
            UnlockedFeatures = new List<string>(UnlockedFeatures),
            CustomUpgradeData = (IComponentSpecificUpgrades?)CustomUpgradeData?.Clone(),
        };
    }

    public override int GetHashCode()
    {
        return UnlockedFeatures.GetHashCode() * 3 ^
            (CustomUpgradeData != null ? CustomUpgradeData.GetHashCode() : 1) * 1151;
    }
}
