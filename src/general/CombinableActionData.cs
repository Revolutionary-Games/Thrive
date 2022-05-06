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
    public CombinableActionData Combine(CombinableActionData other)
    {
        if (GetInterferenceModeWith(other) != ActionInterferenceMode.Combinable)
            throw new NotSupportedException();

        return CombineGuaranteed(other);
    }

    protected abstract ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other);

    /// <summary>
    ///   Combines two actions into one
    /// </summary>
    /// <param name="other">The action this should be combined with. Guaranteed to be combinable</param>
    /// <returns>Returns the combined action</returns>
    protected abstract CombinableActionData CombineGuaranteed(CombinableActionData other);
}
