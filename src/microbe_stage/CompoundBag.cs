using System;
using System.Collections.Generic;

/// <summary>
///   Object that stores compound amounts and capacities
/// </summary>
public class CompoundBag
{
    /// <summary>
    ///   The max amount of any compound that can be stored
    /// </summary>
    public float Capacity;

    private Dictionary<string, float> compounds = new Dictionary<string, float>();

    public CompoundBag(float initialCapacity)
    {
        Capacity = initialCapacity;
    }

    /// <summary>
    ///   Returns all compounds. Don't modify the returned value!
    /// </summary>
    public Dictionary<string, float> Compounds
    {
        get
        {
            return compounds;
        }
    }

    /// <summary>
    ///   Returns the stored amount of the compound in this
    /// </summary>
    public float GetCompoundAmount(string compound)
    {
        if (compounds.ContainsKey(compound))
            return compounds[compound];
        return 0.0f;
    }

    /// <summary>
    ///   Takes some compound out of this bag. Returns the amount
    ///   taken, which can be less than the requested amount.
    /// </summary>
    public float TakeCompound(string compound, float amount)
    {
        if (!compounds.ContainsKey(compound) || amount <= 0.0f)
            return 0.0f;

        amount = Math.Min(compounds[compound], amount);

        compounds[compound] -= amount;
        return amount;
    }

    /// <summary>
    ///   Adds some compound amount to this. Returns the amount that
    ///   didn't fit due to reached capacity.
    /// </summary>
    public float AddCompound(string compound, float amount)
    {
        if (amount <= 0.0f)
            return amount;

        float existingAmount = GetCompoundAmount(compound);

        float newAmount = Math.Min(existingAmount + amount, Capacity);

        compounds[compound] = newAmount;

        float didntFit = amount - newAmount;

        if (didntFit > 0.0f)
        {
            return didntFit;
        }
        else
        {
            return 0.0f;
        }
    }
}
