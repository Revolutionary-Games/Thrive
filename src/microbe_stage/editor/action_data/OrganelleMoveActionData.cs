public class OrganelleMoveActionData : HexMoveActionData<OrganelleTemplate, CellType>
{
    public OrganelleMoveActionData(OrganelleTemplate organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation) : base(organelle, oldLocation, newLocation, oldRotation, newRotation)
    {
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // Endosymbionts can be moved for free after placing
        if (other is EndosymbiontPlaceActionData endosymbiontPlaceActionData)
        {
            // If moved after placing
            if (MovedHex == endosymbiontPlaceActionData.PlacedOrganelle &&
                OldLocation == endosymbiontPlaceActionData.PlacementLocation &&
                OldRotation == endosymbiontPlaceActionData.PlacementRotation)
            {
                return ActionInterferenceMode.Combinable;
            }
        }

        return base.GetInterferenceModeWithGuaranteed(other);
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is EndosymbiontPlaceActionData endosymbiontPlaceActionData)
        {
            return new EndosymbiontPlaceActionData(endosymbiontPlaceActionData.PlacedOrganelle, NewLocation,
                NewRotation, endosymbiontPlaceActionData.RelatedEndosymbiosisAction)
            {
                PerformedUnlock = endosymbiontPlaceActionData.PerformedUnlock,
                OverriddenEndosymbiosisOnUndo = endosymbiontPlaceActionData.OverriddenEndosymbiosisOnUndo,
            };
        }

        return base.CombineGuaranteed(other);
    }

    protected override CombinableActionData CreateDerivedMoveAction(OrganelleTemplate hex, Hex oldLocation,
        Hex newLocation, int oldRotation, int newRotation)
    {
        return new OrganelleMoveActionData(hex, oldLocation, newLocation, oldRotation, newRotation);
    }

    protected override CombinableActionData CreateDerivedPlacementAction(HexPlacementActionData<OrganelleTemplate,
        CellType> data)
    {
        var placementActionData = (OrganellePlacementActionData)data;

        return new OrganellePlacementActionData(placementActionData.PlacedHex, NewLocation, NewRotation)
        {
            ReplacedCytoplasm = placementActionData.ReplacedCytoplasm,
        };
    }
}
