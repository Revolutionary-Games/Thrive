﻿using System;
using System.Collections.Generic;

/// <summary>
///   Upgrades for a placed or template organelle
/// </summary>
public class OrganelleUpgrades : ICloneable
{
    /// <summary>
    ///   A list of "feature" names that have been unlocked for this organelle. Depends on the organelle components
    ///   what names they look for
    /// </summary>
    public List<string> UnlockedFeatures { get; set; } = new();

    /// <summary>
    ///   Organelle type specific upgrade data. Null if not configured
    /// </summary>
    public IComponentSpecificUpgrades? CustomUpgradeData { get; set; }

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
        return (UnlockedFeatures.GetHashCode() * 3) ^
            ((CustomUpgradeData != null ? CustomUpgradeData.GetHashCode() : 1) * 1151);
    }
}
