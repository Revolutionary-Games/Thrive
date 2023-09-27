using System;
using Godot;

[JSONAlwaysDynamicType]
public class NewMicrobeActionData : EditorCombinableActionData<EarlyMulticellularSpecies>
{
    public OrganelleLayout<OrganelleTemplate> OldEditedMicrobeOrganelles;
    public MembraneType OldMembrane;
    public float OldMembraneRigidity;
    public BehaviourDictionary OldBehaviourValues;
    public Color OldMembraneColour;

    public NewMicrobeActionData(OrganelleLayout<OrganelleTemplate> oldEditedMicrobeOrganelles,
        MembraneType oldMembrane, float oldRigidity, Color oldColour, BehaviourDictionary oldBehaviourValues)
    {
        OldEditedMicrobeOrganelles = oldEditedMicrobeOrganelles;
        OldMembrane = oldMembrane;
        OldMembraneRigidity = oldRigidity;
        OldBehaviourValues = new BehaviourDictionary(oldBehaviourValues);
        OldMembraneColour = oldColour;
    }

    public override bool ResetsHistory => true;

    public override int CalculateCost()
    {
        return -Constants.BASE_MUTATION_POINTS;
    }

    protected override int CalculateCostInternal()
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
