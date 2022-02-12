[JSONAlwaysDynamicType]
public class MembraneActionData : MicrobeEditorActionData
{
    public MembraneType OldMembrane;
    public MembraneType NewMembrane;

    public MembraneActionData(MembraneType oldMembrane, MembraneType newMembrane)
    {
        OldMembrane = oldMembrane;
        NewMembrane = newMembrane;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(ActionData other)
    {
        if (other is MembraneActionData membraneActionData)
        {
            // If changed back to the old membrane
            if (membraneActionData.NewMembrane == OldMembrane && NewMembrane == membraneActionData.OldMembrane)
                return MicrobeActionInterferenceMode.CancelsOut;

            // If changed membrane twice
            if (membraneActionData.NewMembrane == OldMembrane || NewMembrane == membraneActionData.OldMembrane)
                return MicrobeActionInterferenceMode.Combinable;
        }

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return NewMembrane.EditorCost;
    }

    protected override ActionData CombineGuaranteed(ActionData other)
    {
        var membraneActionData = (MembraneActionData)other;
        if (OldMembrane == membraneActionData.NewMembrane)
            return new MembraneActionData(membraneActionData.OldMembrane, NewMembrane);

        return new MembraneActionData(membraneActionData.NewMembrane, OldMembrane);
    }
}
