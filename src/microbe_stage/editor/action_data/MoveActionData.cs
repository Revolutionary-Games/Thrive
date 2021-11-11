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

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other)
    {
        // If this organelle got moved in the same session again
        if (other is MoveActionData moveActionData && moveActionData.Organelle.Definition == Organelle.Definition)
        {
            // If this organelle got moved back and forth
            if (OldLocation == moveActionData.NewLocation && NewLocation == moveActionData.OldLocation)
                return MicrobeActionInterferenceMode.CancelsOut;

            // If this organelle got moved twice
            if (moveActionData.NewLocation == OldLocation || NewLocation == moveActionData.OldLocation)
                return MicrobeActionInterferenceMode.Combinable;
        }

        // If this organelle got placed in this session
        if (other is PlacementActionData placementActionData &&
            placementActionData.Organelle.Definition == Organelle.Definition &&
            placementActionData.Location == OldLocation)
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
        return Constants.ORGANELLE_MOVE_COST;
    }

    protected override MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other)
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
