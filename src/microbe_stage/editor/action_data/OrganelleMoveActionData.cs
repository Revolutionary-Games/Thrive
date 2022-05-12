using System;

public class OrganelleMoveActionData : HexMoveActionData<OrganelleTemplate>
{
    public OrganelleMoveActionData(OrganelleTemplate organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation) : base(organelle, oldLocation, newLocation, oldRotation, newRotation)
    {
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // If this organelle got moved in the same session again
        if (other is OrganelleMoveActionData moveActionData &&
            moveActionData.MovedHex.Definition == MovedHex.Definition)
        {
            // If this organelle got moved back and forth
            if (OldLocation == moveActionData.NewLocation && NewLocation == moveActionData.OldLocation &&
                OldRotation == moveActionData.NewRotation && NewRotation == moveActionData.OldRotation)
                return ActionInterferenceMode.CancelsOut;

            // If this organelle got moved twice
            if ((moveActionData.NewLocation == OldLocation && moveActionData.NewRotation == OldRotation) ||
                (NewLocation == moveActionData.OldLocation && NewRotation == moveActionData.OldRotation))
                return ActionInterferenceMode.Combinable;
        }

        // If this organelle got placed in this session
        if (other is OrganellePlacementActionData placementActionData &&
            placementActionData.PlacedHex.Definition == MovedHex.Definition &&
            placementActionData.Location == OldLocation &&
            placementActionData.Orientation == OldRotation)
        {
            return ActionInterferenceMode.Combinable;
        }

        // If this organelle got removed in this session
        if (other is RemoveActionData removeActionData &&
            removeActionData.Organelle.Definition == MovedHex.Definition &&
            removeActionData.Location == NewLocation)
        {
            return ActionInterferenceMode.ReplacesOther;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        switch (other)
        {
            case OrganellePlacementActionData placementActionData:
                return new OrganellePlacementActionData(placementActionData.PlacedHex, NewLocation, NewRotation)
                {
                    ReplacedCytoplasm = placementActionData.ReplacedCytoplasm,
                };
            case OrganelleMoveActionData moveActionData when moveActionData.NewLocation == OldLocation:
                return new OrganelleMoveActionData(MovedHex, moveActionData.OldLocation, NewLocation,
                    moveActionData.OldRotation, NewRotation);
            case OrganelleMoveActionData moveActionData:
                return new OrganelleMoveActionData(moveActionData.MovedHex, OldLocation, moveActionData.NewLocation,
                    OldRotation, moveActionData.NewRotation);
            default:
                throw new NotSupportedException();
        }
    }
}
