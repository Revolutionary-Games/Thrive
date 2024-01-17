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
    ///   The optional context this action was performed in. This is additional data in addition to the action target.
    ///   Not all editors use context info.
    /// </summary>
    public TContext? Context { get; set; }

    public override ActionInterferenceMode GetInterferenceModeWith(CombinableActionData other)
    {
        // If the other action was performed on a different context, we can't combine with it
        if (other is EditorCombinableActionData<TContext> editorActionData)
        {
            // Null context is the same as another null context but any existing context value doesn't equal null
            if ((Context is not null && editorActionData.Context is null) ||
                (Context is null && editorActionData.Context is not null))
            {
                return ActionInterferenceMode.NoInterference;
            }

            if (Context is not null && !Context.Equals(editorActionData.Context))
            {
                return ActionInterferenceMode.NoInterference;
            }
        }

        return base.GetInterferenceModeWith(other);
    }

    public override CombinableActionData Combine(CombinableActionData other)
    {
        var combined = (EditorCombinableActionData<TContext>)base.Combine(other);

        // We need to pass the context along to the combined data. This is fine to just copy our side of the context
        // as it was checked before allowing combine that the context matches.
        combined.Context = Context;

        return combined;
    }
}
