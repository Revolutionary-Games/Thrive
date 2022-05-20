using System;

/// <summary>
///   This exists purely to make complex undo/redo structures correctly apply changes to the right cell types
/// </summary>
public class StartCellTypeEditActionData : EditorCombinableActionData
{
    public CellType StartedCellTypeEdit;

    public StartCellTypeEditActionData(CellType startedCellTypeEdit)
    {
        StartedCellTypeEdit = startedCellTypeEdit;
    }

    protected override int CalculateCostInternal()
    {
        return 0;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        throw new NotSupportedException();
    }
}
