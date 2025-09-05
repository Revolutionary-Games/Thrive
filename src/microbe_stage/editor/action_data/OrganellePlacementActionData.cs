using System.Collections.Generic;

[JSONAlwaysDynamicType]
public class OrganellePlacementActionData : HexPlacementActionData<OrganelleTemplate, CellType>
{
    public List<OrganelleTemplate>? ReplacedCytoplasm;

    public OrganellePlacementActionData(OrganelleTemplate organelle, Hex location, int orientation) : base(organelle,
        location, orientation)
    {
    }

    protected override double CalculateBaseCostInternal()
    {
        return PlacedHex.Definition.MPCost;
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        double refund = 0;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            if (other is OrganelleMoveActionData moveActionData && MatchesContext(moveActionData))
            {
                if ((moveActionData.MovedHex.Definition == PlacedHex.Definition &&
                        moveActionData.OldLocation == Location) ||
                    ReplacedCytoplasm?.Contains(moveActionData.MovedHex) == true)
                {
                    refund += other.GetCalculatedSelfCost();
                    continue;
                }
            }

            if (other is OrganellePlacementActionData placementActionData &&
                ReplacedCytoplasm?.Contains(placementActionData.PlacedHex) == true &&
                MatchesContext(placementActionData))
            {
                refund += other.GetCalculatedSelfCost();
            }
        }

        var baseCost = base.CalculateCostInternal(history, insertPosition);

        return (baseCost.Cost, refund + baseCost.RefundCost);
    }
}
