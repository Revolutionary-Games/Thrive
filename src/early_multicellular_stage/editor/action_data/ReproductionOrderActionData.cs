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
        // TODO if an MP cost is ever added to reproduction order actions, we'll have to come up with some way to
        // combine them. Unfortunately, because each reproduction order change affects two hexes (the hex at the old
        // index and the hex at the new index), coming up with a good way to combine them isn't easy. The best approach
        // might be to just combine all reproduction order actions regardless of which hexes they affect.
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
