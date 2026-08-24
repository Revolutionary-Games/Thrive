using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using Godot;

/// <summary>
///   Access to a microbe colony's compounds through a unified interface. Instances of this class should not be stored
///   and only be accessed with <see cref="Components.MicrobeColonyHelpers.GetCompounds"/>
/// </summary>
public class ColonyCompoundBag : ICompoundStorage
{
    private readonly object refreshListLock = new();

    /// <summary>
    ///   Used just for getting total compounds in the colony
    /// </summary>
    private readonly Dictionary<Compound, float> summedCompoundsBuffer = new();

    // These variables are for distributing compounds and are not always kept up to date
    private readonly HashSet<Compound> usefulCompoundsBuffer = new();
    private readonly Dictionary<Compound, float> availableCompounds = new();
    private readonly Dictionary<Compound, float> compoundCapacities = new();

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
        float sum = 0;
        foreach (var bag in GetCompoundBags())
        {
            sum += bag.GetCapacityForCompound(compound);
        }

        return sum;
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
                if (!colonyMember.IsAliveAndHas<CompoundStorage>())
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

        GD.Print("Compound bag count: " + bags.Count);

        usefulCompoundsBuffer.Clear();
        availableCompounds.Clear();
        compoundCapacities.Clear();

        // Determine compounds that need sharing first
        bool first = true;
        foreach (var compoundBag in bags)
        {
            compoundBag.CopyUsefulToHash(usefulCompoundsBuffer, first);
            first = false;
        }

        // Don't share ATP
        usefulCompoundsBuffer.Remove(Compound.ATP);

        GD.Print("Useful compounds:");
        foreach (var compound in usefulCompoundsBuffer)
        {
            GD.Print($" {compound}");
        }

        bool needsNonUsefulAdjustments = false;

        // This is used as a sum of non-useful compounds
        summedCompoundsBuffer.Clear();

        // Then offer up the compounds
        foreach (var bag in bags)
        {
            GD.Print("Next bag");

            foreach (var bagCompound in bag.Compounds)
            {
                // Don't offer compounds to move that nobody finds useful in this colony
                if (!usefulCompoundsBuffer.Contains(bagCompound.Key))
                    continue;

                if (bagCompound.Value <= 0)
                    continue;

                GD.Print($" Amount({bagCompound.Key}): {bagCompound.Value}");

                // Has available compound, try to share with other bags
                if (availableCompounds.TryGetValue(bagCompound.Key, out var availableAmount))
                {
                    availableCompounds[bagCompound.Key] = availableAmount + bagCompound.Value;
                }
                else
                {
                    availableCompounds.Add(bagCompound.Key, bagCompound.Value);
                }

                // If not useful, this will not receive any of this compound.
                if (!bag.IsUseful(bagCompound.Key))
                {
                    // Store for later as we don't know yet how much of this compound other bags can take
                    needsNonUsefulAdjustments = true;

                    if (summedCompoundsBuffer.TryGetValue(bagCompound.Key, out var summedAmount))
                    {
                        summedCompoundsBuffer[bagCompound.Key] = summedAmount + bagCompound.Value;
                    }
                    else
                    {
                        summedCompoundsBuffer.Add(bagCompound.Key, bagCompound.Value);
                    }
                }
            }

            // Then capture capacities as the bag might not have every compound in it, so the above loop would miss
            // them
            foreach (var compound in usefulCompoundsBuffer)
            {
                // If not useful, don't participate in receiving this compound.
                // The capacity check here returns 0 if the compound is not useful.
                var bagCapacity = bag.GetCapacityForCompound(compound);
                if (bagCapacity <= 0)
                    continue;

                GD.Print($" Capacity({compound}): {bagCapacity}");

                // Report the capacity so that correct fraction of compound is received
                if (compoundCapacities.TryGetValue(compound, out var capacity))
                {
                    compoundCapacities[compound] = capacity + bagCapacity;
                }
                else
                {
                    compoundCapacities.Add(compound, bagCapacity);
                }
            }
        }

        if (needsNonUsefulAdjustments)
        {
            // We want to remove these amounts to not cause infinite compounds to accumulate
            foreach (var entry in summedCompoundsBuffer)
            {
                var receiverCapacity = compoundCapacities[entry.Key];

                // Exclude already used space
                foreach (var bag in bags)
                {
                    if (bag.IsUseful(entry.Key))
                    {
                        receiverCapacity -= bag.GetCompoundAmount(entry.Key);
                    }
                }

                // Share the outgoing capacity across cells if there isn't enough space
                var sendFactor = Math.Min(1, receiverCapacity / entry.Value);

                GD.Print($"Not useful: {entry.Key}, available space: {receiverCapacity}");

                // Then take as much from the sender bags as the receivers can take
                foreach (var bag in bags)
                {
                    if (bag.IsUseful(entry.Key))
                        continue;

                    var amount = bag.GetCompoundAmount(entry.Key);
                    if (amount <= 0)
                        continue;

                    // Take as much from the bag as can fit in receivers
                    var toTake = Math.Min(receiverCapacity, amount * sendFactor);
                    GD.Print($"Moving this amount of not useful out: {toTake}");

                    // Share the outgoing capacity across cells if there isn't enough space
                    /*if (receiverCapacity < entry.Value)
                    {
                        toTake *= amount / entry.Value;
                        GD.Print($"To take adjustment to: {toTake}");
                    }*/

                    // If ran out of space, don't take anything.
                    if (toTake > 0)
                    {
                        bag.TakeCompound(entry.Key, toTake);
                        GD.Print($"Amount left in sender bag: {bag.GetCompoundAmount(entry.Key)}");
                        receiverCapacity -= toTake;
                    }

                    // If we didn't take the full amount, then we need to reduce the number of compounds to share
                    var couldNotShare = amount - toTake;
                    if (couldNotShare > 0)
                    {
                        availableCompounds[entry.Key] -= couldNotShare;
                        GD.Print($"Adjusted available compound down to: {availableCompounds[entry.Key]}");
                    }

                    GD.Print($"Finished one bag, capacity left: {receiverCapacity}");
                }
            }

            summedCompoundsBuffer.Clear();
        }

        GD.Print("Compound capacities:");
        foreach (var entry in compoundCapacities)
        {
            GD.Print($" {entry.Key}: {entry.Value}");
        }

        GD.Print("Compounds to share:");
        foreach (var entry in availableCompounds)
        {
            GD.Print($" {entry.Key}: {entry.Value}");
        }

#if DEBUG
        if (availableCompounds.ContainsKey(Compound.ATP))
            throw new InvalidOperationException("ATP compound should not be shareable");
#endif

        foreach (var compound in usefulCompoundsBuffer)
        {
            if (!compoundCapacities.TryGetValue(compound, out var storage) || storage <= 0)
                ReportZeroCapacityForUsefulCompoundOnce(SimulationParameters.GetCompound(compound));
        }

        // Now, we know how many shareable compounds there are and how much capacity there is for each compound.
        // So we can give each eligible receiver their share.

        foreach (var entry in availableCompounds)
        {
            GD.Print($"Amount of {entry.Key} to share: {entry.Value}");
            foreach (var bag in bags)
            {
                var capacity = bag.GetCapacityForCompound(entry.Key);

                // Don't participate if you can't receive the compound
                if (capacity <= 0)
                    continue;

                // Other bags will grab a share according to their capacity
                var targetLevel = availableCompounds[entry.Key] * (capacity / compoundCapacities[entry.Key]);

                var difference = targetLevel - bag.GetCompoundAmount(entry.Key);

                GD.Print($"Bag gets ({entry.Key}) {targetLevel} out of {entry.Value}. Adjustment: {difference}");

                if (difference > 0)
                {
                    bag.AddCompound(entry.Key, difference);
                }
                else
                {
                    // TODO: should we check the return value here to ensure enough was taken?
                    // We need to negate the value to get it to be positive for taking away compounds
                    bag.TakeCompound(entry.Key, -difference);
                }

                GD.Print($" Resulting amount of {entry.Key}: {bag.GetCompoundAmount(entry.Key)}");
            }
        }
    }

    public void ClampNegativeCompoundAmounts()
    {
        foreach (var bag in GetCompoundBags())
            bag.ClampNegativeCompoundAmounts();
    }

    public bool IsUsefulInAnyCompoundBag(Compound compound)
    {
        return IsUsefulInAnyCompoundBag(SimulationParameters.GetCompound(compound), GetCompoundBags());
    }

    public bool AnyIsUsefulInAnyCompoundBag(List<Compound> compounds)
    {
        // Fetch this once to keep the hot path on the concrete list type.
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

    public float GetCompoundAmount(Compound compound)
    {
        float sum = 0;
        foreach (var bag in GetCompoundBags())
        {
            sum += bag.GetCompoundAmount(compound);
        }

        return sum;
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

    /// <summary>
    ///   Returns a dictionary that contains the combined compounds of the entire colony. The returned dictionary
    ///   shouldn't be stored or modified.
    /// </summary>
    public Dictionary<Compound, float> GetCompoundDictionary()
    {
        FillSummedCompoundsBuffer(GetCompoundBags());

        return summedCompoundsBuffer;
    }

    private static bool IsUsefulInAnyCompoundBag(CompoundDefinition compound, List<CompoundBag> compoundBags)
    {
        foreach (var compoundBag in compoundBags)
        {
            if (compoundBag.IsUseful(compound))
                return true;
        }

        return false;
    }

    private void FillSummedCompoundsBuffer(List<CompoundBag> bags)
    {
        summedCompoundsBuffer.Clear();

        foreach (var compoundBag in bags)
        {
            foreach (var pair in compoundBag.Compounds)
            {
                // Don't need to count compounds there isn't any of
                if (pair.Value <= 0)
                    continue;

                // If a bag does not accept a compound, we should not count it to prevent infinite compounds issue
                if (!compoundBag.IsUseful(pair.Key))
                    continue;

                if (!summedCompoundsBuffer.TryGetValue(pair.Key, out var existingAmount))
                {
                    summedCompoundsBuffer.Add(pair.Key, pair.Value);
                    continue;
                }

                summedCompoundsBuffer[pair.Key] = existingAmount + pair.Value;
            }
        }
    }

    private void ReportZeroCapacityForUsefulCompoundOnce(CompoundDefinition compoundDefinition)
    {
        if (nanIssueReported)
            return;

        GD.PrintErr($"Compound {compoundDefinition.Name} is set to useful but has a Capacity of zero, " +
            "https://github.com/Revolutionary-Games/Thrive/issues/3201");
        nanIssueReported = true;
    }

    private List<CompoundBag> GetCompoundBags()
    {
        return colonyBags;
    }
}
