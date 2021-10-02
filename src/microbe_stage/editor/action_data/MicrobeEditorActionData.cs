using System;

/// <summary>
///   This is its own interface to make JSON loading dynamic type more strict
/// </summary>
public abstract class MicrobeEditorActionData
{
    /// <summary>
    ///   If true this action is only a sub-action to the next one and should be skipped when undoing
    /// </summary>
    public virtual bool IsSubAction => false;

    /// <summary>
    ///   Does this action cancel out with the <paramref name="other"/> action?
    /// </summary>
    /// <returns>
    ///   Returns the interference mode with <paramref name="other"/>
    /// </returns>
    /// <para>Do not call with itself</para>
    public abstract MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other);

    /// <summary>
    ///   Combines two actions to one if possible.
    ///   Call <see cref="MicrobeEditorActionData.GetInterferenceModeWith"/> first and check if it returns
    ///   <see cref="MicrobeActionInterferenceMode.Combinable"/>
    /// </summary>
    /// <param name="other">The action this should be combined with</param>
    /// <returns>Returns the combined action</returns>
    /// <exception cref="NotSupportedException">Thrown when combination is not possible</exception>
    public MicrobeEditorActionData Combine(MicrobeEditorActionData other)
    {
        if (GetInterferenceModeWith(other) != MicrobeActionInterferenceMode.Combinable)
            throw new NotSupportedException();

        return CombineGuaranteed(other);
    }

    public abstract int CalculateCost();

    /// <summary>
    ///   Combines two actions to one
    /// </summary>
    /// <param name="other">The action this should be combined with. Guaranteed to be combinable</param>
    /// <returns>Returns the combined action</returns>
    protected abstract MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other);
}
