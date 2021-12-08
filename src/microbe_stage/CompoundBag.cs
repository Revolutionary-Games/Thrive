using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Object that stores compound amounts and capacities
/// </summary>
[UseThriveSerializer]
public class CompoundBag : ICompoundStorage
{
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
    ///   How much of each compound this bag can store.
    ///   Currently a CompoundBag can hold the same amount of each compound.
    /// </summary>
    [JsonProperty]
    public float Capacity { get; set; }

    /// <summary>
    ///   Returns all compounds. Don't modify the returned value!
    ///   Except if you want to ignore capacity...
    /// </summary>
    [JsonProperty]
    public Dictionary<Compound, float> Compounds { get; private set; } = new Dictionary<Compound, float>();

    /// <summary>
    ///   Gets the capacity for a given compound
    /// </summary>
    /// <returns>Returns <see cref="Capacity"/> if the compound is useful, otherwise 0</returns>
    public float GetCapacityForCompound(Compound compound)
    {
        if (IsUseful(compound))
            return Capacity;

        return 0;
    }

    public float GetCompoundAmount(Compound compound)
    {
        if (Compounds.ContainsKey(compound))
            return Compounds[compound];

        return 0.0f;
    }

    public float TakeCompound(Compound compound, float amount)
    {
        if (!Compounds.ContainsKey(compound) || amount <= 0.0f)
            return 0.0f;

        amount = Math.Min(Compounds[compound], amount);

        Compounds[compound] -= amount;
        return amount;
    }

    public float AddCompound(Compound compound, float amount)
    {
        if (amount <= 0.0f)
            return amount;

        float existingAmount = GetCompoundAmount(compound);

        float newAmount = Math.Min(existingAmount + amount, Capacity);

        Compounds[compound] = newAmount;

        return newAmount - existingAmount;
    }

    public IEnumerator<KeyValuePair<Compound, float>> GetEnumerator()
    {
        return Compounds.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

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

    public void ClampNegativeCompoundAmounts()
    {
        var negative = Compounds.Where(c => c.Value < 0.0f);

        foreach (var entry in negative)
        {
            Compounds[entry.Key] = 0;
        }
    }
}
