using System;
using System.Diagnostics.CodeAnalysis;
using AutoEvo;

public class UpgradeOrganelle : ModifyOrganelleBase
{
    private readonly IComponentSpecificUpgrades? customUpgrade;
    private readonly string? upgradeName;

    /// <summary>
    ///   Updates an organelle type with the given upgrade custom data
    /// </summary>
    /// <param name="criteria">Organelle requirement to apply</param>
    /// <param name="customUpgrade">The custom data to be applied as an upgrade to the organelle</param>
    public UpgradeOrganelle(Func<OrganelleDefinition, bool> criteria, IComponentSpecificUpgrades customUpgrade) : base(
        criteria, false)
    {
        this.customUpgrade = customUpgrade;
    }

    /// <summary>
    ///   Updates an organelle type with the given upgrade, creating a new species to test for every additional
    ///   organelle upgraded.
    /// </summary>
    /// <param name="criteria">Organelle requirement to apply</param>
    /// <param name="upgradeName">The name of the upgrade (from organelles.json) to apply</param>
    /// <param name="shouldRepeat">
    ///   Determines whether this mutation strategy can be used multiple times
    ///   should be false for any upgrade that does not cost MP
    /// </param>
    public UpgradeOrganelle(Func<OrganelleDefinition, bool> criteria, string upgradeName, bool shouldRepeat) : base(
        criteria, shouldRepeat)
    {
        foreach (var organelle in allOrganelles)
        {
            if (!organelle.AvailableUpgrades.ContainsKey(upgradeName))
            {
                throw new ArgumentException(organelle.Name + " does not have upgrade: " + upgradeName);
            }
        }

        this.upgradeName = upgradeName;
    }

    // For now, this matches the case pretty much where free upgrades are not made repeatable
    public override bool ExpectedToCostMP => Repeatable;

    protected override bool CanModifyOrganelle(OrganelleTemplate organelle, double mpRemaining)
    {
        double mpCost = 0;
        bool canUpgrade = false;

        if (upgradeName != null)
        {
            foreach (var availableUpgrade in organelle.Definition.AvailableUpgrades)
            {
                // Filter to just applicable upgrades
                if (availableUpgrade.Key == upgradeName && (organelle.Upgrades == null ||
                        !organelle.Upgrades.UnlockedFeatures.Contains(upgradeName)))
                {
                    mpCost += availableUpgrade.Value.MPCost;
                    canUpgrade = true;
                    break;
                }
            }
        }

        if (customUpgrade != null)
        {
            mpCost += customUpgrade.CalculateCost(organelle.Upgrades?.CustomUpgradeData);
            canUpgrade = true;
        }

        // Don't add to the mutations-to-try-list if too expensive
        if (mpCost > mpRemaining || (customUpgrade == null && !canUpgrade))
            return false;

        return true;
    }

    protected override bool ApplyOrganelleUpgrade(double mpRemaining, OrganelleTemplate originalOrganelle,
        ref double mpCost, [NotNullWhen(true)] out OrganelleTemplate? upgradedOrganelle, Random random)
    {
        bool hasFeatureUpgrade = false;

        // We need to re-calculate the cost here as not all organelles will have a uniform cost based
        // on what upgrades they have already applied (and we need to know that for the final cost)
        if (upgradeName != null)
        {
            foreach (var availableUpgrade in originalOrganelle.Definition.AvailableUpgrades)
            {
                // Check if found an available upgrade that is not applied yet
                if (availableUpgrade.Key == upgradeName && (originalOrganelle.Upgrades == null ||
                        !originalOrganelle.Upgrades.UnlockedFeatures.Contains(upgradeName)))
                {
                    mpCost += availableUpgrade.Value.MPCost;
                    hasFeatureUpgrade = true;
                    break;
                }
            }
        }

        if (customUpgrade != null)
        {
            mpCost += customUpgrade.CalculateCost(originalOrganelle.Upgrades?.CustomUpgradeData);
        }

        if (mpCost > mpRemaining || (customUpgrade == null && !hasFeatureUpgrade))
        {
            // This is wasting an attempt as we do not have enough MPs to upgrade this organelle
            // As we try only one organelle upgrade per loop, we need to abandon this entire species clone
            // attempt
            upgradedOrganelle = null;
            return false;
        }

        upgradedOrganelle = originalOrganelle.Clone(false);

        upgradedOrganelle.Upgrades = new OrganelleUpgrades();

        if (customUpgrade != null)
        {
            upgradedOrganelle.Upgrades.CustomUpgradeData = customUpgrade;
        }

        if (upgradeName != null && hasFeatureUpgrade &&
            !upgradedOrganelle.Upgrades.UnlockedFeatures.Contains(upgradeName))
        {
            upgradedOrganelle.Upgrades.UnlockedFeatures.Add(upgradeName);
        }

        return true;
    }
}
