[JSONAlwaysDynamicType]
public abstract class HexRemoveActionData<THex, TContext> : EditorCombinableActionData<TContext>
    where THex : class, IActionHex
{
    public THex RemovedHex;
    public Hex Location;
    public int Orientation;

    protected HexRemoveActionData(THex hex, Hex location, int orientation)
    {
        RemovedHex = hex;
        Location = location;
        Orientation = orientation;
    }

    protected override int CalculateCostInternal()
    {
        return Constants.ORGANELLE_REMOVE_COST;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // If this hex got placed in this session on the same position
        if (other is HexPlacementActionData<THex, TContext> placementActionData &&
            placementActionData.PlacedHex.MatchesDefinition(RemovedHex))
        {
            // If this hex got placed on the same position
            if (placementActionData.Location == Location)
                return ActionInterferenceMode.CancelsOut;

            // Removing an hex and then placing it is a move operation
            return ActionInterferenceMode.Combinable;
        }

        // If this hex got moved in this session
        if (other is HexMoveActionData<THex, TContext> moveActionData &&
            moveActionData.MovedHex.MatchesDefinition(RemovedHex) &&
            moveActionData.NewLocation == Location)
        {
            return ActionInterferenceMode.Combinable;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is HexPlacementActionData<THex, TContext> placementActionData)
        {
            return CreateDerivedMoveAction(placementActionData);
        }

        return CreateDerivedRemoveAction((HexMoveActionData<THex, TContext>)other);
    }

    protected abstract CombinableActionData CreateDerivedMoveAction(HexPlacementActionData<THex, TContext> data);

    protected abstract CombinableActionData CreateDerivedRemoveAction(HexMoveActionData<THex, TContext> data);
}
