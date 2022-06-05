using System;

public class EndCellTypeEditActionData : EditorCombinableActionData
{
    public CellType FinishedCellType;

    public EndCellTypeEditActionData(CellType finishedCellType)
    {
        FinishedCellType = finishedCellType;
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
