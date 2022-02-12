using System;

[JSONAlwaysDynamicType]
public class MoveActionData : MicrobeEditorActionData
{
    public OrganelleTemplate Organelle;
    public Hex OldLocation;
    public Hex NewLocation;
    public int OldRotation;
    public int NewRotation;

    public MoveActionData(OrganelleTemplate organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation)
    {
        Organelle = organelle;
        OldLocation = oldLocation;
        NewLocation = newLocation;
        OldRotation = oldRotation;
        NewRotation = newRotation;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(ActionData other)
    {
        // If this organelle got moved in the same session again
        if (other is MoveActionData moveActionData && moveActionData.Organelle.Definition == Organelle.Definition)
        {
            // If this organelle got moved back and forth
            if (OldLocation == moveActionData.NewLocation && NewLocation == moveActionData.OldLocation &&
                OldRotation == moveActionData.NewRotation && NewRotation == moveActionData.OldRotation)
                return MicrobeActionInterferenceMode.CancelsOut;

            // If this organelle got moved twice
            if ((moveActionData.NewLocation == OldLocation && moveActionData.NewRotation == OldRotation) ||
                (NewLocation == moveActionData.OldLocation && NewRotation == moveActionData.OldRotation))
                return MicrobeActionInterferenceMode.Combinable;
        }

        // If this organelle got placed in this session
        if (other is PlacementActionData placementActionData &&
            placementActionData.Organelle.Definition == Organelle.Definition &&
            placementActionData.Location == OldLocation &&
            placementActionData.Orientation == OldRotation)
            return MicrobeActionInterferenceMode.Combinable;

        // If this organelle got removed in this session
        if (other is RemoveActionData removeActionData &&
            removeActionData.Organelle.Definition == Organelle.Definition &&
            removeActionData.Location == NewLocation)
            return MicrobeActionInterferenceMode.ReplacesOther;

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        if (OldLocation == NewLocation && OldRotation == NewRotation)
            return 0;

        return Constants.ORGANELLE_MOVE_COST;
    }

    protected override ActionData CombineGuaranteed(ActionData other)
    {
        return other switch
        {
            PlacementActionData placementActionData => new PlacementActionData(placementActionData.Organelle,
                NewLocation, NewRotation) { ReplacedCytoplasm = placementActionData.ReplacedCytoplasm },

            MoveActionData moveActionData when moveActionData.NewLocation == OldLocation => new MoveActionData(
                Organelle, moveActionData.OldLocation, NewLocation, moveActionData.OldRotation, NewRotation),

            MoveActionData moveActionData => new MoveActionData(moveActionData.Organelle, OldLocation,
                moveActionData.NewLocation, OldRotation, moveActionData.NewRotation),

            _ => throw new NotSupportedException(),
        };
    }
}
