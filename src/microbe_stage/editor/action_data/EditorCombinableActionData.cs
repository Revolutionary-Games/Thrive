using System;
using Godot;

public abstract class EditorCombinableActionData : CombinableActionData
{
    public float CostMultiplier { get; set; } = 1.0f;

    /// <summary>
    ///   Calculates cost adjusted by <see cref="CostMultiplier"/> and capped at a minimum and maximum.
    /// </summary>
    /// <returns>
    ///   The calculated adjusted cost.
    /// </returns>
    public virtual float CalculateCost()
    {
        return Mathf.Clamp(CalculateCostInternal() * CostMultiplier, Constants.MINIMUM_MUTATION_POINTS_COST,
            Constants.BASE_MUTATION_POINTS);
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

    protected abstract float CalculateCostInternal();
}
