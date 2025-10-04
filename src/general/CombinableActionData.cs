using System;

/// <summary>
///   Combinable action data that can be combined with other actions.
///   For example, two separate movements of the same object can be combined into one larger movement action.
///   This is implemented as aid for the player,
///   so that how many steps need to be undone when doing repetitive actions is reduced.
/// </summary>
public abstract class CombinableActionData
{
    /// <summary>
    ///   Does this action reset every action that happened before it? Used for the "new cell" button in freebuild.
    /// </summary>
    public virtual bool ResetsHistory => false;

    /// <summary>
    ///   Should this action be merged with the previous one if possible?
    ///   Does this action want to be merged with the <paramref name="other"/> action?
    /// </summary>
    /// <returns>
    ///   Returns true if the two actions should be merged.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when called with itself</exception>
    public virtual bool WantsToMergeWith(CombinableActionData other)
    {
        if (ReferenceEquals(this, other))
            throw new ArgumentException("Do not call with itself", nameof(other));

        return CanMergeWithInternal(other);
    }

    /// <summary>
    ///   Merge the other data into this if possible
    /// </summary>
    /// <param name="other">The action to merge into this</param>
    /// <returns>True if a merge has been conducted</returns>
    /// <remarks>
    ///   <para>
    ///     The other action must *always* be newer than this. So the looping order must always go from an older action
    ///     to a newer action when merging actions.
    ///   </para>
    /// </remarks>
    public virtual bool TryMerge(CombinableActionData other)
    {
        if (!WantsToMergeWith(other))
            return false;

        MergeGuaranteed(other);
        return true;
    }

    protected abstract bool CanMergeWithInternal(CombinableActionData other);

    /// <summary>
    ///   Merges the other data into this action
    /// </summary>
    /// <param name="other">
    ///   The action to merge into this.
    ///   Guaranteed to be mergeable when this is called as <see cref="WantsToMergeWith"/> has been called to make sure
    /// </param>
    protected virtual void MergeGuaranteed(CombinableActionData other)
    {
        throw new NotSupportedException();
    }
}
