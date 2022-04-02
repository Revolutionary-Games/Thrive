using System;

[JSONAlwaysDynamicType]
public class NewMicrobeActionData : MicrobeEditorCombinableActionData
{
    public OrganelleLayout<OrganelleTemplate> OldEditedMicrobeOrganelles;
    public MembraneType OldMembrane;

    public NewMicrobeActionData(OrganelleLayout<OrganelleTemplate> oldEditedMicrobeOrganelles, MembraneType oldMembrane)
    {
        OldEditedMicrobeOrganelles = oldEditedMicrobeOrganelles;
        OldMembrane = oldMembrane;
    }

    public override bool ResetsHistory => true;

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(CombinableActionData other)
    {
        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return -Constants.BASE_MUTATION_POINTS;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        throw new NotImplementedException();
    }
}
