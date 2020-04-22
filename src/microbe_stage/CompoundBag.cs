using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Object that stores compound amounts and capacities
/// </summary>
public class CompoundBag : IEnumerable<KeyValuePair<string, float>>
{
    /// <summary>
    ///   The max amount of any compound that can be stored
    /// </summary>
    public float Capacity;

    private readonly HashSet<string> usefulCompounds = new HashSet<string>();

    public CompoundBag(float initialCapacity)
    {
        Capacity = initialCapacity;
    }

    /// <summary>
    ///   Returns all compounds. Don't modify the returned value!
    ///   Except if you want to ignore capacity...
    /// </summary>
    public Dictionary<string, float> Compounds { get; } = new Dictionary<string, float>();

    /// <summary>
    ///   Returns the stored amount of the compound in this
    /// </summary>
    public float GetCompoundAmount(string compound)
    {
        if (Compounds.ContainsKey(compound))
            return Compounds[compound];
        return 0.0f;
    }

    /// <summary>
    ///   Variant taking Compound
    /// </summary>
    public float GetCompoundAmount(Compound compound)
    {
        return GetCompoundAmount(compound.InternalName);
    }

    /// <summary>
    ///   Takes some compound out of this bag. Returns the amount
    ///   taken, which can be less than the requested amount.
    /// </summary>
    public float TakeCompound(string compound, float amount)
    {
        if (!Compounds.ContainsKey(compound) || amount <= 0.0f)
            return 0.0f;

        amount = Math.Min(Compounds[compound], amount);

        Compounds[compound] -= amount;
        return amount;
    }

    /// <summary>
    ///   Variant taking Compound
    /// </summary>
    public float TakeCompound(Compound compound, float amount)
    {
        return TakeCompound(compound.InternalName, amount);
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

        Compounds[compound] = newAmount;

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

    public IEnumerator<KeyValuePair<string, float>> GetEnumerator()
    {
        return Compounds.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///   Clears the held compounds
    /// </summary>
    public void ClearCompounds()
    {
        Compounds.Clear();
    }

    public void ClearUseful()
    {
        usefulCompounds.Clear();
    }

    public void SetUseful(string compound)
    {
        usefulCompounds.Add(compound);
    }

    /// <summary>
    ///   Returns true if at least one compound type has been marked
    ///   useful. This is used to detect that process system has ran
    ///   before venting.
    /// </summary>
    public bool HasAnyBeenSetUseful()
    {
        return usefulCompounds.Count > 0;
    }

    public bool IsUseful(Compound compound)
    {
        if (compound.IsAlwaysUseful)
        {
            return true;
        }

        return IsUseful(compound.InternalName);
    }

    public bool IsUseful(string compound)
    {
        return usefulCompounds.Contains(compound);
    }

    /// <summary>
    ///   Makes sure no compound amount is negative
    /// </summary>
    public void ClampNegativeCompoundAmounts()
    {
        var negative = Compounds.Where(c => c.Value < 0.0f);

        foreach (var entry in negative)
        {
            Compounds[entry.Key] = 0;
        }
    }
}
