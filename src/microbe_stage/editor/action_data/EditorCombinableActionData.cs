using System;

public abstract class EditorCombinableActionData : CombinableActionData
{
    public float CostMultiplier { get; set; } = 1.0f;

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

public abstract class EditorCombinableActionData<TContext> : EditorCombinableActionData
{
    /// <summary>
    ///   What this action was performed on.
    /// </summary>
    public TContext? Context { get; set; }

    public override ActionInterferenceMode GetInterferenceModeWith(CombinableActionData other)
    {
        // If the other action was performed on a different context, we can't combine with it
        if (other is not EditorCombinableActionData<TContext> editorActionData || editorActionData.Context is null ||
            !editorActionData.Context.Equals(Context))
            return ActionInterferenceMode.NoInterference;

        return base.GetInterferenceModeWith(other);
    }
}
