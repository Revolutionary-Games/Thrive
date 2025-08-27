using System;
using System.Collections.Generic;

[JSONAlwaysDynamicType]
public class MembraneActionData : EditorCombinableActionData<CellType>
{
    public MembraneType OldMembrane;
    public MembraneType NewMembrane;

    public MembraneActionData(MembraneType oldMembrane, MembraneType newMembrane)
    {
        OldMembrane = oldMembrane;
        NewMembrane = newMembrane;
    }

    protected override double CalculateBaseCostInternal()
    {
        if (OldMembrane == NewMembrane)
            return 0;

        return NewMembrane.EditorCost;
    }

    protected override double CalculateCostInternal(IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition)
    {
        var cost = CalculateBaseCostInternal();

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // If the membrane got changed again
            if (other is MembraneActionData membraneActionData && MatchesContext(membraneActionData))
            {
                cost = Math.Min(-other.GetCalculatedCost(), cost);
            }
        }

        return cost;
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
