using System;

public abstract class CombinableActionData
{
    /// <summary>
    ///   Does this action reset every action that happened before it?
    /// </summary>
    public virtual bool ResetsHistory => false;

    /// <summary>
    ///   Does this action cancel out with the <paramref name="other"/> action?
    /// </summary>
    /// <returns>
    ///   Returns the interference mode with <paramref name="other"/>
    /// </returns>
    /// <para>Do not call with itself</para>
    public abstract MicrobeActionInterferenceMode GetInterferenceModeWith(CombinableActionData other);

    /// <summary>
    ///   Combines two actions to one if possible.
    ///   Call <see cref="GetInterferenceModeWith"/> first and check if it returns
    ///   <see cref="MicrobeActionInterferenceMode.Combinable"/>
    /// </summary>
    /// <param name="other">The action this should be combined with</param>
    /// <returns>Returns the combined action</returns>
    /// <exception cref="NotSupportedException">Thrown when combination is not possible</exception>
    public CombinableActionData Combine(CombinableActionData other)
    {
        if (GetInterferenceModeWith(other) != MicrobeActionInterferenceMode.Combinable)
            throw new NotSupportedException();

        return CombineGuaranteed(other);
    }

    /// <summary>
    ///   Combines two actions to one
    /// </summary>
    /// <param name="other">The action this should be combined with. Guaranteed to be combinable</param>
    /// <returns>Returns the combined action</returns>
    protected abstract CombinableActionData CombineGuaranteed(CombinableActionData other);
}
