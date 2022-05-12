using Newtonsoft.Json;

[JSONAlwaysDynamicType]
public class OrganelleRemoveActionData : RemoveHexActionData<OrganelleTemplate>
{
    /// <summary>
    ///   Used for replacing Cytoplasm. If true this action is free.
    /// </summary>
    public bool GotReplaced;

    [JsonConstructor]
    public OrganelleRemoveActionData(OrganelleTemplate organelle, Hex location, int orientation) : base(organelle,
        location,
        orientation)
    {
    }

    public OrganelleRemoveActionData(OrganelleTemplate organelle) : base(organelle, organelle.Position,
        organelle.Orientation)
    {
    }

    protected override int CalculateCostInternal()
    {
        return GotReplaced ? 0 : base.CalculateCostInternal();
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // If this organelle got placed in this session on the same position
        if (other is OrganellePlacementActionData placementActionData &&
            placementActionData.PlacedHex.Definition == AddedHex.Definition)
        {
            // If this organelle got placed on the same position
            if (placementActionData.Location == Location)
                return ActionInterferenceMode.CancelsOut;

            // Removing an organelle and then placing it is a move operation
            return ActionInterferenceMode.Combinable;
        }

        // If this organelle got moved in this session
        if (other is OrganelleMoveActionData moveActionData &&
            moveActionData.MovedHex.Definition == AddedHex.Definition &&
            moveActionData.NewLocation == Location)
        {
            return ActionInterferenceMode.Combinable;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is OrganellePlacementActionData placementActionData)
        {
            return new OrganelleMoveActionData(placementActionData.PlacedHex, Location, placementActionData.Location,
                Orientation, placementActionData.Orientation);
        }

        var moveActionData = (OrganelleMoveActionData)other;
        return new OrganelleRemoveActionData(AddedHex, moveActionData.OldLocation, moveActionData.OldRotation);
    }
}
