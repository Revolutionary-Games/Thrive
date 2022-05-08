using System;

[JSONAlwaysDynamicType]
public class NewMicrobeActionData : EditorCombinableActionData
{
    public OrganelleLayout<OrganelleTemplate> OldEditedMicrobeOrganelles;
    public MembraneType OldMembrane;

    public NewMicrobeActionData(OrganelleLayout<OrganelleTemplate> oldEditedMicrobeOrganelles, MembraneType oldMembrane)
    {
        OldEditedMicrobeOrganelles = oldEditedMicrobeOrganelles;
        OldMembrane = oldMembrane;
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
