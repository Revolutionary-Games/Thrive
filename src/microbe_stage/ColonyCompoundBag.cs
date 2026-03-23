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
    private readonly Dictionary<Compound, float> summedCompoundsBuffer = new();

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
        FillSummedCompoundsBuffer(bags);

        foreach (var (compound, compoundAmount) in summedCompoundsBuffer)
        {
            var compoundDefinition = SimulationParameters.GetCompound(compound);
            if (!TryPrepareCompoundDistribution(compound, compoundAmount, bags, compoundDefinition, out var ratio))
                continue;

            RedistributeCompoundAcrossBags(compound, compoundDefinition, ratio, bags);
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
                if (!summedCompoundsBuffer.TryGetValue(pair.Key, out var existingAmount))
                {
                    summedCompoundsBuffer.Add(pair.Key, pair.Value);
                    continue;
                }

                summedCompoundsBuffer[pair.Key] = existingAmount + pair.Value;
            }
        }
    }

    private bool TryPrepareCompoundDistribution(Compound compound, float compoundAmount, List<CompoundBag> bags,
        CompoundDefinition compoundDefinition, out float ratio)
    {
        ratio = 0;

        if (!compoundDefinition.CanBeDistributed)
            return false;

        float compoundCapacity = 0;
        var usefulInAnyBag = false;

        foreach (var bag in bags)
        {
            if (!usefulInAnyBag && bag.IsUseful(compoundDefinition))
                usefulInAnyBag = true;

            compoundCapacity += bag.GetCapacityForCompound(compound);
        }

        if (!usefulInAnyBag)
            return false;

        // This is just an error print, can be removed if no more NaN issues occur
        // See also CompoundBag.FixNaNCompounds which fixes NaN values after they occur
        if (compoundCapacity == 0)
        {
            ReportZeroCapacityForUsefulCompoundOnce(compoundDefinition);
            return false;
        }

        ratio = compoundAmount / compoundCapacity;
        return true;
    }

    private void ReportZeroCapacityForUsefulCompoundOnce(CompoundDefinition compoundDefinition)
    {
        if (nanIssueReported)
            return;

        GD.PrintErr($"Compound {compoundDefinition.Name} is set to useful but has a Capacity of zero, " +
            "https://github.com/Revolutionary-Games/Thrive/issues/3201");
        nanIssueReported = true;
    }

    private void RedistributeCompoundAcrossBags(Compound compound, CompoundDefinition compoundDefinition,
        float ratio, List<CompoundBag> bags)
    {
        foreach (var bag in bags)
        {
            if (!bag.IsUseful(compoundDefinition))
                continue;

            var expectedAmount = ratio * bag.GetCapacityForCompound(compound);
            var surplus = bag.GetCompoundAmount(compound) - expectedAmount;

            if (surplus > 0)
            {
                bag.TakeCompound(compound, surplus);
                continue;
            }

            bag.AddCompound(compound, -surplus);
        }
    }

    private List<CompoundBag> GetCompoundBags()
    {
        return colonyBags;
    }
}
