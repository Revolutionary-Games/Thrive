using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Object that stores compound amounts and capacities
/// </summary>
[UseThriveSerializer]
public class CompoundBag : IEnumerable<KeyValuePair<Compound, float>>
{
    /// <summary>
    ///   The max amount of any compound that can be stored
    /// </summary>
    public float Capacity;

    [JsonProperty]
    private readonly HashSet<Compound> usefulCompounds = new HashSet<Compound>();

    /// <summary>
    ///   Creates a new bag
    /// </summary>
    /// <param name="capacity">Specifies the initial capacity of the compound bag</param>
    public CompoundBag(float capacity)
    {
        Capacity = capacity;
    }

    /// <summary>
    ///   Returns all compounds. Don't modify the returned value!
    ///   Except if you want to ignore capacity...
    /// </summary>
    [JsonProperty]
    public Dictionary<Compound, float> Compounds { get; private set; } = new Dictionary<Compound, float>();

    /// <summary>
    ///   Returns the stored amount of the compound in this
    /// </summary>
    public float GetCompoundAmount(Compound compound)
    {
        if (Compounds.ContainsKey(compound))
            return Compounds[compound];

        return 0.0f;
    }

    /// <summary>
    ///   Takes some compound out of this bag. Returns the amount
    ///   taken, which can be less than the requested amount.
    /// </summary>
    public float TakeCompound(Compound compound, float amount)
    {
        if (!Compounds.ContainsKey(compound) || amount <= 0.0f)
            return 0.0f;

        amount = Math.Min(Compounds[compound], amount);

        Compounds[compound] -= amount;
        return amount;
    }

    /// <summary>
    ///   Adds some compound amount to this. Returns the amount that
    ///   didn't fit due to reached capacity.
    /// </summary>
    public float AddCompound(Compound compound, float amount)
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

        return 0.0f;
    }

    public IEnumerator<KeyValuePair<Compound, float>> GetEnumerator()
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

    public void SetUseful(Compound compound)
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

        return IsSpecificallySetUseful(compound);
    }

    public bool IsSpecificallySetUseful(Compound compound)
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
