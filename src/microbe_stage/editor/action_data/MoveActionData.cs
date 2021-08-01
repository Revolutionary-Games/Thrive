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
            if (OldLocation == moveActionData.NewLocation && NewLocation == moveActionData.OldLocation)
                return MicrobeActionInterferenceMode.CancelsOut;
            if (moveActionData.NewLocation == OldLocation || NewLocation == moveActionData.OldLocation)
                return MicrobeActionInterferenceMode.Combinable;
        }

        // If this organelle got placed in this session
        if (other is PlacementActionData placementActionData && placementActionData.Organelle == Organelle)
            return MicrobeActionInterferenceMode.Combinable;

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return Constants.ORGANELLE_MOVE_COST;
    }

    protected override MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other)
    {
        if (other is PlacementActionData placementActionData)
        {
            return new PlacementActionData(placementActionData.Organelle, NewLocation, NewRotation)
            {
                ReplacedCytoplasm = placementActionData.ReplacedCytoplasm,
            };
        }

        var moveActionData = (MoveActionData)other;
        if (moveActionData.NewLocation == OldLocation)
        {
            return new MoveActionData(Organelle, moveActionData.OldLocation, NewLocation, moveActionData.OldRotation,
                NewRotation);
        }

        return new MoveActionData(moveActionData.Organelle, OldLocation, moveActionData.NewLocation, OldRotation,
            moveActionData.NewRotation);
    }
}
