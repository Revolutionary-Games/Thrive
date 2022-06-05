﻿using System;

public abstract class EditorCombinableActionData : CombinableActionData
{
    public float CostMultiplier { get; set; } = 1.0f;

    public virtual int CalculateCost()
    {
        return (int)(CalculateCostInternal() * CostMultiplier);
    }

    public override CombinableActionData Combine(CombinableActionData other)
    {
        if (other is not EditorCombinableActionData)
            throw new NotSupportedException("Can't combine editor combinable data with base combinable data object");

        // We override the behaviour here so that we can pass on our cost multiplier
        var combined = (EditorCombinableActionData)base.Combine(other);
        combined.CostMultiplier = CostMultiplier;
        return combined;
    }

    protected abstract int CalculateCostInternal();
}
