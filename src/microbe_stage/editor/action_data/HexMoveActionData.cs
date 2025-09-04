using System.Collections.Generic;

public abstract class HexMoveActionData<THex, TContext> : EditorCombinableActionData<TContext>
    where THex : class, IActionHex
{
    public THex MovedHex;
    public Hex OldLocation;
    public Hex NewLocation;
    public int OldRotation;
    public int NewRotation;

    protected HexMoveActionData(THex hex, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation)
    {
        MovedHex = hex;
        OldLocation = oldLocation;
        NewLocation = newLocation;
        OldRotation = oldRotation;
        NewRotation = newRotation;
    }

    protected override double CalculateBaseCostInternal()
    {
        if (OldLocation == NewLocation && OldRotation == NewRotation)
            return 0;

        return Constants.ORGANELLE_MOVE_COST;
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        // Move is free if moving a hex placed in this session, or if moving something moved already
        var cost = CalculateBaseCostInternal();
        double refund = 0;

        bool removed = false;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            if (other is HexRemoveActionData<THex, TContext> removeActionData &&
                removeActionData.RemovedHex.MatchesDefinition(MovedHex) && MatchesContext(removeActionData))
            {
                removed = true;
                continue;
            }

            if (other is HexPlacementActionData<THex, TContext> placementActionData &&
                placementActionData.PlacedHex.MatchesDefinition(MovedHex) &&
                placementActionData.Location == OldLocation &&
                placementActionData.Orientation == OldRotation && MatchesContext(placementActionData))
            {
                // If placed in the same session and not deleted before that, then all moves are free
                if (!removed)
                {
                    return (0, 0);
                }

                continue;
            }

            // If this hex got moved in the same session again
            if (other is HexMoveActionData<THex, TContext> moveActionData &&
                moveActionData.MovedHex.MatchesDefinition(MovedHex) && MatchesContext(moveActionData))
            {
                // If this hex got moved back and forth
                if (OldLocation == moveActionData.NewLocation && NewLocation == moveActionData.OldLocation &&
                    OldRotation == moveActionData.NewRotation && NewRotation == moveActionData.OldRotation)
                {
                    cost = 0;
                    refund += other.GetCalculatedSelfCost();
                    continue;
                }

                // If this hex got moved twice
                if ((moveActionData.NewLocation == OldLocation && moveActionData.NewRotation == OldRotation) ||
                    (NewLocation == moveActionData.OldLocation && NewRotation == moveActionData.OldRotation))
                {
                    refund += other.GetCalculatedSelfCost();
                }
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
