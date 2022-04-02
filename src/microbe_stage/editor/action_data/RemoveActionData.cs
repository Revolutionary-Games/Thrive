[JSONAlwaysDynamicType]
public class RemoveActionData : MicrobeEditorCombinableActionData
{
    public OrganelleTemplate Organelle;
    public Hex Location;
    public int Orientation;

    /// <summary>
    ///   Used for replacing Cytoplasm. If true this action is free.
    /// </summary>
    public bool GotReplaced;

    public RemoveActionData(OrganelleTemplate organelle, Hex location, int orientation)
    {
        Organelle = organelle;
        Location = location;
        Orientation = orientation;
    }

    public override int CalculateCost()
    {
        return GotReplaced ? 0 : Constants.ORGANELLE_REMOVE_COST;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // If this organelle got placed in this session on the same position
        if (other is PlacementActionData placementActionData &&
            placementActionData.Organelle.Definition == Organelle.Definition)
        {
            // If this organelle got placed on the same position
            if (placementActionData.Location == Location)
                return ActionInterferenceMode.CancelsOut;

            // Removing an organelle and then placing it is a move operation
            return ActionInterferenceMode.Combinable;
        }

        // If this organelle got moved in this session
        if (other is MoveActionData moveActionData &&
            moveActionData.Organelle.Definition == Organelle.Definition &&
            moveActionData.NewLocation == Location)
            return ActionInterferenceMode.Combinable;

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is PlacementActionData placementActionData)
        {
            return new MoveActionData(placementActionData.Organelle,
                Location,
                placementActionData.Location,
                Orientation,
                placementActionData.Orientation);
        }

        var moveActionData = (MoveActionData)other;
        return new RemoveActionData(Organelle, moveActionData.OldLocation, moveActionData.OldRotation);
    }
}
