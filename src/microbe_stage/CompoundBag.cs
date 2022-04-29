﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Object that stores compound amounts and capacities
/// </summary>
[UseThriveSerializer]
public class CompoundBag : ICompoundStorage
{
    [JsonProperty]
    private readonly HashSet<Compound> usefulCompounds = new();

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
    public Dictionary<Compound, float> Compounds { get; private set; } = new();

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
        Compounds.TryGetValue(compound, out var amount);

        return amount;
    }

    public float TakeCompound(Compound compound, float amount)
    {
        if (!Compounds.TryGetValue(compound, out var existingAmount) || amount <= 0.0f)
            return 0.0f;

        amount = Math.Min(existingAmount, amount);

        Compounds[compound] = existingAmount - amount;
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
        var negative = Compounds.Where(c => c.Value < 0.0f).ToList();

        foreach (var entry in negative)
        {
            Compounds[entry.Key] = 0;
        }
    }

    /// <summary>
    ///   Sets NaN compounds back to 0. Mitigation for https://github.com/Revolutionary-Games/Thrive/issues/3201
    ///   TODO: remove once that issue is solved
    /// </summary>
    public void FixNaNCompounds()
    {
        var nan = Compounds.Where(c => float.IsNaN(c.Value)).ToList();

        foreach (var entry in nan)
        {
            GD.PrintErr("Detected compound amount of ", entry.Key, " to be NaN. Setting amount to 0.");
            Compounds[entry.Key] = 0;
        }
    }
}
