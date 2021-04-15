using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

[UseThriveSerializer]
public class ColonyCompoundBag : ICompoundStorage
{
    public ColonyCompoundBag()
    {
    }

    public ColonyCompoundBag(MicrobeColony colony)
    {
        Colony = colony;
    }

    [JsonProperty]
    public MicrobeColony Colony { get; set; }

    public float Capacity => GetCompoundBags().Sum(p => p.Capacity);

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

            var average = compound.Value / bags.Count;

            foreach (var bag in bags)
            {
                var surplus = bag.GetCompoundAmount(compound.Key) - average;
                if (surplus > 0)
                    bag.TakeCompound(compound.Key, surplus);
                else
                    bag.AddCompound(compound.Key, -surplus);
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
        // bags that compound can be taken from
        var remainingBags = GetCompoundBags().ToList();

        while (amount > MathUtils.EPSILON && remainingBags.Any())
        {
            var bagToDrainFrom = remainingBags[0];
            var couldNotBeDrained = bagToDrainFrom.TakeCompound(compound, amount);
            var amountDrained = amount - couldNotBeDrained;

            // if bag is dry
            if (couldNotBeDrained > MathUtils.EPSILON)
                remainingBags.RemoveAt(0);

            amount -= amountDrained;
        }

        return amount;
    }

    public float AddCompound(Compound compound, float amount)
    {
        // bags that compound can added to
        var remainingBags = GetCompoundBags().ToList();
        var added = 0f;

        while (amount > MathUtils.EPSILON && remainingBags.Any())
        {
            var bagToAddTo = remainingBags[0];
            var amountAdded = bagToAddTo.AddCompound(compound, amount);
            var amountNotAdded = amount - amountAdded;

            // if bag is full
            if (amountNotAdded > MathUtils.EPSILON)
                remainingBags.RemoveAt(0);

            amount -= amountAdded;
            added += amountAdded;
        }

        return added;
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
        return Colony.GetColonyMembers().Select(p => p.Compounds);
    }
}
