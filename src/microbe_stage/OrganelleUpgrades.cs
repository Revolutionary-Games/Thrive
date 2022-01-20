using System.Collections.Generic;

/// <summary>
///   Upgrades for a placed or template organelle
/// </summary>
public class OrganelleUpgrades
{
    /// <summary>
    ///   A list of "feature" names that have been unlocked for this organelle. Depends on the organelle components
    ///   what names they look for
    /// </summary>
    public List<string> UnlockedFeatures { get; set; } = new();

    /// <summary>
    ///   Organelle type specific upgrade data. Null if not configured
    /// </summary>
    public IComponentSpecificUpgrades CustomUpgradeData { get; set; }
}
