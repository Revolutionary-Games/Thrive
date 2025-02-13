[JSONAlwaysDynamicType]
public class ToleranceActionData : EditorCombinableActionData
{
    public EnvironmentalTolerances OldTolerances;
    public EnvironmentalTolerances NewTolerances;

    protected override int CalculateCostInternal()
    {
        throw new System.NotImplementedException();
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        if (other is not ToleranceActionData toleranceActionData)
            return ActionInterferenceMode.NoInterference;

        // If changed back to original then the actions cancel out
        if (OldTolerances.Equals(toleranceActionData.NewTolerances))
            return ActionInterferenceMode.CancelsOut;

        return ActionInterferenceMode.Combinable;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        var otherData = (ToleranceActionData)other;

        return new ToleranceActionData
        {
            OldTolerances = OldTolerances,
            NewTolerances = otherData.NewTolerances,
        };
    }
}
