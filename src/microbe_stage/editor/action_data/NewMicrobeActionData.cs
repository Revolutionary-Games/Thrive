using System;
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

    public override double CalculateCost()
    {
        return -Constants.BASE_MUTATION_POINTS;
    }

    protected override double CalculateCostInternal()
    {
        throw new NotSupportedException();
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        throw new NotSupportedException();
    }
}
