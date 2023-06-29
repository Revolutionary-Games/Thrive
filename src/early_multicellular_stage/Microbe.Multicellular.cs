using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Early multicellular functionality of the microbes. This is a separate file to put all of the core logic of
///   the overridden behaviours in the same place.
/// </summary>
public partial class Microbe
{
    [JsonProperty]
    private int nextBodyPlanCellToGrowIndex = -1;

    /// <summary>
    ///   List of cells that need to be regrown after being lost in <see cref="AddMulticellularGrowthCell"/>
    /// </summary>
    [JsonProperty]
    private List<int>? lostPartsOfBodyPlan;

    /// <summary>
    ///   Once all lost body plan parts have been grown, this is the index the growing resumes at
    /// </summary>
    [JsonProperty]
    private int? resumeBodyPlanAfterReplacingLost;

    [JsonProperty]
    private bool enoughResourcesForBudding;

    [JsonProperty]
    private Dictionary<Compound, float>? compoundsNeededForNextCell;

    [JsonProperty]
    private Dictionary<Compound, float>? compoundsUsedForMulticellularGrowth;

    [JsonProperty]
    private Dictionary<Compound, float>? totalNeededForMulticellularGrowth;

    [JsonIgnore]
    public bool IsFullyGrownMulticellular => nextBodyPlanCellToGrowIndex >= CastedMulticellularSpecies.Cells.Count;

    /// <summary>
    ///   Used to keep track of which part of a body plan a non-first cell in a multicellular colony is.
    ///   This is required for regrowing after losing a cell.
    /// </summary>
    [JsonProperty]
    public int MulticellularBodyPlanPartIndex { get; set; }

    public void ApplyMulticellularNonFirstCellSpecies(EarlyMulticellularSpecies species, CellType cellType)
    {
        cachedMicrobeSpecies = null;
        cachedMulticellularSpecies = species;
        MulticellularCellType = cellType;

        Species = species;

        FinishSpeciesSetup();

        // We have to force our membrane to be setup here so that the attach logic will have valid membrane data
        // to work with
        SendOrganellePositionsToMembrane();
    }

    /// <summary>
    ///   Adds the next cell missing from this multicellular species' body plan to this microbe's colony
    /// </summary>
    public void AddMulticellularGrowthCell(bool keepCompounds = false)
    {
        if (Colony == null)
        {
            MicrobeColony.CreateColonyForMicrobe(this);

            if (Colony == null)
                throw new Exception("An issue occured during colony creation!");
        }

        var template = CastedMulticellularSpecies.Cells[nextBodyPlanCellToGrowIndex];

        var cell = CreateMulticellularColonyMemberCell(template.CellType, keepCompounds);
        cell.MulticellularBodyPlanPartIndex = nextBodyPlanCellToGrowIndex;

        // We don't reset our state here in case we want to be in engulf mode
        cell.State = State;

        // Attach the created cell to the right spot in our colony
        var ourTransform = GlobalTransform;

        var attachVector = ourTransform.origin + ourTransform.basis.Xform(Hex.AxialToCartesian(template.Position));

        // Ensure no tiny y component exists here
        attachVector.y = 0;

        var newCellTransform = new Transform(
            MathUtils.CreateRotationForOrganelle(template.Orientation) * ourTransform.basis.Quat(),
            attachVector);
        cell.GlobalTransform = newCellTransform;

        var newCellPosition = newCellTransform.origin;

        // Adding a cell to a colony snaps it close to its colony parent so we need to find the closes existing
        // cell in the colony to use as that here
        var parent = this;
        var currentDistanceSquared = (newCellPosition - ourTransform.origin).LengthSquared();

        foreach (var colonyMember in Colony.ColonyMembers)
        {
            if (colonyMember == this)
                continue;

            var distance = (colonyMember.GlobalTransform.origin - newCellPosition).LengthSquared();

            if (distance < currentDistanceSquared)
            {
                parent = colonyMember;
                currentDistanceSquared = distance;
            }
        }

        Colony.AddToColony(cell, parent);

        ++nextBodyPlanCellToGrowIndex;
        compoundsNeededForNextCell = null;
    }

    public void BecomeFullyGrownMulticellularColony()
    {
        while (!IsFullyGrownMulticellular)
        {
            AddMulticellularGrowthCell(true);
        }
    }

    private void HandleMulticellularReproduction(float elapsedSinceLastUpdate)
    {
        compoundsUsedForMulticellularGrowth ??= new Dictionary<Compound, float>();

        var (remainingAllowedCompoundUse, remainingFreeCompounds) =
            CalculateFreeCompoundsAndLimits(elapsedSinceLastUpdate);

        if (compoundsNeededForNextCell == null)
        {
            // Regrow lost cells
            if (lostPartsOfBodyPlan is { Count: > 0 })
            {
                // Store where we will resume from
                resumeBodyPlanAfterReplacingLost ??= nextBodyPlanCellToGrowIndex;

                // Grow from the first cell to grow back, in the body plan grow order
                nextBodyPlanCellToGrowIndex = lostPartsOfBodyPlan.Min();
                lostPartsOfBodyPlan.Remove(nextBodyPlanCellToGrowIndex);
            }
            else if (resumeBodyPlanAfterReplacingLost != null)
            {
                // Done regrowing, resume where we were
                nextBodyPlanCellToGrowIndex = resumeBodyPlanAfterReplacingLost.Value;
                resumeBodyPlanAfterReplacingLost = null;
            }

            // Need to setup the next cell to be grown in our body plan
            if (IsFullyGrownMulticellular)
            {
                // We have completed our body plan and can (once enough resources) reproduce
                if (enoughResourcesForBudding)
                {
                    ReadyToReproduce();
                }
                else
                {
                    // Apply the base reproduction cost at this point after growing the full layout
                    if (!ProcessBaseReproductionCost(ref remainingAllowedCompoundUse, ref remainingFreeCompounds,
                            compoundsUsedForMulticellularGrowth))
                    {
                        // Not ready yet for budding
                        return;
                    }

                    // Budding cost is after the base reproduction cost has been overcome
                    compoundsNeededForNextCell = GetCompoundsNeededForNextCell();
                }

                return;
            }

            compoundsNeededForNextCell = GetCompoundsNeededForNextCell();
        }

        bool stillNeedsSomething = false;

        // Consume some compounds for the next cell in the layout
        // Similar logic for "growing" more cells than in PlacedOrganelle growth
        foreach (var entry in consumeReproductionCompoundsReverse ?
                     compoundsNeededForNextCell.Reverse() :
                     compoundsNeededForNextCell)
        {
            var amountNeeded = entry.Value;

            float usedAmount = 0;

            float allowedUseAmount = Math.Min(amountNeeded, remainingAllowedCompoundUse);

            if (remainingFreeCompounds > 0)
            {
                var usedFreeCompounds = Math.Min(allowedUseAmount, remainingFreeCompounds);
                usedAmount += usedFreeCompounds;
                allowedUseAmount -= usedFreeCompounds;

                // As we loop just once we don't need to update the free compounds or allowed use compounds variables
            }

            stillNeedsSomething = true;

            var amountAvailable = Compounds.GetCompoundAmount(entry.Key) -
                Constants.ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST;

            if (amountAvailable > MathUtils.EPSILON)
            {
                // We can take some
                var amountToTake = Mathf.Min(allowedUseAmount, amountAvailable);

                usedAmount += Compounds.TakeCompound(entry.Key, amountToTake);
            }

            var left = amountNeeded - usedAmount;

            if (left < 0.0001f)
            {
                compoundsNeededForNextCell.Remove(entry.Key);
            }
            else
            {
                compoundsNeededForNextCell[entry.Key] = left;
            }

            compoundsUsedForMulticellularGrowth.TryGetValue(entry.Key, out float alreadyUsed);

            compoundsUsedForMulticellularGrowth[entry.Key] = alreadyUsed + usedAmount;

            // As we modify the list, we are content just consuming one type of compound per frame
            break;
        }

        if (!stillNeedsSomething)
        {
            // The current cell to grow is now ready to be added
            // Except in the case that we were just getting resources for budding, skip in that case
            if (!IsFullyGrownMulticellular)
            {
                AddMulticellularGrowthCell();
            }
            else
            {
                // Has collected enough resources to spawn the first cell type as budding type reproduction
                enoughResourcesForBudding = true;
                compoundsNeededForNextCell = null;
            }
        }
    }

    private Dictionary<Compound, float> GetCompoundsNeededForNextCell()
    {
        return CastedMulticellularSpecies.Cells[IsFullyGrownMulticellular ? 0 : nextBodyPlanCellToGrowIndex].CellType
            .CalculateTotalComposition();
    }

    private void ResetMulticellularProgress()
    {
        // Clear variables

        // The first cell is the last to duplicate (budding reproduction) so the body plan starts filling at index 1
        nextBodyPlanCellToGrowIndex = 1;
        enoughResourcesForBudding = false;

        compoundsNeededForNextCell = null;
        compoundsUsedForMulticellularGrowth = null;

        totalNeededForMulticellularGrowth = null;

        // Delete the cells in our colony currently
        if (Colony != null)
        {
            GD.Print("Resetting growth in a multicellular colony");
            var cellsToDestroy = Colony.ColonyMembers.Where(m => m != this).ToList();

            Colony.RemoveFromColony(this);

            foreach (var microbe in cellsToDestroy)
            {
                microbe.DetachAndQueueFree();
            }
        }
    }

    private Microbe CreateMulticellularColonyMemberCell(CellType cellType, bool keepCompounds)
    {
        var newCell = SpawnHelpers.SpawnMicrobe(Species, Translation,
            GetParent(), SpawnHelpers.LoadMicrobeScene(), true, cloudSystem!, spawnSystem!, CurrentGame, cellType);

        // Make it despawn like normal (if our colony is accidentally somehow disbanded)
        throw new NotImplementedException();

        // spawnSystem!.NotifyExternalEntitySpawned(newCell);

        if (!keepCompounds)
        {
            // Remove the compounds from the created cell
            newCell.Compounds.ClearCompounds();
        }

        // TODO: different sound effect?
        PlaySoundEffect("res://assets/sounds/soundeffects/reproduction.ogg");

        return newCell;
    }

    private void OnMulticellularColonyCellLost(Microbe cell)
    {
        // Don't bother if this cell is being destroyed
        if (destroyed)
            return;

        // We need to reset our growth towards the next cell and instead replace the cell we just lost
        lostPartsOfBodyPlan ??= new List<int>();

        // TODO: figure out why these duplicate calls come from colonies, we ignore them for now
        if (lostPartsOfBodyPlan.Contains(cell.MulticellularBodyPlanPartIndex))
            return;

        lostPartsOfBodyPlan.Add(cell.MulticellularBodyPlanPartIndex);
        allOrganellesDivided = false;

        if (resumeBodyPlanAfterReplacingLost != null)
        {
            // We are already regrowing something, so we need to remember that by adding it back to the list
            lostPartsOfBodyPlan.Add(nextBodyPlanCellToGrowIndex);
        }

        var usedForProgress = new Dictionary<Compound, float>();

        if (compoundsNeededForNextCell != null)
        {
            var totalNeededForCurrentlyGrowingCell = GetCompoundsNeededForNextCell();

            foreach (var entry in totalNeededForCurrentlyGrowingCell)
            {
                var compound = entry.Key;
                var neededAmount = entry.Value;

                if (compoundsNeededForNextCell.TryGetValue(compound, out var left))
                {
                    var alreadyUsed = neededAmount - left;

                    if (alreadyUsed > 0)
                        usedForProgress.Add(compound, alreadyUsed);
                }
            }

            compoundsNeededForNextCell = null;
        }
        else if (enoughResourcesForBudding)
        {
            // Refund the budding cost
            usedForProgress = GetCompoundsNeededForNextCell();
        }

        enoughResourcesForBudding = false;

        // TODO: maybe we should use a separate store for the used compounds for the next cell progress, for now
        // just add those to our storage (even with the risk of us losing some compounds due to too little storage)
        foreach (var entry in usedForProgress)
        {
            if (entry.Value > MathUtils.EPSILON)
                Compounds.AddCompound(entry.Key, entry.Value);
        }

        // Adjust the already used compound amount to lose the progress we made for the current cell and also towards
        // the lost cell, this we the total progress bar should be correct
        if (compoundsUsedForMulticellularGrowth != null)
        {
            var totalNeededForLostCell = CastedMulticellularSpecies.Cells[cell.MulticellularBodyPlanPartIndex].CellType
                .CalculateTotalComposition();

            foreach (var compound in compoundsUsedForMulticellularGrowth.Keys.ToArray())
            {
                var totalUsed = compoundsUsedForMulticellularGrowth[compound];

                if (usedForProgress.TryGetValue(compound, out var wasted))
                {
                    totalUsed -= wasted;
                }

                if (totalNeededForLostCell.TryGetValue(compound, out wasted))
                {
                    totalUsed -= wasted;
                }

                if (totalUsed < 0)
                    totalUsed = 0;

                compoundsUsedForMulticellularGrowth[compound] = totalUsed;
            }
        }
    }

    private Dictionary<Compound, float> CalculateTotalBodyPlanCompounds()
    {
        if (totalNeededForMulticellularGrowth == null)
        {
            totalNeededForMulticellularGrowth = new Dictionary<Compound, float>();

            foreach (var cell in CastedMulticellularSpecies.Cells)
            {
                totalNeededForMulticellularGrowth.Merge(cell.CellType.CalculateTotalComposition());
            }

            totalNeededForMulticellularGrowth.Merge(Species.BaseReproductionCost);
        }

        return totalNeededForMulticellularGrowth;
    }
}
