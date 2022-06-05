using System;

[JSONAlwaysDynamicType]
public class NewMicrobeActionData : EditorCombinableActionData
{
    public OrganelleLayout<OrganelleTemplate> OldEditedMicrobeOrganelles;
    public MembraneType OldMembrane;
    public float OldMembraneRigidity;
    public BehaviourDictionary OldBehaviourValues;

    public NewMicrobeActionData(OrganelleLayout<OrganelleTemplate> oldEditedMicrobeOrganelles, MembraneType oldMembrane,
        float oldRigidity, BehaviourDictionary oldBehaviourValues)
    {
        OldEditedMicrobeOrganelles = oldEditedMicrobeOrganelles;
        OldMembrane = oldMembrane;
        OldMembraneRigidity = oldRigidity;
        OldBehaviourValues = new BehaviourDictionary(oldBehaviourValues);
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
        throw new NotImplementedException();
    }
}
