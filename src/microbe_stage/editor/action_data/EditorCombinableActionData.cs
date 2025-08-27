using System;
using System.Collections.Generic;

public abstract class EditorCombinableActionData : CombinableActionData
{
    public float CostMultiplier { get; set; } = 1.0f;

    public virtual double CalculateCost(IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        return Math.Min(CalculateCostInternal(history, insertPosition) * CostMultiplier, 100);
    }

    public virtual double GetBaseCost()
    {
        return Math.Min(CalculateBaseCostInternal() * CostMultiplier, 100);
    }

    protected abstract double CalculateCostInternal(IReadOnlyList<EditorCombinableActionData> history, int insertPosition);

    protected abstract double CalculateBaseCostInternal();
}

public abstract class EditorCombinableActionData<TContext> : EditorCombinableActionData
{
    /// <summary>
    ///   The optional context this action was performed in. This is additional data in addition to the action target.
    ///   Not all editors use context info.
    /// </summary>
    public TContext? Context { get; set; }

    public override bool WantsToMergeWith(CombinableActionData other)
    {
        // If the other action was performed in a different context, we can't combine with it
        if (other is EditorCombinableActionData<TContext> editorActionData)
        {
            // Null context is the same as another null context, but any existing context value doesn't equal null
            if ((Context is not null && editorActionData.Context is null) ||
                (Context is null && editorActionData.Context is not null))
            {
                return false;
            }

            if (Context is not null && !Context.Equals(editorActionData.Context))
            {
                return false;
            }
        }

        return base.WantsToMergeWith(other);
    }
}
