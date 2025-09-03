using System;
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

    protected override double CalculateCostInternal(IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition)
    {
        // Move is free if moving a hex placed in this session, or if moving something moved already
        var cost = CalculateBaseCostInternal();

        bool interruptedByRemove = false;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            if (other is HexRemoveActionData<THex, TContext> removeActionData &&
                removeActionData.RemovedHex.MatchesDefinition(MovedHex) && MatchesContext(removeActionData))
            {
                interruptedByRemove = true;
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
                    cost = Math.Min(-other.GetCalculatedCost(), cost);
                    continue;
                }

                // If this hex got moved twice
                if (((moveActionData.NewLocation == OldLocation && moveActionData.NewRotation == OldRotation) ||
                        (NewLocation == moveActionData.OldLocation && NewRotation == moveActionData.OldRotation)) &&
                    !interruptedByRemove)
                {
                    cost = Math.Min(0, cost);
                    continue;
                }
            }

            // If this hex got placed in this session
            if (other is HexPlacementActionData<THex, TContext> placementActionData &&
                placementActionData.PlacedHex.MatchesDefinition(MovedHex) &&
                placementActionData.Location == OldLocation &&
                placementActionData.Orientation == OldRotation && MatchesContext(placementActionData) &&
                !interruptedByRemove)
            {
                cost = Math.Min(0, cost);
            }

            // Moves shouldn't happen after a remove, so we don't check that here
        }

        return cost;
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
