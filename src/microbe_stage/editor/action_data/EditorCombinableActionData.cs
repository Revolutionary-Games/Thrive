using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using SharedBase.Archive;

public abstract class EditorCombinableActionData : CombinableActionData
{
    public const ushort SERIALIZATION_VERSION_EDITOR = 1;

    protected double lastCalculatedSelfCost = double.NaN;

    /// <summary>
    ///   Note that this is *positive* and in most uses needs to be negated when applying to be correct
    /// </summary>
    protected double lastCalculatedRefundCost = double.NaN;

    protected double calculatedAvailableRefund = double.NaN;

    public float CostMultiplier { get; set; } = 1.0f;

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(CostMultiplier);
    }

    public virtual double CalculateCost(IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        (lastCalculatedSelfCost, lastCalculatedRefundCost) = CalculateCostInternal(history, insertPosition);
        calculatedAvailableRefund = lastCalculatedSelfCost * CostMultiplier;

        return Math.Min((lastCalculatedSelfCost - lastCalculatedRefundCost) * CostMultiplier, 100);
    }

    public virtual double GetBaseCost()
    {
        return Math.Min(CalculateBaseCostInternal() * CostMultiplier, 100);
    }

    /// <summary>
    ///   The last calculated *self* cost of this action, which doesn't take refunds into account.
    ///   Updated by <see cref="CalculateCost"/>. This means that actions can only have their costs processed in
    ///   order, otherwise bad things will happen.
    /// </summary>
    /// <returns>
    ///   The full realised cost of this action (note that this is not scaled by <see cref="CostMultiplier"/>
    /// </returns>
    public virtual double GetCalculatedSelfCost()
    {
        // TODO: resetting this to NaN for a whole action tree before calculating costs again would provide some extra
        // safety against bugs
        if (double.IsNaN(lastCalculatedSelfCost))
        {
            GD.PrintErr("Trying to get the cost of an action before it has been calculated. " +
                "Things are being processed in the wrong order!");

            if (Debugger.IsAttached)
                Debugger.Break();

            return 0;
        }

        return Math.Min(lastCalculatedSelfCost, 100);
    }

    /// <summary>
    ///   New primary method for other actions to refund costs of other actions (for example, deleting something that
    ///   was previously placed)
    /// </summary>
    /// <returns>
    ///   The amount of available refund left (and then it is set to 0 for the next call).
    ///   Returns 0 if cost hasn't been calculated yet (but prints an error).
    /// </returns>
    public virtual double GetAndConsumeAvailableRefund()
    {
        if (double.IsNaN(calculatedAvailableRefund))
        {
            GD.PrintErr("Trying to get and consume the refund cost of an action before it has been calculated. " +
                "Things are being processed in the wrong order!");

            if (Debugger.IsAttached)
                Debugger.Break();

            return 0;
        }

        var refund = calculatedAvailableRefund;
        calculatedAvailableRefund = 0;
        return Math.Min(refund, 100);
    }

    public virtual double GetCalculatedRefundCost()
    {
        if (double.IsNaN(lastCalculatedRefundCost))
        {
            GD.PrintErr("Trying to get the refund cost of an action before it has been calculated. " +
                "Things are being processed in the wrong order!");

            if (Debugger.IsAttached)
                Debugger.Break();

            return 0;
        }

        return Math.Min(lastCalculatedRefundCost, 100);
    }

    /// <summary>
    ///   Effective cost that's been calculated already.
    ///   Made up of <see cref="GetCalculatedSelfCost"/> - <see cref="GetCalculatedRefundCost"/>.
    /// </summary>
    /// <returns>The effective cost that's been calculated</returns>
    public virtual double GetCalculatedEffectiveCost()
    {
        return GetCalculatedSelfCost() - GetCalculatedRefundCost();
    }

    protected virtual void ReadBasePropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION_EDITOR or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_EDITOR);

        CostMultiplier = reader.ReadFloat();

        lastCalculatedRefundCost = double.NaN;
        lastCalculatedSelfCost = double.NaN;
    }

    protected abstract (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition);

    protected abstract double CalculateBaseCostInternal();

    /// <summary>
    ///   Detects where the current region that should be looked at for MP costs begins.
    ///   Used, for example, to ignore deletes / moves that affected a hex in its previous life before it was placed
    ///   again (and thus are irrelevant for many actions' MP calculations)
    /// </summary>
    /// <param name="history">History to search</param>
    /// <param name="insertPosition">Position before which the region must be</param>
    /// <param name="bias">
    ///   Bias value added to the return value. By default, 1 to return the index after the interesting found boundary.
    /// </param>
    /// <returns>0 if not found, otherwise the last relevant position</returns>
    protected int CalculateValidityRegionStart(IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition, int bias = 1)
    {
        int lastInterestingPoint = 0;

        // TODO: could this loop run in reverse for more efficient searching?
        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            if (ActionDenotesInterestingRegionBoundary(other))
            {
                lastInterestingPoint = i + bias;
            }
        }

        return lastInterestingPoint;
    }

    protected virtual bool ActionDenotesInterestingRegionBoundary(EditorCombinableActionData action)
    {
        throw new NotImplementedException();
    }
}

public abstract class EditorCombinableActionData<TContext> : EditorCombinableActionData
    where TContext : IArchivable
{
    public const ushort SERIALIZATION_VERSION_CONTEXT = 1;

    /// <summary>
    ///   The optional context this action was performed in. This is additional data in addition to the action target.
    ///   Not all editors use context info.
    /// </summary>
    public TContext? Context { get; set; }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(SERIALIZATION_VERSION_EDITOR);
        base.WriteToArchive(writer);

        writer.WriteObjectOrNull(Context);
    }

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
        return MatchesContext(Context, other);
    }

    protected static bool MatchesContext(TContext? originalContext, EditorCombinableActionData<TContext> other)
    {
        if (originalContext is null)
            return other.Context is null;

        if (other.Context is null)
            return false;

        return originalContext.Equals(other.Context);
    }

    protected override void ReadBasePropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION_CONTEXT or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_CONTEXT);

        // Base version is different (which we saved so we can read it here)
        base.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        Context = reader.ReadObjectOrNull<TContext>();
    }
}
