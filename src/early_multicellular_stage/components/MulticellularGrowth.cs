namespace Components
{
    using System;
    using System.Collections.Generic;
    using DefaultEcs;
    using Newtonsoft.Json;

    /// <summary>
    ///   Keeps track of multicellular growth data
    /// </summary>
    public struct MulticellularGrowth
    {
        public Dictionary<CellType, Entity>? GrownCells;

        /// <summary>
        ///   List of cells that need to be regrown after being lost in
        ///   <see cref="MulticellularGrowthHelpers.AddMulticellularGrowthCell"/>
        /// </summary>
        public List<int>? LostPartsOfBodyPlan;

        public Dictionary<Compound, float>? CompoundsNeededForNextCell;

        public Dictionary<Compound, float>? CompoundsUsedForMulticellularGrowth;

        public Dictionary<Compound, float>? TotalNeededForMulticellularGrowth;

        /// <summary>
        ///   The final cell layout this multicellular species member is growing towards
        /// </summary>
        public CellLayout<CellTemplate>? TargetCellLayout;

        /// <summary>
        ///   Once all lost body plan parts have been grown, this is the index the growing resumes at
        /// </summary>
        public int? ResumeBodyPlanAfterReplacingLost;

        /// <summary>
        ///   Used to keep track of which part of a body plan a non-first cell in a multicellular colony is.
        ///   This is required for regrowing after losing a cell.
        /// </summary>
        public int MulticellularBodyPlanPartIndex;

        public int NextBodyPlanCellToGrowIndex;

        public bool EnoughResourcesForBudding;

        public MulticellularGrowth(CellType cellType, EarlyMulticellularSpecies species)
        {
            NextBodyPlanCellToGrowIndex = -1;

            GrownCells = null;
            LostPartsOfBodyPlan = null;
            CompoundsNeededForNextCell = null;
            CompoundsUsedForMulticellularGrowth = null;
            TotalNeededForMulticellularGrowth = null;
            ResumeBodyPlanAfterReplacingLost = null;
            EnoughResourcesForBudding = false;

            MulticellularBodyPlanPartIndex = species.CellTypes.FindIndex(c => c == cellType);

            if (MulticellularBodyPlanPartIndex == -1)
            {
                MulticellularBodyPlanPartIndex = 0;

#if DEBUG
                throw new ArgumentException("Multicellular growth given invalid first cell type");
#endif
            }

            TargetCellLayout = species.Cells;

            // TODO: this needs to be recalculated if the species' properties changes
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
        public static void AddMulticellularGrowthCell(this ref MulticellularGrowth multicellularGrowth)
        {
            if (Colony == null)
            {
                MicrobeColony.CreateColonyForMicrobe(this);

                if (Colony == null)
                    throw new Exception("An issue occured during colony creation!");
            }

            var template = CastedMulticellularSpecies.Cells[nextBodyPlanCellToGrowIndex];

            var cell = CreateMulticellularColonyMemberCell(template.CellType);
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

        public static void BecomeFullyGrownMulticellularColony(this ref MulticellularGrowth multicellularGrowth)
        {
            while (!IsFullyGrownMulticellular)
            {
                AddMulticellularGrowthCell();
            }
        }

        public static void ResetMulticellularProgress(this ref MulticellularGrowth multicellularGrowth)
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

        public static void OnMulticellularColonyCellLost(this ref MulticellularGrowth multicellularGrowth, Microbe cell)
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
                var totalNeededForLostCell = CastedMulticellularSpecies.Cells[cell.MulticellularBodyPlanPartIndex]
                    .CellType
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

        public static Dictionary<Compound, float> GetCompoundsNeededForNextCell(
            this ref MulticellularGrowth multicellularGrowth)
        {
            return CastedMulticellularSpecies.Cells[IsFullyGrownMulticellular ? 0 : nextBodyPlanCellToGrowIndex]
                .CellType
                .CalculateTotalComposition();
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
}
