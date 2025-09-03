using System;
using System.Collections.Generic;

[JSONAlwaysDynamicType]
public class RigidityActionData : EditorCombinableActionData<CellType>
{
    public float NewRigidity;
    public float PreviousRigidity;

    public RigidityActionData(float newRigidity, float previousRigidity)
    {
        NewRigidity = newRigidity;
        PreviousRigidity = previousRigidity;
    }

    public static double CalculateRigidityCost(float newRigidity, float previousRigidity)
    {
        return Math.Abs(newRigidity - previousRigidity) * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO *
            Constants.MEMBRANE_RIGIDITY_COST_PER_STEP;
    }

    protected override double CalculateBaseCostInternal()
    {
        return CalculateRigidityCost(NewRigidity, PreviousRigidity);
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        bool foundOther = false;
        var cost = CalculateBaseCostInternal();
        double refund = 0;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            if (other is RigidityActionData rigidityChangeActionData && MatchesContext(rigidityChangeActionData))
            {
                // Calculate the cost as the total change and offset the previous action's cost by the change
                if (!foundOther)
                {
                    cost = CalculateRigidityCost(NewRigidity, rigidityChangeActionData.PreviousRigidity);
                    foundOther = true;
                }

                refund += other.GetCalculatedSelfCost();
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return other is RigidityActionData;
    }

    protected override void MergeGuaranteed(CombinableActionData other)
    {
        var rigidityChangeActionData = (RigidityActionData)other;

        if (Math.Abs(PreviousRigidity - rigidityChangeActionData.NewRigidity) < MathUtils.EPSILON)
        {
            // Handle cancels out
            if (Math.Abs(NewRigidity - rigidityChangeActionData.PreviousRigidity) < MathUtils.EPSILON)
            {
                NewRigidity = rigidityChangeActionData.NewRigidity;
                return;
            }

            PreviousRigidity = rigidityChangeActionData.PreviousRigidity;
            return;
        }

        NewRigidity = rigidityChangeActionData.NewRigidity;
    }
}
