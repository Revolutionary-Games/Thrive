using System.Collections.Generic;

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

    protected override double CalculateCostInternal(IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition)
    {
        return 0;
    }

    protected override double CalculateBaseCostInternal()
    {
        return 0;
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
