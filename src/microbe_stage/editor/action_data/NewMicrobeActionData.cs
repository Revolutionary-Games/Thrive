using System;

[JSONAlwaysDynamicType]
public class NewMicrobeActionData : MicrobeEditorActionData
{
    public OrganelleLayout<OrganelleTemplate> OldEditedMicrobeOrganelles;
    public MembraneType OldMembrane;

    public NewMicrobeActionData(OrganelleLayout<OrganelleTemplate> oldEditedMicrobeOrganelles, MembraneType oldMembrane)
    {
        OldEditedMicrobeOrganelles = oldEditedMicrobeOrganelles;
        OldMembrane = oldMembrane;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(ActionData other)
    {
        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return -Constants.BASE_MUTATION_POINTS;
    }

    protected override ActionData CombineGuaranteed(ActionData other)
    {
        throw new NotImplementedException();
    }
}
