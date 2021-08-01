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

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other)
    {
        if (other is MembraneActionData membraneActionData)
        {
            if (membraneActionData.NewMembrane == OldMembrane && NewMembrane == membraneActionData.OldMembrane)
                return MicrobeActionInterferenceMode.CancelsOut;

            if (membraneActionData.NewMembrane == OldMembrane || NewMembrane == membraneActionData.OldMembrane)
                return MicrobeActionInterferenceMode.Combinable;
        }

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return NewMembrane.EditorCost;
    }

    protected override MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other)
    {
        var membraneActionData = (MembraneActionData)other;
        if (OldMembrane == membraneActionData.NewMembrane)
            return new MembraneActionData(membraneActionData.OldMembrane, NewMembrane);

        return new MembraneActionData(membraneActionData.NewMembrane, OldMembrane);
    }
}
