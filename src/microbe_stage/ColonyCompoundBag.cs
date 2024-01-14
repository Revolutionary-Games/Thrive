using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Components;
using DefaultEcs;
using Godot;

/// <summary>
///   Access to a microbe colony's compounds through a unified interface. Instances of this class should not be stored
///   and only be accessed with <see cref="Components.MicrobeColonyHelpers.GetCompounds"/>
/// </summary>
public class ColonyCompoundBag : ICompoundStorage
{
    private readonly object refreshListLock = new();

    private List<CompoundBag> colonyBags = new();
    private List<CompoundBag> bagBuilder = new();

    private bool nanIssueReported;

    public ColonyCompoundBag(Entity[] colonyMembers)
    {
        // This +4 is here basically for fun to give a reasonable initial size (as colonies start mostly with 2
        // members)
        bagBuilder.Capacity = colonyMembers.Length + 4;
        UpdateColonyMembers(colonyMembers);
    }

    public float GetCapacityForCompound(Compound compound)
    {
        return GetCompoundBags().Sum(p => p.GetCapacityForCompound(compound));
    }

    /// <summary>
    ///   Updates the colony members of this bag. Should only be called from the colony helper methods for adding and
    ///   removing members
    /// </summary>
    /// <param name="colonyMembers">The new colony member entities</param>
    public void UpdateColonyMembers(Entity[] colonyMembers)
    {
        lock (refreshListLock)
        {
            bagBuilder.Clear();

            // Initialize capacity to something that probably fits
            if (bagBuilder.Capacity < 1)
                bagBuilder.Capacity = colonyBags.Capacity + 2;

            foreach (var colonyMember in colonyMembers)
            {
                if (!colonyMember.Has<CompoundStorage>())
                {
                    GD.PrintErr("Colony compound bag member entity has no compound storage");
                    continue;
                }

                bagBuilder.Add(colonyMember.Get<CompoundStorage>().Compounds);
            }

            (colonyBags, bagBuilder) = (bagBuilder, colonyBags);
        }
    }

    /// <summary>
    ///   Evenly spreads out the compounds among all microbes
    /// </summary>
    public void DistributeCompoundSurplus()
    {
        var bags = GetCompoundBags();

        foreach (var currentPair in this)
        {
            var compound = currentPair.Key;

            if (!compound.CanBeDistributed || !IsUsefulInAnyCompoundBag(compound, bags))
                continue;

            var compoundAmount = currentPair.Value;
            var compoundCapacity = GetCapacityForCompound(compound);

            // This is just an error print, can be removed if no more NaN issues occur
            // See also CompoundBag.FixNaNCompounds which fixes NaN values after they occur
            if (compoundCapacity == 0)
            {
                if (!nanIssueReported)
                {
                    GD.PrintErr($"Compound {compound.Name} is set to useful but has a Capacity of zero, " +
                        "https://github.com/Revolutionary-Games/Thrive/issues/3201");
                    nanIssueReported = true;
                }
            }

            var ratio = compoundAmount / compoundCapacity;

            foreach (var bag in bags)
            {
                if (!bag.IsUseful(compound))
                    continue;

                var expectedAmount = ratio * bag.GetCapacityForCompound(compound);
                var surplus = bag.GetCompoundAmount(compound) - expectedAmount;
                if (surplus > 0)
                {
                    bag.TakeCompound(compound, surplus);
                }
                else
                {
                    bag.AddCompound(compound, -surplus);
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

    public void ClampNegativeCompoundAmounts()
    {
        foreach (var bag in GetCompoundBags())
            bag.ClampNegativeCompoundAmounts();
    }

    public bool IsUsefulInAnyCompoundBag(Compound compound)
    {
        return IsUsefulInAnyCompoundBag(compound, GetCompoundBags());
    }

    public bool AnyIsUsefulInAnyCompoundBag(IEnumerable<Compound> compounds)
    {
        // Just in case the compound bag method gets turned back into an iterator, this is fetched just once
        var bags = GetCompoundBags();

        foreach (var compound in compounds)
        {
            foreach (var bag in bags)
            {
                if (bag.IsUseful(compound))
                    return true;
            }
        }

        return false;
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

    private static bool IsUsefulInAnyCompoundBag(Compound compound, IEnumerable<CompoundBag> compoundBags)
    {
        return compoundBags.Any(p => p.IsUseful(compound));
    }

    private ICollection<CompoundBag> GetCompoundBags()
    {
        return colonyBags;
    }
}
