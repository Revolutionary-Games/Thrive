using System;

/// <summary>
///   A combinable action data can be combined with other actions.
///   For example two separate movements of the same object can be combined into one larger movement action.
///   This is implemented as an aid for the player so that they do not have to think about optimizing their actions
///   to cost the least amount of MP. And reduce how many steps need to be undone when doing repetitive actions.
/// </summary>
public abstract class CombinableActionData
{
    /// <summary>
    ///   Does this action reset every action that happened before it?
    /// </summary>
    public virtual bool ResetsHistory => false;

    /// <summary>
    ///   How does this action interfere with the <paramref name="other"/> action?
    /// </summary>
    /// <returns>
    ///   Returns the interference mode with <paramref name="other"/>
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when called with itself</exception>
    public ActionInterferenceMode GetInterferenceModeWith(CombinableActionData other)
    {
        if (ReferenceEquals(this, other))
            throw new ArgumentException("Do not call with itself", nameof(other));

        return GetInterferenceModeWithGuaranteed(other);
    }

    /// <summary>
    ///   Combines two actions into one if possible. Call <see cref="GetInterferenceModeWith"/> first and check if
    ///   it returns <see cref="ActionInterferenceMode.Combinable"/>
    /// </summary>
    /// <param name="other">The action this should be combined with</param>
    /// <returns>Returns the combined action</returns>
    /// <exception cref="NotSupportedException">Thrown when combination is not possible</exception>
    public virtual CombinableActionData Combine(CombinableActionData other)
    {
        if (GetInterferenceModeWith(other) != ActionInterferenceMode.Combinable)
            throw new NotSupportedException();

        return CombineGuaranteed(other);
    }

    /// <summary>
    ///   Should this action be merged with the previous one if possible?
    /// </summary>
    /// <returns>True if this action wants to be merged with <see cref="other"/></returns>
    public virtual bool WantsMergeWith(CombinableActionData other)
    {
        return false;
    }

    /// <summary>
    ///   Merge the other data into this if possible
    /// </summary>
    /// <param name="other">The action to merge into this</param>
    /// <param name="otherIsNewer">
    ///   If the other data is newer than the current one, use only when the two cancels out, to make sure it goes right
    /// </param>
    /// <returns>True if a merge has been conducted</returns>
    public virtual bool TryMerge(CombinableActionData other, bool? otherIsNewer = null)
    {
        var mode = GetInterferenceModeWith(other);
        if (mode != ActionInterferenceMode.Combinable && mode != ActionInterferenceMode.CancelsOut)
            return false;

        MergeGuaranteed(other, otherIsNewer);
        return true;
    }

    protected abstract ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other);

    /// <summary>
    ///   Combines two actions into one
    /// </summary>
    /// <param name="other">The action this should be combined with. Guaranteed to be combinable</param>
    /// <returns>Returns the combined action</returns>
    protected abstract CombinableActionData CombineGuaranteed(CombinableActionData other);

    /// <summary>
    ///   Merges the other data into this action
    /// </summary>
    /// <param name="other">
    ///   The action to merge into this. Guaranteed to be mergeable by the called.
    ///   <see cref="GetInterferenceModeWith"/> has been called to make sure this can be combined.
    /// </param>
    /// <param name="otherIsNewer">If the other data is newer than the current one</param>
    protected virtual void MergeGuaranteed(CombinableActionData other, bool? otherIsNewer = null)
    {
        throw new NotSupportedException();
    }
}
