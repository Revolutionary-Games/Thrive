using System.Collections.Generic;

[JSONAlwaysDynamicType]
public class OrganellePlacementActionData : HexPlacementActionData<OrganelleTemplate>
{
    public List<OrganelleTemplate>? ReplacedCytoplasm;

    public OrganellePlacementActionData(OrganelleTemplate organelle, Hex location, int orientation) : base(organelle, location, orientation)
    {
    }

    protected override int CalculateCostInternal()
    {
        return PlacedHex.Definition.MPCost;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // If this organelle got removed in this session
        if (other is OrganelleRemoveActionData removeActionData && removeActionData.AddedHex.Definition == PlacedHex.Definition)
        {
            // If the placed organelle has been placed on the same position where it got removed before
            if (removeActionData.Location == Location)
                return ActionInterferenceMode.CancelsOut;

            // Removing and placing an organelle is a move operation
            return ActionInterferenceMode.Combinable;
        }

        if (other is OrganelleMoveActionData moveActionData &&
            moveActionData.MovedHex.Definition == PlacedHex.Definition)
        {
            if (moveActionData.OldLocation == Location)
                return ActionInterferenceMode.Combinable;

            if (ReplacedCytoplasm?.Contains(moveActionData.MovedHex) == true)
                return ActionInterferenceMode.ReplacesOther;
        }

        if (other is OrganellePlacementActionData placementActionData &&
            ReplacedCytoplasm?.Contains(placementActionData.PlacedHex) == true)
            return ActionInterferenceMode.ReplacesOther;

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is OrganelleRemoveActionData removeActionData)
        {
            return new OrganelleMoveActionData(removeActionData.AddedHex, removeActionData.Location, Location,
                removeActionData.Orientation, Orientation);
        }

        var moveActionData = (OrganelleMoveActionData)other;
        return new OrganellePlacementActionData(PlacedHex, moveActionData.NewLocation, moveActionData.NewRotation);
    }
}
