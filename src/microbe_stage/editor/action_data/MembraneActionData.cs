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

    public static double CalculateCost(MembraneType oldMembrane, MembraneType newMembrane)
    {
        if (oldMembrane == newMembrane)
            return 0;

        return newMembrane.EditorCost;
    }

    protected override double CalculateBaseCostInternal()
    {
        return CalculateCost(OldMembrane, NewMembrane);
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        var cost = CalculateBaseCostInternal();
        double refund = 0;
        bool seenOther = false;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // If the membrane got changed again
            if (other is MembraneActionData membraneActionData && MatchesContext(membraneActionData))
            {
                if (!seenOther)
                {
                    seenOther = true;
                    cost = CalculateCost(membraneActionData.OldMembrane, NewMembrane);
                }

                refund += other.GetCalculatedSelfCost();
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
