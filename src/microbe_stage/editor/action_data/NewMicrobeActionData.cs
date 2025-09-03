using System.Collections.Generic;
using Godot;

[JSONAlwaysDynamicType]
public class NewMicrobeActionData : EditorCombinableActionData<CellType>
{
    public OrganelleLayout<OrganelleTemplate> OldEditedMicrobeOrganelles;
    public MembraneType OldMembrane;
    public float OldMembraneRigidity;

    /// <summary>
    ///   Old behaviour values to restore. Doesn't exist in the multicellular editor as in that the cell editor doesn't
    ///   handle behaviour.
    /// </summary>
    public BehaviourDictionary? OldBehaviourValues;

    public EnvironmentalTolerances? OldTolerances;

    public Color OldMembraneColour;

    public NewMicrobeActionData(OrganelleLayout<OrganelleTemplate> oldEditedMicrobeOrganelles,
        MembraneType oldMembrane, float oldRigidity, Color oldColour, BehaviourDictionary? oldBehaviourValues,
        EnvironmentalTolerances? oldTolerances)
    {
        OldEditedMicrobeOrganelles = oldEditedMicrobeOrganelles;
        OldMembrane = oldMembrane;
        OldMembraneRigidity = oldRigidity;

        if (oldBehaviourValues != null)
            OldBehaviourValues = new BehaviourDictionary(oldBehaviourValues);

        if (oldTolerances != null)
        {
            OldTolerances = new EnvironmentalTolerances();
            OldTolerances.CopyFrom(oldTolerances);
        }

        OldMembraneColour = oldColour;
    }

    public override bool ResetsHistory => true;

    protected override double CalculateBaseCostInternal()
    {
        return 0;
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        return (CalculateBaseCostInternal(), Constants.BASE_MUTATION_POINTS);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
