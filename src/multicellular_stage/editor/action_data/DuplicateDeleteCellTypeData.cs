using System;

/// <summary>
///   Stores information for duplicating and deleting cell types.
/// </summary>
[JSONAlwaysDynamicType]
public class DuplicateDeleteCellTypeData : EditorCombinableActionData<MulticellularSpecies>
{
    public readonly CellType CellType;

    public DuplicateDeleteCellTypeData(CellType cellType)
    {
        CellType = cellType;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        throw new NotSupportedException();
    }

    protected override double CalculateCostInternal()
    {
        return 0;
    }
}
