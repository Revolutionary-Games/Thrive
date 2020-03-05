using System;

/// <summary>
///   Object that stores compound amounts and capacities
/// </summary>
public class CompoundBag
{
    /// <summary>
    ///   The max amount of any compound that can be stored
    /// </summary>
    public float Capacity;

    public CompoundBag(float initialCapacity)
    {
        Capacity = initialCapacity;
    }

    public float TakeCompound(string compound, float amount)
    {
        return amount;
    }
}
