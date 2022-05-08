using System.Collections.Generic;

[JSONAlwaysDynamicType]
public class PlacementActionData : EditorCombinableActionData
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

    protected override int CalculateCostInternal()
    {
        return Organelle.Definition.MPCost;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // If this organelle got removed in this session
        if (other is RemoveActionData removeActionData && removeActionData.Organelle.Definition == Organelle.Definition)
        {
            // If the placed organelle has been placed on the same position where it got removed before
            if (removeActionData.Location == Location)
                return ActionInterferenceMode.CancelsOut;

            // Removing and placing an organelle is a move operation
            return ActionInterferenceMode.Combinable;
        }

        if (other is OrganelleMoveActionData moveActionData &&
            moveActionData.MovedHex.Definition == Organelle.Definition)
        {
            if (moveActionData.OldLocation == Location)
                return ActionInterferenceMode.Combinable;

            if (ReplacedCytoplasm?.Contains(moveActionData.MovedHex) == true)
                return ActionInterferenceMode.ReplacesOther;
        }

        if (other is PlacementActionData placementActionData &&
            ReplacedCytoplasm?.Contains(placementActionData.Organelle) == true)
            return ActionInterferenceMode.ReplacesOther;

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is RemoveActionData removeActionData)
        {
            return new OrganelleMoveActionData(removeActionData.Organelle, removeActionData.Location, Location,
                removeActionData.Orientation, Orientation);
        }

        var moveActionData = (OrganelleMoveActionData)other;
        return new PlacementActionData(Organelle, moveActionData.NewLocation, moveActionData.NewRotation);
    }
}
