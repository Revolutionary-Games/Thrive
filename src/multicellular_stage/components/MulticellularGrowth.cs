﻿namespace Components;

using System;
using System.Collections.Generic;
using System.Linq;
using DefaultEcs;
using DefaultEcs.Command;
using Godot;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   Keeps track of multicellular growth data
/// </summary>
[JSONDynamicTypeAllowed]
public struct MulticellularGrowth
{
    /// <summary>
    ///   List of cells that need to be regrown, after being lost, in
    ///   <see cref="MulticellularGrowthHelpers.AddMulticellularGrowthCell"/>
    /// </summary>
    public List<int>? LostPartsOfBodyPlan;

    // TODO: update the growth system to reuse these objects instead of needing to clear these to null
    public List<(Compound Compound, float AmountNeeded)>? CompoundsNeededForNextCell;

    public Dictionary<Compound, float>? CompoundsUsedForMulticellularGrowth;

    public Dictionary<Compound, float>? TotalNeededForMulticellularGrowth;

    /// <summary>
    ///   The final cell layout this multicellular species member is growing towards
    /// </summary>
    public CellLayout<CellTemplate>? TargetCellLayout;

    // TODO: switch this to non-nullable (and add a separate variable indicating if replacing something)
    /// <summary>
    ///   Once all lost body plan parts have been grown, this is the index the growing resumes at
    /// </summary>
    public int? ResumeBodyPlanAfterReplacingLost;

    // TODO: MulticellularBodyPlanPartIndex used to be here, now it is in MulticellularSpeciesMember
    // which means that a new system is needed to create MulticellularGrowth components on ejected cells that
    // should be allowed to resume growing

    public int NextBodyPlanCellToGrowIndex;

    public bool EnoughResourcesForBudding;

    public MulticellularGrowth(MulticellularSpecies species)
    {
        // Start growing at the cell after the initial bud
        // TODO: this needs changing when other reproduction methods are implemented (this same thing is also
        // in ResetMulticellularProgress)
        NextBodyPlanCellToGrowIndex = 1;

        LostPartsOfBodyPlan = null;
        CompoundsNeededForNextCell = null;
        CompoundsUsedForMulticellularGrowth = null;
        TotalNeededForMulticellularGrowth = null;
        ResumeBodyPlanAfterReplacingLost = null;
        EnoughResourcesForBudding = false;

        TargetCellLayout = species.Cells;

        // This is updated by ReApplyCellTypeProperties when needed
        this.CalculateTotalBodyPlanCompounds(species);
    }

    [JsonIgnore]
    public bool IsFullyGrownMulticellular => NextBodyPlanCellToGrowIndex >=
        (TargetCellLayout?.Count ?? throw new InvalidOperationException("Unknown full layout"));
}

public static class MulticellularGrowthHelpers
{
    /// <summary>
    ///   Adds the next cell missing from this multicellular species' body plan to this microbe's colony
    /// </summary>
    public static void AddMulticellularGrowthCell(this ref MulticellularGrowth multicellularGrowth,
        in Entity entity, MulticellularSpecies species, IWorldSimulation worldSimulation,
        IMicrobeSpawnEnvironment spawnEnvironment, EntityCommandRecorder recorder, ISpawnSystem notifySpawnTo)
    {
        if (!entity.Has<MicrobeColony>())
        {
            var entityRecord = recorder.Record(entity);

            entityRecord.Set(new MicrobeColony(true, entity, entity.Get<MicrobeControl>().State));
        }

        ref var colonyPosition = ref entity.Get<WorldPosition>();

        var cellTemplate = species.Cells[multicellularGrowth.NextBodyPlanCellToGrowIndex];

        // Remove the starting compounds as this is a growth cell which shouldn't give free resources to the
        // colony it joins
        DelayedColonyOperationSystem.CreateDelayAttachedMicrobe(ref colonyPosition, entity,
            multicellularGrowth.NextBodyPlanCellToGrowIndex, cellTemplate, species, worldSimulation, spawnEnvironment,
            recorder, notifySpawnTo, false);

        ++multicellularGrowth.NextBodyPlanCellToGrowIndex;
        multicellularGrowth.CompoundsNeededForNextCell = null;
    }

    public static void ResetMulticellularProgress(this ref MulticellularGrowth multicellularGrowth,
        in Entity entity, IWorldSimulation worldSimulation)
    {
        // Clear variables

        // The first cell is the last to duplicate (budding reproduction) so the body plan starts filling at index 1
        // Note that this is also set in the struct constructor
        multicellularGrowth.NextBodyPlanCellToGrowIndex = 1;
        multicellularGrowth.EnoughResourcesForBudding = false;

        multicellularGrowth.CompoundsNeededForNextCell = null;
        multicellularGrowth.CompoundsUsedForMulticellularGrowth = null;

        multicellularGrowth.TotalNeededForMulticellularGrowth = null;

        // Delete the cells in our colony currently
        if (entity.Has<MicrobeColony>())
        {
            var recorder = worldSimulation.StartRecordingEntityCommands();

            ref var colony = ref entity.Get<MicrobeColony>();

            foreach (var member in colony.ColonyMembers)
            {
                if (member == entity)
                    continue;

                worldSimulation.DestroyEntity(member);
            }

            var entityRecord = recorder.Record(entity);
            entityRecord.Remove<MicrobeColony>();
            worldSimulation.FinishRecordingEntityCommands(recorder);
        }
    }

    public static void OnMulticellularColonyCellLost(this ref MulticellularGrowth multicellularGrowth,
        ref OrganelleContainer organelleContainer, CompoundBag compoundRefundLocation, in Entity colonyEntity,
        in Entity lostCell)
    {
        var species = colonyEntity.Get<MulticellularSpeciesMember>().Species;

        var lostPartIndex = lostCell.Get<MulticellularSpeciesMember>().MulticellularBodyPlanPartIndex;

        // If the lost index is the first cell, then it should be disbanding the colony. We don't need to keep
        // track of when that will regrow as entirely new colonies will be created for the surviving members.
        // This shouldn't really matter anyway as this growth object should be getting deleted anyway shortly along
        // with the removed cell.
        if (lostPartIndex == 0)
            return;

        if (lostPartIndex >= species.Cells.Count)
        {
            GD.PrintErr("Multicellular colony lost a cell at index that is no longer valid for the species, " +
                "ignoring this for regrowing");

            // TODO: does this need to  adjust multicellularGrowth.CompoundsUsedForMulticellularGrowth?
            return;
        }

        // We need to reset our growth towards the next cell and instead replace the cell we just lost
        multicellularGrowth.LostPartsOfBodyPlan ??= new List<int>();

        // TODO: figure out why these duplicate calls come from colonies, we ignore them for now
        if (multicellularGrowth.LostPartsOfBodyPlan.Contains(lostPartIndex))
            return;

        multicellularGrowth.LostPartsOfBodyPlan.Add(lostPartIndex);
        organelleContainer.AllOrganellesDivided = false;

        if (multicellularGrowth.ResumeBodyPlanAfterReplacingLost != null)
        {
            // We are already regrowing something, so we need to remember that by adding it back to the list
            multicellularGrowth.LostPartsOfBodyPlan.Add(multicellularGrowth.NextBodyPlanCellToGrowIndex);
        }

        var usedForProgress = new List<(Compound Compound, float AmountNeeded)>();

        if (multicellularGrowth.CompoundsNeededForNextCell != null)
        {
            var totalNeededForCurrentlyGrowingCell = multicellularGrowth.GetCompoundsNeededForNextCell(species);

            foreach (var entry in totalNeededForCurrentlyGrowingCell)
            {
                var id = multicellularGrowth.CompoundsNeededForNextCell!.FindIndexByKey(entry.Compound);

                if (id != -1)
                {
                    var alreadyUsed = entry.AmountNeeded
                        - multicellularGrowth.CompoundsNeededForNextCell![id].AmountNeeded;

                    if (alreadyUsed > 0)
                        usedForProgress.Add((entry.Compound, alreadyUsed));
                }
            }

            multicellularGrowth.CompoundsNeededForNextCell = null;
        }
        else if (multicellularGrowth.EnoughResourcesForBudding)
        {
            // Refund the budding cost
            usedForProgress = multicellularGrowth.GetCompoundsNeededForNextCell(species);
        }

        multicellularGrowth.EnoughResourcesForBudding = false;

        // TODO: maybe we should use a separate store for the used compounds for the next cell progress, for now
        // just add those to our storage (even with the risk of us losing some compounds due to too little storage)
        foreach (var entry in usedForProgress)
        {
            if (entry.AmountNeeded > MathUtils.EPSILON)
                compoundRefundLocation.AddCompound(entry.Compound, entry.AmountNeeded);
        }

        // Adjust the already used compound amount to lose the progress we made for the current cell and also
        // towards the lost cell, this should ensure the total progress bar should be correct
        if (multicellularGrowth.CompoundsUsedForMulticellularGrowth != null)
        {
            var totalNeededForLostCell = species.Cells[lostPartIndex]
                .CellType.CalculateTotalComposition();

            foreach (var compound in multicellularGrowth.CompoundsUsedForMulticellularGrowth.Keys.ToArray())
            {
                var totalUsed = multicellularGrowth.CompoundsUsedForMulticellularGrowth[compound];

                int index = usedForProgress.FindIndexByKey(compound);

                if (index != -1)
                {
                    totalUsed -= usedForProgress[index].AmountNeeded;
                }

                if (totalNeededForLostCell.TryGetValue(compound, out var wasted))
                {
                    totalUsed -= wasted;
                }

                if (totalUsed < 0)
                    totalUsed = 0;

                multicellularGrowth.CompoundsUsedForMulticellularGrowth[compound] = totalUsed;
            }
        }
    }

    public static List<(Compound Compound, float AmountNeeded)> GetCompoundsNeededForNextCell(
        this ref MulticellularGrowth multicellularGrowth, MulticellularSpecies species)
    {
        return species
            .Cells[
                multicellularGrowth.IsFullyGrownMulticellular ? 0 : multicellularGrowth.NextBodyPlanCellToGrowIndex]
            .CellType.CalculateTotalCompositionList();
    }

    public static void CalculateTotalBodyPlanCompounds(this ref MulticellularGrowth multicellularGrowth,
        Species species)
    {
        multicellularGrowth.TotalNeededForMulticellularGrowth ??= new Dictionary<Compound, float>();
        multicellularGrowth.TotalNeededForMulticellularGrowth.Clear();

        foreach (var cell in multicellularGrowth.TargetCellLayout ??
                 throw new InvalidOperationException("Unknown target layout"))
        {
            multicellularGrowth.TotalNeededForMulticellularGrowth.Merge(cell.CellType.CalculateTotalComposition());
        }

        multicellularGrowth.TotalNeededForMulticellularGrowth.Merge(species.BaseReproductionCost);
    }
}
