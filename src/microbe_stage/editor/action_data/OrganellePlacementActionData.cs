using System.Collections.Generic;

[JSONAlwaysDynamicType]
public class OrganellePlacementActionData : HexPlacementActionData<OrganelleTemplate, CellType>
{
    public List<OrganelleTemplate>? ReplacedCytoplasm;

    public OrganellePlacementActionData(OrganelleTemplate organelle, Hex location, int orientation) : base(organelle,
        location, orientation)
    {
    }

    protected override int CalculateCostInternal()
    {
        return PlacedHex.Definition.MPCost;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
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

        return base.GetInterferenceModeWithGuaranteed(other);
    }

    protected override CombinableActionData CreateDerivedMoveAction(
        HexRemoveActionData<OrganelleTemplate, CellType> data)
    {
        return new OrganelleMoveActionData(data.RemovedHex, data.Location, Location,
            data.Orientation, Orientation);
    }

    protected override CombinableActionData CreateDerivedPlacementAction(
        HexMoveActionData<OrganelleTemplate, CellType> data)
    {
        return new OrganellePlacementActionData(PlacedHex, data.NewLocation, data.NewRotation);
    }
}
