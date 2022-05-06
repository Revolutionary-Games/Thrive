[JSONAlwaysDynamicType]
public class MembraneActionData : EditorCombinableActionData
{
    public MembraneType OldMembrane;
    public MembraneType NewMembrane;

    public MembraneActionData(MembraneType oldMembrane, MembraneType newMembrane)
    {
        OldMembrane = oldMembrane;
        NewMembrane = newMembrane;
    }

    public override int CalculateCost()
    {
        return NewMembrane.EditorCost;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        if (other is MembraneActionData membraneActionData)
        {
            // If changed back to the old membrane
            if (membraneActionData.NewMembrane == OldMembrane && NewMembrane == membraneActionData.OldMembrane)
                return ActionInterferenceMode.CancelsOut;

            // If changed membrane twice
            if (membraneActionData.NewMembrane == OldMembrane || NewMembrane == membraneActionData.OldMembrane)
                return ActionInterferenceMode.Combinable;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        var membraneActionData = (MembraneActionData)other;
        if (OldMembrane == membraneActionData.NewMembrane)
            return new MembraneActionData(membraneActionData.OldMembrane, NewMembrane);

        return new MembraneActionData(membraneActionData.NewMembrane, OldMembrane);
    }
}
