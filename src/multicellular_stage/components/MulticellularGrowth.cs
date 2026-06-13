namespace Components;

using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Godot;
using SharedBase.Archive;
using Systems;

/// <summary>
///   Keeps track of multicellular growth data
/// </summary>
public struct MulticellularGrowth : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 3;

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

    public bool IsASpore;

    public bool SpawnedInitialMassBuddingCells;

    public MulticellularGrowth(MulticellularSpecies species)
    {
        this.ResetGrowthProgress();

        ResumeBodyPlanAfterReplacingLost = null;
        EnoughResourcesForBudding = false;

        TargetCellLayout = species.ModifiableGameplayCells;

        // This is updated by ReApplyCellTypeProperties when needed
        this.CalculateTotalBodyPlanCompounds(species);
    }

    public bool IsFullyGrownMulticellular => NextBodyPlanCellToGrowIndex >=
        (TargetCellLayout?.Count ?? throw new InvalidOperationException("Unknown full layout"));

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentMulticellularGrowth;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObjectOrNull(LostPartsOfBodyPlan);
        writer.WriteObjectOrNull(CompoundsNeededForNextCell);

        if (CompoundsUsedForMulticellularGrowth != null)
        {
            writer.WriteObject(CompoundsUsedForMulticellularGrowth);
        }
        else
        {
            writer.WriteNullObject();
        }

        if (TotalNeededForMulticellularGrowth != null)
        {
            writer.WriteObject(TotalNeededForMulticellularGrowth);
        }
        else
        {
            writer.WriteNullObject();
        }

        writer.WriteObjectOrNull(TargetCellLayout);

        writer.Write(ResumeBodyPlanAfterReplacingLost.HasValue);
        if (ResumeBodyPlanAfterReplacingLost.HasValue)
            writer.Write(ResumeBodyPlanAfterReplacingLost.Value);

        writer.Write(NextBodyPlanCellToGrowIndex);
        writer.Write(EnoughResourcesForBudding);

        writer.Write(IsASpore);
        writer.Write(SpawnedInitialMassBuddingCells);
    }
}

public static class MulticellularGrowthHelpers
{
    public static MulticellularGrowth ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > MulticellularGrowth.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, MulticellularGrowth.SERIALIZATION_VERSION);

        var instance = new MulticellularGrowth
        {
            LostPartsOfBodyPlan = reader.ReadObjectOrNull<List<int>>(),
            CompoundsNeededForNextCell = reader.ReadObjectOrNull<List<(Compound Compound, float AmountNeeded)>>(),
            CompoundsUsedForMulticellularGrowth = reader.ReadObjectOrNull<Dictionary<Compound, float>>(),
            TotalNeededForMulticellularGrowth = reader.ReadObjectOrNull<Dictionary<Compound, float>>(),
            TargetCellLayout = reader.ReadObjectOrNull<CellLayout<CellTemplate>>(),
        };

        if (reader.ReadBool())
        {
            instance.ResumeBodyPlanAfterReplacingLost = reader.ReadInt32();
        }
        else
        {
            instance.ResumeBodyPlanAfterReplacingLost = null;
        }

        instance.NextBodyPlanCellToGrowIndex = reader.ReadInt32();
        instance.EnoughResourcesForBudding = reader.ReadBool();

        if (version >= 2)
        {
            instance.IsASpore = reader.ReadBool();
        }

        if (version >= 3)
        {
            instance.SpawnedInitialMassBuddingCells = reader.ReadBool();
        }

        return instance;
    }

    /// <summary>
    ///   Adds the next cell missing from this multicellular species' body plan to this microbe's colony
    /// </summary>
    public static void AddMulticellularGrowthCell(this ref MulticellularGrowth multicellularGrowth,
        in Entity entity, MulticellularSpecies species, IWorldSimulation worldSimulation,
        IMicrobeSpawnEnvironment spawnEnvironment, CommandBuffer recorder, ISpawnSystem notifySpawnTo)
    {
        if (!entity.Has<MicrobeColony>())
        {
            recorder.Add(entity, new MicrobeColony(true, entity, entity.Get<MicrobeControl>().State));
        }

        ref var colonyPosition = ref entity.Get<WorldPosition>();

        var cellTemplate = species.ModifiableGameplayCells[multicellularGrowth.NextBodyPlanCellToGrowIndex];

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
        multicellularGrowth.ResetGrowthProgress();

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

            recorder.Remove<MicrobeColony>(entity);

            worldSimulation.FinishRecordingEntityCommands(recorder);
        }
    }

    /// <summary>
    ///   Resets all growth progress to grow the normal body plan. Used after exiting engulfment (which disbands the
    ///   colony), as well as after returning from the edtior
    /// </summary>
    public static void ResetGrowthProgress(this ref MulticellularGrowth multicellularGrowth)
    {
        // Start growing cells starting with the second one. The first one is the lead cell and gets spawned
        // immediately. Same goes for a few more cells if the species uses the mass budding reproduction method,
        // but that is handled separately by MulticellularGrowthSystem
        multicellularGrowth.NextBodyPlanCellToGrowIndex = 1;
        multicellularGrowth.SpawnedInitialMassBuddingCells = false;
        multicellularGrowth.EnoughResourcesForBudding = false;

        multicellularGrowth.CompoundsNeededForNextCell = null;
        multicellularGrowth.CompoundsUsedForMulticellularGrowth = null;

        multicellularGrowth.TotalNeededForMulticellularGrowth = null;
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

        if (lostPartIndex >= species.ModifiableGameplayCells.Count)
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
        // towards the lost cell; this should ensure the total progress bar should be correct
        if (multicellularGrowth.CompoundsUsedForMulticellularGrowth != null)
        {
            var totalNeededForLostCell = species.ModifiableGameplayCells[lostPartIndex]
                .ModifiableCellType.CalculateTotalComposition();

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
        if (multicellularGrowth.IsFullyGrownMulticellular)
        {
            // Calculate compounds needed for reproduction
            if (species.ReproductionMethod is MulticellularReproductionMethod.Budding
                or MulticellularReproductionMethod.Sporulation)
            {
                return species.FirstCellTypeToSpawn().CalculateTotalCompositionList();
            }

            if (species.ReproductionMethod is MulticellularReproductionMethod.MassBudding)
            {
                var total = new List<(Compound Compound, float AmountNeeded)>();

                for (int i = 0; i < species.MassBuddingCellCount; ++i)
                {
                    species.ModifiableGameplayCells[i].CalculateTotalCompositionList(total);
                }

                return total;
            }

            throw new NotImplementedException($"Reproduction method's reproduction cost calculation is" +
                $"unimplemented: {species.ReproductionMethod}");
        }

        return species.ModifiableGameplayCells[multicellularGrowth.NextBodyPlanCellToGrowIndex]
            .ModifiableCellType.CalculateTotalCompositionList();
    }

    public static void CalculateTotalBodyPlanCompounds(this ref MulticellularGrowth multicellularGrowth,
        Species species)
    {
        multicellularGrowth.TotalNeededForMulticellularGrowth ??= new Dictionary<Compound, float>();
        multicellularGrowth.TotalNeededForMulticellularGrowth.Clear();

        foreach (var cell in multicellularGrowth.TargetCellLayout ??
                 throw new InvalidOperationException("Unknown target layout"))
        {
            multicellularGrowth.TotalNeededForMulticellularGrowth.Merge(cell.ModifiableCellType
                .CalculateTotalComposition());
        }

        multicellularGrowth.TotalNeededForMulticellularGrowth.Merge(species.BaseReproductionCost);
    }

    public static void GerminateSpore(this ref MulticellularGrowth multicellularGrowth,
        in Entity entity, IWorldSimulation worldSimulation, IMicrobeSpawnEnvironment microbeSpawnEnvironment,
        List<Hex> workMemory1, List<Hex> workMemory2)
    {
        if (!entity.Has<MulticellularSpeciesMember>())
            return;

        if (!multicellularGrowth.IsASpore)
            return;

        ref var control = ref entity.Get<MicrobeControl>();

        control.GerminatingSpore = false;

        ref var cellProperties = ref entity.Get<CellProperties>();

        ref var multicellularSpeciesType = ref entity.Get<MulticellularSpeciesMember>();

        multicellularSpeciesType.MulticellularCellType = multicellularSpeciesType.Species.ColonyRootCellType();

        multicellularGrowth.IsASpore = false;

        var resolvedTolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(
            MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(multicellularSpeciesType.Species,
                microbeSpawnEnvironment.CurrentBiome));

        ref var environmentalEffects = ref entity.Get<MicrobeEnvironmentalEffects>();

        var totalSpecializationBonus = multicellularSpeciesType.MulticellularCellType.CellTypeSpecializationBonus *
            multicellularSpeciesType.Species.GetAdjacencySpecializationBonus(0);

        environmentalEffects.ApplyEffects(resolvedTolerances, totalSpecializationBonus, ref entity.Get<BioProcesses>());

        cellProperties.ReApplyCellTypeProperties(ref environmentalEffects, entity,
            multicellularSpeciesType.MulticellularCellType, multicellularSpeciesType.Species, totalSpecializationBonus,
            worldSimulation, workMemory1, workMemory2);
    }

    public static void SpawnInitialMassBuddingCells(this ref MulticellularGrowth multicellularGrowth, in Entity entity,
        MulticellularSpecies species, IWorldSimulation worldSimulation, IMicrobeSpawnEnvironment spawnEnvironment,
        CommandBuffer recorder, ISpawnSystem notifySpawnTo)
    {
        if (multicellularGrowth.NextBodyPlanCellToGrowIndex != 1)
        {
            GD.PrintErr($"Tried to spawn initial mass budding cells ({species.ReadableName}) while some colony"
                + $" cells were already grown (x{multicellularGrowth.NextBodyPlanCellToGrowIndex})");

            multicellularGrowth.SpawnedInitialMassBuddingCells = true;
            return;
        }

        for (int i = 0; i < species.MassBuddingCellCount - 1; ++i)
        {
            multicellularGrowth.AddMulticellularGrowthCell(entity, species, worldSimulation, spawnEnvironment,
                recorder, notifySpawnTo);
        }

        multicellularGrowth.SpawnedInitialMassBuddingCells = true;
    }
}
