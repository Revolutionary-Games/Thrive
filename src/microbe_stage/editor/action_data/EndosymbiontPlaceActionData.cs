using System;

[JSONAlwaysDynamicType]
public class EndosymbiontPlaceActionData : EditorCombinableActionData<CellType>
{
    public OrganelleTemplate PlacedOrganelle;
    public bool PerformedUnlock;

    public Hex PlacementLocation;
    public int PlacementRotation;

    /// <summary>
    ///   The related endosymbiosis data. Required to be able to fully roll back the editor state
    /// </summary>
    public EndosymbiosisData.InProgressEndosymbiosis RelatedEndosymbiosisAction;

    /// <summary>
    ///   When not null, undoing this action required replacing endosymbiosis action, which is stored here for redo
    ///   purposes
    /// </summary>
    public EndosymbiosisData.InProgressEndosymbiosis? OverriddenEndosymbiosisOnUndo;

    public EndosymbiontPlaceActionData(OrganelleTemplate placedOrganelle, Hex placementLocation, int placementRotation,
        EndosymbiosisData.InProgressEndosymbiosis relatedEndosymbiosisAction)
    {
        PlacedOrganelle = placedOrganelle;
        PlacementLocation = placementLocation;
        PlacementRotation = placementRotation;
        RelatedEndosymbiosisAction = relatedEndosymbiosisAction;
    }

    public EndosymbiontPlaceActionData(EndosymbiosisData.InProgressEndosymbiosis fromEndosymbiosisData) : this(
        new OrganelleTemplate(fromEndosymbiosisData.TargetOrganelle, new Hex(0, 0), 0),
        new Hex(0, 0), 0, fromEndosymbiosisData)
    {
    }

    protected override int CalculateCostInternal()
    {
        // Endosymbiosis placement never costs MP
        return 0;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // Endosymbionts can be moved for free after placing
        if (other is OrganelleMoveActionData moveActionData)
        {
            // If moved after placing
            if (moveActionData.MovedHex == PlacedOrganelle &&
                moveActionData.OldLocation == PlacementLocation &&
                moveActionData.OldRotation == PlacementRotation)
            {
                return ActionInterferenceMode.Combinable;
            }
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is OrganelleMoveActionData moveActionData)
        {
            return new EndosymbiontPlaceActionData(PlacedOrganelle, moveActionData.NewLocation,
                moveActionData.NewRotation, RelatedEndosymbiosisAction)
            {
                PerformedUnlock = PerformedUnlock,
                OverriddenEndosymbiosisOnUndo = OverriddenEndosymbiosisOnUndo,
            };
        }

        throw new NotSupportedException();
    }
}
