using System.Collections.Generic;

/// <summary>
///   Allows storing compounds up to the specified capacity
/// </summary>
public interface ICompoundStorage : IEnumerable<KeyValuePair<Compound, float>>
{
    /// <summary>
    ///   The max amount of a compound that can be stored
    /// </summary>
    public float GetCapacityForCompound(Compound compound);

    /// <summary>
    ///   Returns the stored amount of the compound in this
    /// </summary>
    public float GetCompoundAmount(Compound compound);

    /// <summary>
    ///   Takes some compound out of this bag.
    /// </summary>
    /// <returns>Returns the amount taken, which can be less than the requested amount.</returns>
    public float TakeCompound(Compound compound, float amount);

    /// <summary>
    ///   Adds some compound to this bag.
    /// </summary>
    /// <returns>Returns the amount that was added, which can be less than the given amount.</returns>
    public float AddCompound(Compound compound, float amount);

    /// <summary>
    ///   Clears the held compounds
    /// </summary>
    public void ClearCompounds();

    /// <summary>
    ///   Makes sure no compound amount is negative
    /// </summary>
    public void ClampNegativeCompoundAmounts();
}
