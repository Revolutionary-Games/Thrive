using System;
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

    protected override double CalculateCostInternal(IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition)
    {
        double cost = 0;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            if (other is OrganelleMoveActionData moveActionData &&
                moveActionData.MovedHex.Definition == PlacedHex.Definition && MatchesContext(moveActionData))
            {
                if (moveActionData.OldLocation == Location ||
                    ReplacedCytoplasm?.Contains(moveActionData.MovedHex) == true)
                {
                    cost = Math.Min(-other.GetCalculatedCost(), cost);
                    continue;
                }
            }

            if (other is OrganellePlacementActionData placementActionData &&
                ReplacedCytoplasm?.Contains(placementActionData.PlacedHex) == true &&
                MatchesContext(placementActionData))
            {
                cost = Math.Min(-other.GetCalculatedCost(), cost);
            }
        }

        return cost + base.CalculateCostInternal(history, insertPosition);
    }
}
