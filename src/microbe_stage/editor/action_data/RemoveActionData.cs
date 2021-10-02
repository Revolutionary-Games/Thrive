[JSONAlwaysDynamicType]
public class RemoveActionData : MicrobeEditorActionData
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

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other)
    {
        // If this organelle got placed in this session on the same position
        if (other is PlacementActionData placementActionData &&
            placementActionData.Organelle.Definition == Organelle.Definition)
        {
            // If this organelle got placed on the same position
            if (placementActionData.Location == Location)
                return MicrobeActionInterferenceMode.CancelsOut;

            // Removing an organelle and then placing it is a move operation
            return MicrobeActionInterferenceMode.Combinable;
        }

        // If this organelle got moved in this session
        if (other is MoveActionData moveActionData &&
            moveActionData.Organelle.Definition == Organelle.Definition &&
            moveActionData.NewLocation == Location)
            return MicrobeActionInterferenceMode.ReplacesOther;

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return GotReplaced ? 0 : Constants.ORGANELLE_REMOVE_COST;
    }

    protected override MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other)
    {
        var placementActionData = (PlacementActionData)other;
        return new MoveActionData(placementActionData.Organelle,
            Location,
            placementActionData.Location,
            Orientation,
            placementActionData.Orientation);
    }
}
