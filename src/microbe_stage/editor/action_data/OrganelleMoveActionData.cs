using System.Collections.Generic;

public class OrganelleMoveActionData : HexMoveActionData<OrganelleTemplate, CellType>
{
    public OrganelleMoveActionData(OrganelleTemplate organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation) : base(organelle, oldLocation, newLocation, oldRotation, newRotation)
    {
    }

    protected override double CalculateCostInternal(IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition)
    {
        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // Endosymbionts can be moved for free after placing
            if (other is EndosymbiontPlaceActionData endosymbiontPlaceActionData &&
                MatchesContext(endosymbiontPlaceActionData))
            {
                // If moved after placing
                if (MovedHex == endosymbiontPlaceActionData.PlacedOrganelle &&
                    OldLocation == endosymbiontPlaceActionData.PlacementLocation &&
                    OldRotation == endosymbiontPlaceActionData.PlacementRotation)
                {
                    return 0;
                }
            }
        }

        return base.CalculateCostInternal(history, insertPosition);
    }
}
