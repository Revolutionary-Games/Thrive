using System;
using System.Linq;

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

    protected override int CalculateCostInternal()
    {
        int cost = 0;

        // TODO: allow custom upgrades to have a cost

        // Calculate the costs of the selected new general upgrades (minus the cost of removed upgrades)
        var availableUpgrades = UpgradedOrganelle.Definition.AvailableUpgrades;

        var newUpgrades = NewUpgrades.UnlockedFeatures.Except(OldUpgrades.UnlockedFeatures)
            .Where(u => availableUpgrades.ContainsKey(u)).Select(u => availableUpgrades[u]);
        var removedUpgrades = OldUpgrades.UnlockedFeatures.Except(NewUpgrades.UnlockedFeatures)
            .Where(u => availableUpgrades.ContainsKey(u)).Select(u => availableUpgrades[u]);

        cost += newUpgrades.Sum(u => u.MPCost);
        cost -= removedUpgrades.Sum(u => u.MPCost);

        return cost;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        if (other is OrganelleUpgradeActionData upgradeActionData)
        {
            if (ReferenceEquals(UpgradedOrganelle, upgradeActionData.UpgradedOrganelle))
                return ActionInterferenceMode.Combinable;
        }

        // The replacing action is in the remove organelle action

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is OrganelleUpgradeActionData upgradeActionData)
        {
            return new OrganelleUpgradeActionData(upgradeActionData.OldUpgrades, NewUpgrades,
                UpgradedOrganelle);
        }

        throw new NotSupportedException();
    }

    protected override void MergeGuaranteed(CombinableActionData other)
    {
        var upgradeActionData = (OrganelleUpgradeActionData)other;

        OldUpgrades = upgradeActionData.OldUpgrades;
    }
}
