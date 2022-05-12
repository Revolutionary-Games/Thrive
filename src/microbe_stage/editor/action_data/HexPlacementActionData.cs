[JSONAlwaysDynamicType]
public abstract class HexPlacementActionData<THex> : EditorCombinableActionData
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

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // If this hex got removed in this session
        if (other is HexRemoveActionData<THex> removeActionData &&
            removeActionData.AddedHex.MatchesDefinition(PlacedHex))
        {
            // If the placed hex has been placed on the same position where it got removed before
            if (removeActionData.Location == Location)
                return ActionInterferenceMode.CancelsOut;

            // Removing and placing a hex is a move operation
            return ActionInterferenceMode.Combinable;
        }

        if (other is HexMoveActionData<THex> moveActionData &&
            moveActionData.MovedHex.MatchesDefinition(PlacedHex))
        {
            if (moveActionData.OldLocation == Location)
                return ActionInterferenceMode.Combinable;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is HexRemoveActionData<THex> removeActionData)
        {
            return CreateDerivedMoveAction(removeActionData);
        }

        return CreateDerivedPlacementAction((HexMoveActionData<THex>)other);
    }

    protected abstract CombinableActionData CreateDerivedMoveAction(HexRemoveActionData<THex> data);
    protected abstract CombinableActionData CreateDerivedPlacementAction(HexMoveActionData<THex> data);
}
