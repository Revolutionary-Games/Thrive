using System.Collections.Generic;

[JSONAlwaysDynamicType]
public abstract class HexRemoveActionData<THex, TContext> : EditorCombinableActionData<TContext>
    where THex : class, IActionHex
{
    public THex RemovedHex;
    public Hex Location;
    public int Orientation;

    protected HexRemoveActionData(THex hex, Hex location, int orientation)
    {
        RemovedHex = hex;
        Location = location;
        Orientation = orientation;
    }

    protected override double CalculateBaseCostInternal()
    {
        return Constants.ORGANELLE_REMOVE_COST;
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        var cost = CalculateBaseCostInternal();
        double refund = 0;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // If this hex got placed in this session
            if (other is HexPlacementActionData<THex, TContext> placementActionData &&
                placementActionData.PlacedHex.MatchesDefinition(RemovedHex) && MatchesContext(placementActionData))
            {
                cost = 0;
                refund += other.GetCalculatedSelfCost();
                continue;
            }

            // If this hex got moved in this session, refund the move cost
            if (other is HexMoveActionData<THex, TContext> moveActionData &&
                moveActionData.MovedHex.MatchesDefinition(RemovedHex) &&
                moveActionData.NewLocation == Location && MatchesContext(moveActionData))
            {
                refund += other.GetCalculatedSelfCost() - other.GetCalculatedRefundCost();
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
