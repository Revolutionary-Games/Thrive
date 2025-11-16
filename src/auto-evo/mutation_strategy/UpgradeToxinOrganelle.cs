using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using AutoEvo;

/// <summary>
///   A specific mutation strategy for toxin-launching organelles
///   applies a fixed toxin type based on a given upgradeName
///   and randomly adjusts toxicity upward, downward, or in both directions
/// </summary>
public class UpgradeToxinOrganelle : ModifyOrganelleBase
{
    private readonly MutationDirection direction;
    private readonly string upgradeName;
    private readonly bool isDefault;

    /// <summary>
    ///   Updates a toxin-launching organelle with a given toxin type, updating the toxin type in custom data to match,
    ///   and randomly moves toxicity in a given direction.
    /// </summary>
    /// <param name="criteria">Organelle requirement to apply</param>
    /// <param name="upgradeName">The name of the upgrade (from organelles.json) to apply</param>
    /// <param name="isDefault">Must be true when the upgrade name is the default upgrade</param>
    /// <param name="direction">"increase", "decrease", or "both" to decide what to do with toxicity</param>
    public UpgradeToxinOrganelle(Func<OrganelleDefinition, bool> criteria, string upgradeName, bool isDefault,
        MutationDirection direction) : base(criteria, true)
    {
        this.upgradeName = upgradeName;
        this.isDefault = isDefault;
        this.direction = direction;
    }

    /// <summary>
    ///   Direction for toxicity mutation
    /// </summary>
    public enum MutationDirection
    {
        Increase,
        Decrease,
        Both,
    }

    public override bool ExpectedToCostMP => true;

    protected override bool CanModifyOrganelle(OrganelleTemplate organelle, double mpRemaining)
    {
        double mpCost = 0;
        bool canUpgrade = false;

        foreach (var availableUpgrade in organelle.Definition.AvailableUpgrades)
        {
            // Filter to just applicable upgrades
            if (availableUpgrade.Key == upgradeName && !IsUpgradeSelected(organelle))
            {
                mpCost += availableUpgrade.Value.MPCost;
                canUpgrade = true;
                break;
            }
        }

        if (mpCost > mpRemaining || !canUpgrade)
            return false;

        return true;
    }

    protected override bool ApplyOrganelleUpgrade(double mpRemaining, OrganelleTemplate originalOrganelle,
        ref double mpCost, [NotNullWhen(true)] out OrganelleTemplate? upgradedOrganelle, Random random)
    {
        ToxinType toxinType = ToxinType.Oxytoxy;
        bool canUpgrade = false;

        foreach (var availableUpgrade in originalOrganelle.Definition.AvailableUpgrades)
        {
            if (availableUpgrade.Key == upgradeName && !IsUpgradeSelected(originalOrganelle))
            {
                toxinType = ToxinUpgradeNames.ToxinTypeFromName(availableUpgrade.Key);
                mpCost += availableUpgrade.Value.MPCost;
                canUpgrade = true;
                break;
            }
        }

        if (mpCost > mpRemaining || !canUpgrade)
        {
            upgradedOrganelle = null;
            return false;
        }

        // Start applying changes
        upgradedOrganelle = originalOrganelle.Clone(false);

        ToxinUpgrades toxinData;
        if (originalOrganelle.Upgrades?.CustomUpgradeData is not ToxinUpgrades)
        {
            toxinData = new ToxinUpgrades(toxinType, 0.0f);
            upgradedOrganelle.ModifiableUpgrades = new OrganelleUpgrades
            {
                CustomUpgradeData = toxinData,
            };
        }
        else
        {
            upgradedOrganelle.ModifiableUpgrades = originalOrganelle.Upgrades.Clone();
            toxinData = (ToxinUpgrades?)upgradedOrganelle.ModifiableUpgrades?.CustomUpgradeData ??
                throw new InvalidOperationException("Clone didn't clone custom data");
            toxinData.BaseType = toxinType;
        }

        if (canUpgrade)
        {
            // Don't stack all of the toxin types, only one can be active at a time
            upgradedOrganelle.ModifiableUpgrades.ModifiableUnlockedFeatures.Clear();
            if (!isDefault)
            {
                upgradedOrganelle.ModifiableUpgrades.ModifiableUnlockedFeatures.Add(upgradeName);
            }
        }

        // Adjust toxicity
        var change = (float)(random.NextDouble() * Constants.AUTO_EVO_MUTATION_TOXICITY_STEP);

        switch (direction)
        {
            case MutationDirection.Increase:
                toxinData.Toxicity = Math.Clamp(toxinData.Toxicity + change, -1.0f, 1.0f);
                break;

            case MutationDirection.Decrease:
                toxinData.Toxicity = Math.Clamp(toxinData.Toxicity - change, -1.0f, 1.0f);
                break;

            case MutationDirection.Both:
                change *= random.NextDouble() < 0.5 ? -1.0f : 1.0f;
                toxinData.Toxicity = Math.Clamp(toxinData.Toxicity + change, -1.0f, 1.0f);
                break;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsUpgradeSelected(OrganelleTemplate organelle)
    {
        if (organelle.Upgrades == null)
            return isDefault;

        if (organelle.Upgrades.UnlockedFeatures.Contains(upgradeName))
            return true;

        // If missing from the list, is the default if the list is empty
        if (organelle.Upgrades.UnlockedFeatures.Count < 1)
            return isDefault;

        return false;
    }
}
