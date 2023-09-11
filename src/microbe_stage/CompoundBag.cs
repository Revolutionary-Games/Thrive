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
    private readonly HashSet<Compound> usefulCompounds = new();

    [JsonProperty]
    private Dictionary<Compound, float>? compoundCapacities;

    public CompoundBag(float nominalCapacity)
    {
        NominalCapacity = nominalCapacity;
    }

    /// <summary>
    ///   Specifies the default capacity for all compounds that do
    ///   not have a specific capacity set in <see cref="compoundCapacities"/>
    /// </summary>
    [JsonProperty]
    public float NominalCapacity { get; set; }

    /// <summary>
    ///   Returns all compounds. Don't modify the returned value!
    ///   Except if you want to ignore capacity...
    /// </summary>
    [JsonProperty]
    public Dictionary<Compound, float> Compounds { get; private set; } = new();

    /// <summary>
    ///   Gets the capacity for a given compound
    /// </summary>
    /// <returns>
    ///   Returns the capacity this bag has for storing the compound if it is useful, otherwise 0
    /// </returns>
    public float GetCapacityForCompound(Compound compound)
    {
        if (!IsUseful(compound))
            return 0;

        if (compoundCapacities != null && compoundCapacities.TryGetValue(compound, out var capacity))
            return capacity;

        return NominalCapacity;
    }

    public void SetCapacityForCompound(Compound compound, float capacity)
    {
        compoundCapacities ??= new Dictionary<Compound, float>();
        compoundCapacities[compound] = capacity;
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

        float newAmount = Math.Min(existingAmount + amount, GetCapacityForCompound(compound));

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

    public void ClearSpecificCapacities()
    {
        compoundCapacities?.Clear();
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

    public bool AreAnySpecificallySetUseful(IEnumerable<Compound> compounds)
    {
        return compounds.Any(usefulCompounds.Contains);
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
            // GD.PrintErr("Detected compound amount of ", entry.Key, " to be NaN. Setting amount to 0.");
            Compounds[entry.Key] = 0;
        }
    }
}
