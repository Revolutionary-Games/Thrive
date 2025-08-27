using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public abstract class EditorCombinableActionData : CombinableActionData
{
    protected double lastCalculatedCost = double.NaN;

    public float CostMultiplier { get; set; } = 1.0f;

    public virtual double CalculateCost(IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        return Math.Min((lastCalculatedCost = CalculateCostInternal(history, insertPosition)) * CostMultiplier, 100);
    }

    public virtual double GetBaseCost()
    {
        return Math.Min(CalculateBaseCostInternal() * CostMultiplier, 100);
    }

    /// <summary>
    ///   The last calculated cost of this action.
    ///   Updated by <see cref="CalculateCost"/>. This means that actions can only have their costs processed in
    ///   order, otherwise bad things will happen.
    /// </summary>
    /// <returns>The full realised cost of this action</returns>
    public virtual double GetCalculatedCost()
    {
        // TODO: resetting this to NaN for a whole action tree before calculating costs again would provide some extra
        // safety against bugs
        if (double.IsNaN(lastCalculatedCost))
        {
            GD.PrintErr("Trying to get the cost of an action before it has been calculated. " +
                "Things are being processed in the wrong order!");

            if (Debugger.IsAttached)
                Debugger.Break();

            return 0;
        }

        return Math.Min(lastCalculatedCost * CostMultiplier, 100);
    }

    protected abstract double CalculateCostInternal(IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition);

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

    public bool MatchesContext(EditorCombinableActionData<TContext> other)
    {
        if (Context is null)
            return other.Context is null;

        if (other.Context is null)
            return false;

        return Context.Equals(other.Context);
    }
}
