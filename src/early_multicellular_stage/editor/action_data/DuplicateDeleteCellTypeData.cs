using System;

/// <summary>
///   Stores information for duplicating and deleting cell types.
/// </summary>
[JSONAlwaysDynamicType]
public class DuplicateDeleteCellTypeData : EditorCombinableActionData<EarlyMulticellularSpecies>
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

    protected override int CalculateCostInternal()
    {
        return 0;
    }
}
