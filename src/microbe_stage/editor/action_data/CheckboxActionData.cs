[JSONAlwaysDynamicType]
public class CheckboxActionData : EditorCombinableActionData
{
    public bool Value;

    public CheckboxActionData(bool value)
    {
        Value = value;
    }

    protected override int CalculateCostInternal()
    {
        // Checkbox changes are currently free (TODO: change this?)
        return 0;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // Subsequent actions will always cancel out since a checkbox is a boolean toggle
        return ActionInterferenceMode.CancelsOut;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        return this;
    }
}
