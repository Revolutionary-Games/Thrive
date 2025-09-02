using System;
using System.Collections.Generic;
using Godot;

[JSONAlwaysDynamicType]
public class OrganelleUpgradeActionData : EditorCombinableActionData<CellType>
{
    public OrganelleUpgrades NewUpgrades;
    public OrganelleUpgrades OldUpgrades;

    // TODO: make the upgrade not cost MP if a new organelle of the same type is placed at the same location and then
    // upgraded in the same way
    public OrganelleTemplate UpgradedOrganelle;

    public OrganelleUpgradeActionData(OrganelleUpgrades oldUpgrades, OrganelleUpgrades newUpgrades,
        OrganelleTemplate upgradedOrganelle)
    {
        OldUpgrades = oldUpgrades;
        NewUpgrades = newUpgrades;
        UpgradedOrganelle = upgradedOrganelle;
    }

    public static double CalculateUpgradeCost(Dictionary<string, AvailableUpgrade> availableUpgrades,
        List<string> newUpgrades, List<string> oldUpgrades, bool refund = false)
    {
        int cost = 0;

        // TODO: allow custom upgrades to have a cost (should also add a test in EditorMPTests)

        // Calculate the costs of the selected new general upgrades

        foreach (var newUpgrade in newUpgrades)
        {
            if (oldUpgrades.Contains(newUpgrade))
                continue;

            if (!availableUpgrades.TryGetValue(newUpgrade, out var upgrade))
            {
                // TODO: this probably should be suppressed in cases where we have dynamically called upgrade names,
                // which we might do in the future
                GD.PrintErr("Cannot calculate cost for an unknown upgrade: ", newUpgrade);
            }
            else
            {
                cost += upgrade.MPCost;
            }
        }

        if (refund)
        {
            // Refund removed upgrades
            foreach (var oldUpgrade in oldUpgrades)
            {
                if (newUpgrades.Contains(oldUpgrade))
                    continue;

                if (!availableUpgrades.TryGetValue(oldUpgrade, out var upgrade))
                {
                    // See the TODO above
                    GD.PrintErr("Cannot calculate cost for an unknown upgrade: ", oldUpgrade);
                }
                else
                {
                    cost -= upgrade.MPCost;
                }
            }
        }

        // else
        {
            // TODO: Removals should cost MP: https://github.com/Revolutionary-Games/Thrive/issues/4095
            // var removedUpgrades = OldUpgrades.UnlockedFeatures.Except(NewUpgrades.UnlockedFeatures)
            //     .Where(u => availableUpgrades.ContainsKey(u)).Select(u => availableUpgrades[u]);
            // ? removedUpgrades.Sum(u => u.MPCost);
        }

        return cost;
    }

    protected override double CalculateBaseCostInternal()
    {
        return CalculateUpgradeCost(UpgradedOrganelle.Definition.AvailableUpgrades, NewUpgrades.UnlockedFeatures,
            OldUpgrades.UnlockedFeatures);
    }

    protected override double CalculateCostInternal(IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition)
    {
        var cost = CalculateBaseCostInternal();

        int i = CalculateValidityRegionStart(history, insertPosition, 0);

        var count = history.Count;
        for (; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            if (other is OrganelleUpgradeActionData upgradeActionData && MatchesContext(upgradeActionData))
            {
                if (ReferenceEquals(UpgradedOrganelle, upgradeActionData.UpgradedOrganelle))
                {
                    // When there's a previous upgrade, calculate this cost in relation to that to process refunds as
                    // well correctly
                    cost = CalculateUpgradeCost(UpgradedOrganelle.Definition.AvailableUpgrades,
                        NewUpgrades.UnlockedFeatures,
                        upgradeActionData.NewUpgrades.UnlockedFeatures, true);
                }
            }
        }

        return cost;
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        // Doesn't need to merge as organelle upgrades are applied when hitting "ok" in the GUI and not for each slider
        // step
        return false;
    }

    protected override bool ActionDenotesInterestingRegionBoundary(EditorCombinableActionData action)
    {
        return action is OrganelleUpgradeActionData upgradeActionData && MatchesContext(upgradeActionData) &&
            ReferenceEquals(UpgradedOrganelle, upgradeActionData.UpgradedOrganelle);
    }
}
