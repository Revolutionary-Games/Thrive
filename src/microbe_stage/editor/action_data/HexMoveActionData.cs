using System;

public abstract class HexMoveActionData<THex> : EditorCombinableActionData
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

    protected override int CalculateCostInternal()
    {
        if (OldLocation == NewLocation && OldRotation == NewRotation)
            return 0;

        return Constants.ORGANELLE_MOVE_COST;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // If this hex got moved in the same session again
        if (other is HexMoveActionData<THex> moveActionData &&
            moveActionData.MovedHex.MatchesDefinition(MovedHex))
        {
            // If this hex got moved back and forth
            if (OldLocation == moveActionData.NewLocation && NewLocation == moveActionData.OldLocation &&
                OldRotation == moveActionData.NewRotation && NewRotation == moveActionData.OldRotation)
                return ActionInterferenceMode.CancelsOut;

            // If this hex got moved twice
            if ((moveActionData.NewLocation == OldLocation && moveActionData.NewRotation == OldRotation) ||
                (NewLocation == moveActionData.OldLocation && NewRotation == moveActionData.OldRotation))
                return ActionInterferenceMode.Combinable;
        }

        // If this hex got placed in this session
        if (other is HexPlacementActionData<THex> placementActionData &&
            placementActionData.PlacedHex.MatchesDefinition(MovedHex) &&
            placementActionData.Location == OldLocation &&
            placementActionData.Orientation == OldRotation)
        {
            return ActionInterferenceMode.Combinable;
        }

        // If this hex got removed in this session
        if (other is HexRemoveActionData<THex> removeActionData &&
            removeActionData.AddedHex.MatchesDefinition(MovedHex) &&
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
            case HexPlacementActionData<THex> placementActionData:
                return CreateDerivedPlacementAction(placementActionData);
            case HexMoveActionData<THex> moveActionData when moveActionData.NewLocation == OldLocation:
                return CreateDerivedMoveAction(MovedHex, moveActionData.OldLocation, NewLocation,
                    moveActionData.OldRotation, NewRotation);
            case HexMoveActionData<THex> moveActionData:
                return CreateDerivedMoveAction(moveActionData.MovedHex, OldLocation, moveActionData.NewLocation,
                    OldRotation, moveActionData.NewRotation);
            default:
                throw new NotSupportedException();
        }
    }

    protected abstract CombinableActionData CreateDerivedMoveAction(THex hex, Hex oldLocation, Hex newLocation,
        int oldRotation, int newRotation);

    protected abstract CombinableActionData CreateDerivedPlacementAction(HexPlacementActionData<THex> data);
}
