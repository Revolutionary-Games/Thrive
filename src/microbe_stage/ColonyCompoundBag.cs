﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

[UseThriveSerializer]
public class ColonyCompoundBag : ICompoundStorage
{
    public ColonyCompoundBag(MicrobeColony colony)
    {
        Colony = colony;
    }

    [JsonProperty]
    private MicrobeColony Colony { get; set; }

    public float GetCapacityForCompound(Compound compound)
    {
        return GetCompoundBags().Sum(p => p.GetCapacityForCompound(compound));
    }

    /// <summary>
    ///   Evenly spreads out the compounds among all microbes
    /// </summary>
    public void DistributeCompoundSurplus()
    {
        var bags = GetCompoundBags().ToList();
        using var compounds = GetEnumerator();

        while (compounds.MoveNext())
        {
            var compound = compounds.Current;
            if (!compound.Key.CanBeDistributed)
                continue;

            var ratio = compound.Value / GetCapacityForCompound(compound.Key);

            foreach (var bag in bags)
            {
                var amount = ratio * bag.GetCapacityForCompound(compound.Key);
                var surplus = bag.GetCompoundAmount(compound.Key) - amount;
                if (surplus > 0)
                {
                    bag.TakeCompound(compound.Key, surplus);
                }
                else
                {
                    bag.AddCompound(compound.Key, -surplus);
                }
            }
        }
    }

    public IEnumerator<KeyValuePair<Compound, float>> GetEnumerator()
    {
        return GetCompoundBags()
            .SelectMany(p => p.Compounds)
            .GroupBy(p => p.Key)
            .Select(p => new KeyValuePair<Compound, float>(p.Key, p.Sum(x => x.Value)))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public float GetCompoundAmount(Compound compound)
    {
        return GetCompoundBags().Sum(p => p.GetCompoundAmount(compound));
    }

    public float TakeCompound(Compound compound, float amount)
    {
        foreach (var bagToDrainFrom in GetCompoundBags())
        {
            var couldNotBeDrained = bagToDrainFrom.TakeCompound(compound, amount);
            var amountDrained = amount - couldNotBeDrained;

            amount -= amountDrained;

            if (amount <= MathUtils.EPSILON)
                break;
        }

        return amount;
    }

    public float AddCompound(Compound compound, float amount)
    {
        var totalAmountAdded = 0.0f;

        foreach (var bagToAddTo in GetCompoundBags())
        {
            var amountAdded = bagToAddTo.AddCompound(compound, amount);

            totalAmountAdded += amountAdded;
            amount -= amountAdded;

            if (amount <= MathUtils.EPSILON)
                break;
        }

        return totalAmountAdded;
    }

    public void ClearCompounds()
    {
        foreach (var bag in GetCompoundBags())
            bag.ClearCompounds();
    }

    public void ClampNegativeCompoundAmounts()
    {
        foreach (var bag in GetCompoundBags())
            bag.ClampNegativeCompoundAmounts();
    }

    private IEnumerable<CompoundBag> GetCompoundBags()
    {
        return Colony.ColonyMembers.Select(p => p.Compounds);
    }
}
