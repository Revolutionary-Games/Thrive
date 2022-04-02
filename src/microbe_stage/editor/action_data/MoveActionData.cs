using System;

[JSONAlwaysDynamicType]
public class MoveActionData : MicrobeEditorCombinableActionData
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

    public override ActionInterferenceMode GetInterferenceModeWith(CombinableActionData other)
    {
        // If this organelle got moved in the same session again
        if (other is MoveActionData moveActionData && moveActionData.Organelle.Definition == Organelle.Definition)
        {
            // If this organelle got moved back and forth
            if (OldLocation == moveActionData.NewLocation && NewLocation == moveActionData.OldLocation &&
                OldRotation == moveActionData.NewRotation && NewRotation == moveActionData.OldRotation)
                return ActionInterferenceMode.CancelsOut;

            // If this organelle got moved twice
            if ((moveActionData.NewLocation == OldLocation && moveActionData.NewRotation == OldRotation) ||
                (NewLocation == moveActionData.OldLocation && NewRotation == moveActionData.OldRotation))
                return ActionInterferenceMode.Combinable;
        }

        // If this organelle got placed in this session
        if (other is PlacementActionData placementActionData &&
            placementActionData.Organelle.Definition == Organelle.Definition &&
            placementActionData.Location == OldLocation &&
            placementActionData.Orientation == OldRotation)
            return ActionInterferenceMode.Combinable;

        // If this organelle got removed in this session
        if (other is RemoveActionData removeActionData &&
            removeActionData.Organelle.Definition == Organelle.Definition &&
            removeActionData.Location == NewLocation)
            return ActionInterferenceMode.ReplacesOther;

        return ActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        if (OldLocation == NewLocation && OldRotation == NewRotation)
            return 0;

        return Constants.ORGANELLE_MOVE_COST;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
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
