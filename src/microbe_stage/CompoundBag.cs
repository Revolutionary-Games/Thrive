﻿using System;
using System.Collections;
using System.Collections.Generic;
using Godot;
using JetBrains.Annotations;
using Newtonsoft.Json;

/// <summary>
///   Object that stores compound amounts and capacities
/// </summary>
[UseThriveSerializer]
public class CompoundBag : ICompoundStorage
{
    private readonly HashSet<Compound> usefulCompounds = new();

    /// <summary>
    ///   Temporary data holder used to avoid extraneous allocations each game update
    /// </summary>
    private readonly List<Compound> tempCompounds = new();

    [JsonProperty]
    private Dictionary<Compound, float>? compoundCapacities;

    private float nominalCapacity;

    public CompoundBag(float nominalCapacity)
    {
        NominalCapacity = nominalCapacity;
    }

    /// <summary>
    ///   Specifies the default capacity for all compounds that do
    ///   not have a specific capacity set in <see cref="compoundCapacities"/>
    /// </summary>
    [JsonProperty]
    public float NominalCapacity
    {
        get => nominalCapacity;
        set
        {
            nominalCapacity = value;

            if (nominalCapacity < 0)
                throw new ArgumentException("Capacity can't be negative", nameof(NominalCapacity));
        }
    }

    /// <summary>
    ///   Returns all compounds. Don't modify the returned value!
    ///   Except if you want to ignore capacity...
    /// </summary>
    [JsonProperty]
    public Dictionary<Compound, float> Compounds { get; private set; } = new();

    /// <summary>
    ///   Gets the capacity for a given compound
    /// </summary>
    /// <param name="compound">Compound type to get capacity for</param>
    /// <param name="ignoreUsefulness">
    ///   If true then the capacity check ignores if the compound is not considered useful and returns the real
    ///   physical capacity even if the compound is not considered useful to add
    /// </param>
    /// <returns>
    ///   Returns the capacity this bag has for storing the compound if it is useful, otherwise 0
    /// </returns>
    /// <remarks>
    ///   <para>
    ///     TODO: maybe put this in the base interface with the default value of false for ignore usefulness?
    ///   </para>
    /// </remarks>
    public float GetCapacityForCompound(Compound compound, bool ignoreUsefulness)
    {
        if (!ignoreUsefulness && !IsUseful(compound))
            return 0;

        if (compoundCapacities != null && compoundCapacities.TryGetValue(compound, out var capacity))
            return capacity;

        return NominalCapacity;
    }

    /// <inheritdoc cref="GetCapacityForCompound(Compound,bool)"/>
    public float GetCapacityForCompound(Compound compound)
    {
        return GetCapacityForCompound(compound, false);
    }

    /// <summary>
    ///   Adds specialized capacity for a compound. <see cref="NominalCapacity"/> must be set before calling this.
    ///   To reset this value call <see cref="ClearSpecificCapacities"/> to restart filling this info.
    /// </summary>
    /// <param name="compound">The compound type</param>
    /// <param name="capacityToAdd">Capacity to add for this compound</param>
    /// <remarks>
    ///   <para>
    ///     This now adds capacity (and starts capacities from the nominal capacity) instead of setting the value
    ///     directly. This is to allow the <see cref="MicrobeInternalCalculations"/> method that updates this to avoid
    ///     memory allocations.
    ///   </para>
    /// </remarks>
    public void AddSpecificCapacityForCompound(Compound compound, float capacityToAdd)
    {
        if (capacityToAdd < 0)
            throw new ArgumentException("Capacity to set can't be negative", nameof(capacityToAdd));

        if (compound == Compound.Invalid)
        {
            GD.PrintErr("Cannot add compound capacity of invalid type to bag");
            return;
        }

        compoundCapacities ??= new Dictionary<Compound, float>();

        if (!compoundCapacities.TryGetValue(compound, out var existing))
        {
            // Add nominal capacity as the base amount here when the first specific capacity value is added
            compoundCapacities[compound] = capacityToAdd + NominalCapacity;
        }
        else
        {
            compoundCapacities[compound] = existing + capacityToAdd;
        }
    }

    public float GetCompoundAmount(Compound compound)
    {
        Compounds.TryGetValue(compound, out var amount);

        return amount;
    }

    /// <summary>
    ///   The space available for a compound of given type. If not useful no free space is ever reported.
    /// </summary>
    /// <param name="compound">The compound type to check</param>
    /// <returns>The free space available</returns>
    public float GetFreeSpaceForCompound(Compound compound)
    {
        // Due to venting not triggering immediately at capacity, we use max here to avoid negative free space
        return Math.Max(GetCapacityForCompound(compound) - GetCompoundAmount(compound), 0);
    }

    public float TakeCompound(Compound compound, float amount)
    {
        if (amount <= 0.0f || !Compounds.TryGetValue(compound, out var existingAmount))
            return 0.0f;

        amount = Math.Min(existingAmount, amount);

        Compounds[compound] = existingAmount - amount;
        return amount;
    }

    public float AddCompound(Compound compound, float amount)
    {
        if (amount <= 0.0f)
            return amount;

        if (compound == Compound.Invalid)
        {
            GD.PrintErr("Cannot add compound amount of invalid type to bag");
            return 0;
        }

        float existingAmount = GetCompoundAmount(compound);

        float newAmount = Math.Min(existingAmount + amount, GetCapacityForCompound(compound));

        Compounds[compound] = newAmount;

        return newAmount - existingAmount;
    }

    [MustDisposeResource]
    public IEnumerator<KeyValuePair<Compound, float>> GetEnumerator()
    {
        return Compounds.GetEnumerator();
    }

    public void ClearCompounds()
    {
        Compounds.Clear();
    }

    public void ClearUseful()
    {
        usefulCompounds.Clear();
    }

    public void ClearSpecificCapacities()
    {
        compoundCapacities?.Clear();
    }

    public void SetUseful(Compound compound)
    {
        if (compound == Compound.Invalid)
        {
            GD.PrintErr("Cannot set invalid compound type as useful in bag");
            return;
        }

        usefulCompounds.Add(compound);
    }

    /// <summary>
    ///   Returns true if at least one compound type has been marked useful.
    ///   This is used to detect that process system has ran before venting.
    /// </summary>
    public bool HasAnyBeenSetUseful()
    {
        return usefulCompounds.Count > 0;
    }

    public bool IsUseful(Compound compound)
    {
        if (IsSpecificallySetUseful(compound))
        {
            return true;
        }

        // Now that compound is just an index, this needs to look up the actual data so this isn't as efficient as
        // before
        return SimulationParameters.GetCompound(compound).IsAlwaysUseful;
    }

    public bool IsUseful(CompoundDefinition compound)
    {
        // This variant checks always useful first as the data is immediately available
        if (compound.IsAlwaysUseful)
            return true;

        return IsSpecificallySetUseful(compound.ID);
    }

    public bool IsSpecificallySetUseful(Compound compound)
    {
        return usefulCompounds.Contains(compound);
    }

    /// <summary>
    ///   Checks if any of the given compounds are marked specifically useful. This variant exists to be hopefully more
    ///   efficient in terms of enumerator allocation.
    /// </summary>
    /// <param name="compounds">Compounds to check</param>
    /// <returns>True if any are set specifically useful</returns>
    public bool AreAnySpecificallySetUseful(IList<Compound> compounds)
    {
        foreach (var compound in compounds)
        {
            if (usefulCompounds.Contains(compound))
                return true;
        }

        return false;
    }

    public bool AreAnySpecificallySetUseful(IEnumerable<Compound> compounds)
    {
        foreach (var compound in compounds)
        {
            if (usefulCompounds.Contains(compound))
                return true;
        }

        return false;
    }

    /// <summary>
    ///   Copies the useful data from another bag
    /// </summary>
    public void CopyUsefulFrom(CompoundBag other)
    {
        ClearUseful();

        foreach (var useful in other.usefulCompounds)
        {
            usefulCompounds.Add(useful);
        }
    }

    /// <summary>
    ///   Returns true only if this compound bag contains any compounds whatsoever
    /// </summary>
    /// <returns>True if not empty</returns>
    public bool HasAnyCompounds()
    {
        foreach (var compoundsValue in Compounds.Values)
        {
            if (compoundsValue > 0)
                return true;
        }

        return false;
    }

    public void AddInitialCompounds(IReadOnlyDictionary<Compound, float> compounds)
    {
        foreach (var entry in compounds)
        {
            if (!Compounds.TryGetValue(entry.Key, out var existingAmount))
            {
                Compounds[entry.Key] = entry.Value;
                continue;
            }

            float toAdd = entry.Value - existingAmount;

            if (toAdd > 0)
                Compounds[entry.Key] = existingAmount + toAdd;
        }
    }

    /// <summary>
    ///   Adds an extra initial compound that respects storage space (used for example for night compound buff)
    /// </summary>
    /// <param name="compound">Compound type</param>
    /// <param name="amount">Amount</param>
    public void AddExtraInitialCompoundIfUnderStorageLimit(Compound compound, float amount)
    {
        if (amount <= 0)
            return;

        Compounds.TryGetValue(compound, out var existingAmount);

        Compounds[compound] = Math.Min(existingAmount + amount, GetCapacityForCompound(compound, true));
    }

    /// <summary>
    ///   Tops up on a compound to be at least at the minimum value (ignores usefulness but takes capacity into
    ///   account)
    /// </summary>
    /// <param name="compound">Compound type to add</param>
    /// <param name="wantedMinimumAmount">Minimum level of the compound that should be reached</param>
    public void TopUpCompound(Compound compound, float wantedMinimumAmount)
    {
        if (wantedMinimumAmount <= 0)
            return;

        Compounds.TryGetValue(compound, out var existingAmount);

        Compounds[compound] = Math.Min(Math.Max(existingAmount, wantedMinimumAmount),
            GetCapacityForCompound(compound, true));
    }

    public void ClampNegativeCompoundAmounts()
    {
        foreach (var entry in Compounds)
        {
            if (entry.Value < 0.0f)
                tempCompounds.Add(entry.Key);
        }

        if (tempCompounds.Count < 1)
            return;

        foreach (var entry in tempCompounds)
        {
            Compounds[entry] = 0;
        }

        tempCompounds.Clear();
    }

    /// <summary>
    ///   Sets NaN compounds back to 0. Mitigation for https://github.com/Revolutionary-Games/Thrive/issues/3201
    ///   TODO: remove once that issue is solved
    /// </summary>
    public void FixNaNCompounds()
    {
        foreach (var entry in Compounds)
        {
            if (float.IsNaN(entry.Value))
                tempCompounds.Add(entry.Key);
        }

        if (tempCompounds.Count < 1)
            return;

        foreach (var entry in tempCompounds)
        {
            // TODO: should maybe re-enable this print to track this issue happening?
            // GD.PrintErr("Detected compound amount of ", entry.Key, " to be NaN. Setting amount to 0.");
            Compounds[entry] = 0;
        }

        tempCompounds.Clear();
    }

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
