using System;

public abstract class EditorCombinableActionData : CombinableActionData
{
    public float CostMultiplier { get; set; } = 1.0f;

    /// <summary>
    ///   The <see cref="CellType"/> the action was performed on.
    /// </summary>
    /// <remarks>
    ///   In the microbe stage, this will always be null because there's only one <see cref="CellType"/>.
    /// </remarks>
    public CellType? CellType { get; set; }

    public virtual int CalculateCost()
    {
        return (int)Math.Min(CalculateCostInternal() * CostMultiplier, 100);
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
