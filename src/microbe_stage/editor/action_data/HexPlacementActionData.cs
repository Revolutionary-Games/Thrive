using System;
using System.Collections.Generic;

[JSONAlwaysDynamicType]
public abstract class HexPlacementActionData<THex, TContext> : EditorCombinableActionData<TContext>
    where THex : class, IActionHex
{
    public THex PlacedHex;
    public Hex Location;
    public int Orientation;

    protected HexPlacementActionData(THex hex, Hex location, int orientation)
    {
        PlacedHex = hex;
        Location = location;
        Orientation = orientation;
    }

    protected override double CalculateCostInternal(IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition)
    {
        var cost = CalculateBaseCostInternal();

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // If this hex got removed in this session
            if (other is HexRemoveActionData<THex, TContext> removeActionData &&
                removeActionData.RemovedHex.MatchesDefinition(PlacedHex) && MatchesContext(removeActionData))
            {
                // If the placed hex has been placed in the same position where it got removed from before
                if (removeActionData.Location == Location)
                {
                    cost = Math.Min(-other.GetCalculatedCost(), cost);
                }
                else
                {
                    // Removing and placing a hex is a move operation
                    cost = Math.Min(-other.GetCalculatedCost() + Constants.ORGANELLE_MOVE_COST, cost);
                }
            }
        }

        return cost;
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
