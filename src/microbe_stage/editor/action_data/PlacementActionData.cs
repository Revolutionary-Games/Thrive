using System.Collections.Generic;

[JSONAlwaysDynamicType]
public class PlacementActionData : MicrobeEditorCombinableActionData
{
    public List<OrganelleTemplate>? ReplacedCytoplasm;
    public OrganelleTemplate Organelle;
    public Hex Location;
    public int Orientation;

    public PlacementActionData(OrganelleTemplate organelle, Hex location, int orientation)
    {
        Organelle = organelle;
        Location = location;
        Orientation = orientation;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(CombinableActionData other)
    {
        // If this organelle got removed in this session
        if (other is RemoveActionData removeActionData && removeActionData.Organelle.Definition == Organelle.Definition)
        {
            // If the placed organelle has been placed on the same position where it got removed before
            if (removeActionData.Location == Location)
                return MicrobeActionInterferenceMode.CancelsOut;

            // Removing and placing an organelle is a move operation
            return MicrobeActionInterferenceMode.Combinable;
        }

        if (other is MoveActionData moveActionData && moveActionData.Organelle.Definition == Organelle.Definition)
        {
            if (moveActionData.OldLocation == Location)
                return MicrobeActionInterferenceMode.Combinable;

            if (ReplacedCytoplasm?.Contains(moveActionData.Organelle) == true)
                return MicrobeActionInterferenceMode.ReplacesOther;
        }

        if (other is PlacementActionData placementActionData &&
            ReplacedCytoplasm?.Contains(placementActionData.Organelle) == true)
            return MicrobeActionInterferenceMode.ReplacesOther;

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return Organelle.Definition.MPCost;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is RemoveActionData removeActionData)
        {
            return new MoveActionData(removeActionData.Organelle, removeActionData.Location, Location,
                removeActionData.Orientation, Orientation);
        }

        var moveActionData = (MoveActionData)other;
        return new PlacementActionData(Organelle, moveActionData.NewLocation, moveActionData.NewRotation);
    }
}
