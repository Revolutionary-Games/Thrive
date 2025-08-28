using System.Collections.Generic;
using Newtonsoft.Json;

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
    ///   When not null, undoing this action required replacing the endosymbiosis action, which is stored here for redo
    ///   purposes
    /// </summary>
    public EndosymbiosisData.InProgressEndosymbiosis? OverriddenEndosymbiosisOnUndo;

    [JsonConstructor]
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

    protected override double CalculateBaseCostInternal()
    {
        // Endosymbiosis placement never costs MP
        return 0;
    }

    protected override double CalculateCostInternal(IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition)
    {
        // No cost adjustment as this is free
        return CalculateBaseCostInternal();
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
