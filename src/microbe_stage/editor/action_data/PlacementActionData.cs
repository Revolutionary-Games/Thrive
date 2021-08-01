using System.Collections.Generic;

[JSONAlwaysDynamicType]
public class PlacementActionData : MicrobeEditorActionData
{
    public List<OrganelleTemplate> ReplacedCytoplasm;
    public OrganelleTemplate Organelle;
    public Hex Location;
    public int Orientation;

    public PlacementActionData(OrganelleTemplate organelle, Hex location, int orientation)
    {
        Organelle = organelle;
        Location = location;
        Orientation = orientation;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other)
    {
        // If this organelle got removed in this session
        if (other is RemoveActionData removeActionData && removeActionData.Organelle.Definition == Organelle.Definition)
        {
            if (removeActionData.Location == Location)
                return MicrobeActionInterferenceMode.CancelsOut;

            return MicrobeActionInterferenceMode.Combinable;
        }

        if (other is MoveActionData moveActionData && ReplacedCytoplasm?.Contains(moveActionData.Organelle) == true)
            return MicrobeActionInterferenceMode.ReplacesOther;

        if (other is PlacementActionData placementActionData &&
            ReplacedCytoplasm?.Contains(placementActionData.Organelle) == true)
            return MicrobeActionInterferenceMode.ReplacesOther;

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return Organelle.Definition.MPCost;
    }

    protected override MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other)
    {
        var removeActionData = (RemoveActionData)other;
        return new MoveActionData(removeActionData.Organelle, removeActionData.Location, Location,
            removeActionData.Orientation, Orientation);
    }
}
