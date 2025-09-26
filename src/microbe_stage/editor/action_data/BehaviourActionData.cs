using System;
using System.Collections.Generic;

[JSONAlwaysDynamicType]
public class BehaviourActionData : EditorCombinableActionData
{
    public float NewValue;
    public float OldValue;
    public BehaviouralValueType Type;

    public BehaviourActionData(float newValue, float oldValue, BehaviouralValueType type)
    {
        OldValue = oldValue;
        NewValue = newValue;
        Type = type;
    }

    protected override double CalculateBaseCostInternal()
    {
        // TODO: add a cost for this. CalculateCostInternal needs tweaking to handle this
        return 0;
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

            if (other is BehaviourActionData behaviourChangeActionData && behaviourChangeActionData.Type == Type)
            {
                // If the value has been changed back to a previous value
                if (Math.Abs(NewValue - behaviourChangeActionData.OldValue) < MathUtils.EPSILON &&
                    Math.Abs(behaviourChangeActionData.NewValue - OldValue) < MathUtils.EPSILON)
                {
                    cost = 0;
                    refund += other.GetCalculatedSelfCost();
                    continue;
                }

                // If the value has been changed twice
                if (Math.Abs(NewValue - behaviourChangeActionData.OldValue) < MathUtils.EPSILON ||
                    Math.Abs(behaviourChangeActionData.NewValue - OldValue) < MathUtils.EPSILON)
                {
                    if (!seenOther)
                    {
                        seenOther = true;

                        // TODO: need to calculate real total cost from the other old value to our new value once
                        // there are costs and not just 0
                        // cost = CalculateBehaviourCost(behaviourChangeActionData.OldValue, NewValue);
                        cost = 0;
                    }

                    refund += other.GetCalculatedSelfCost();
                }
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        if (other is not BehaviourActionData otherBehaviour)
            return false;

        // Only combine the same type. Otherwise, terrible bugs happen.
        return otherBehaviour.Type == Type;
    }

    protected override void MergeGuaranteed(CombinableActionData other)
    {
        var behaviourChangeActionData = (BehaviourActionData)other;

        if (Math.Abs(OldValue - behaviourChangeActionData.NewValue) < MathUtils.EPSILON)
        {
            // Handle cancels out
            if (Math.Abs(NewValue - behaviourChangeActionData.OldValue) < MathUtils.EPSILON)
            {
                NewValue = behaviourChangeActionData.NewValue;
                return;
            }

            OldValue = behaviourChangeActionData.OldValue;
            return;
        }

        NewValue = behaviourChangeActionData.NewValue;
    }
}
