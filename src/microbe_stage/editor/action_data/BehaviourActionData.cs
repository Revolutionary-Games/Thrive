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

    protected override double CalculateCostInternal(IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition)
    {
        var cost = CalculateBaseCostInternal();

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
                    cost = Math.Min(-other.GetCalculatedCost(), cost);
                    continue;
                }

                // If the value has been changed twice
                if (Math.Abs(NewValue - behaviourChangeActionData.OldValue) < MathUtils.EPSILON ||
                    Math.Abs(behaviourChangeActionData.NewValue - OldValue) < MathUtils.EPSILON)
                {
                    // TODO: calculate the new total change and return that (minus the already paid other cost)
                    // return ActionInterferenceMode.Combinable;
                }
            }
        }

        return cost;
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return other is BehaviourActionData;
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
