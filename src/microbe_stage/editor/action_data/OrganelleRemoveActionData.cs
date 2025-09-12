using System.Collections.Generic;
using Newtonsoft.Json;

[JSONAlwaysDynamicType]
public class OrganelleRemoveActionData : HexRemoveActionData<OrganelleTemplate, CellType>
{
    /// <summary>
    ///   Used for replacing Cytoplasm. If true, this action is free.
    /// </summary>
    public bool GotReplaced;

    [JsonConstructor]
    public OrganelleRemoveActionData(OrganelleTemplate organelle, Hex location, int orientation) : base(organelle,
        location, orientation)
    {
    }

    public OrganelleRemoveActionData(OrganelleTemplate organelle) : base(organelle, organelle.Position,
        organelle.Orientation)
    {
    }

    protected override double CalculateBaseCostInternal()
    {
        return GotReplaced ? 0 : base.CalculateBaseCostInternal();
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        var cost = base.CalculateCostInternal(history, insertPosition);
        double refund = 0;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // Endosymbionts can be deleted for free after placing (not that it is very useful, but it should be free)
            if (other is EndosymbiontPlaceActionData endosymbiontPlaceActionData &&
                MatchesContext(endosymbiontPlaceActionData))
            {
                if (RemovedHex == endosymbiontPlaceActionData.PlacedOrganelle)
                {
                    return (0, cost.RefundCost);
                }
            }

            if (other is OrganelleUpgradeActionData upgradeActionData &&
                upgradeActionData.UpgradedOrganelle == RemovedHex && MatchesContext(upgradeActionData))
            {
                // This replaces (refunds) the MP for an upgrade done to this organelle
                if (ReferenceEquals(upgradeActionData.UpgradedOrganelle, RemovedHex))
                {
                    refund += upgradeActionData.GetCalculatedSelfCost();
                }
            }
        }

        return (cost.Cost, cost.RefundCost + refund);
    }
}
