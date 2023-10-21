using System;

/// <summary>
///   Stores information needed to change the reproduction order of cells in an <see cref="EarlyMulticellularSpecies"/>.
/// </summary>
[JSONAlwaysDynamicType]
public class ReproductionOrderActionData : EditorCombinableActionData<EarlyMulticellularSpecies>
{
    public readonly int OldIndex;
    public readonly int NewIndex;

    public ReproductionOrderActionData(int oldIndex, int newIndex)
    {
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        throw new NotSupportedException();
    }

    protected override int CalculateCostInternal()
    {
        // Changing reproduction order is free
        return 0;
    }
}
