namespace Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DefaultEcs;
    using Newtonsoft.Json;

    /// <summary>
    ///   Keeps track of multicellular growth data
    /// </summary>
    public struct MulticellularGrowth
    {
        // TODO: remove if this doesn't end up a useful variable
        // public Dictionary<CellType, Entity>? GrownCells;

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

        // TODO: switch this to non-nullable
        /// <summary>
        ///   Once all lost body plan parts have been grown, this is the index the growing resumes at
        /// </summary>
        public int? ResumeBodyPlanAfterReplacingLost;

        // TODO: MulticellularBodyPlanPartIndex used to be here, now it is in EarlyMulticellularSpeciesMember
        // which means that a new system is needed to create MulticellularGrowth components on ejected cells that
        // should be allowed to resume growing

        public int NextBodyPlanCellToGrowIndex;

        public bool EnoughResourcesForBudding;

        public MulticellularGrowth(EarlyMulticellularSpecies species)
        {
            NextBodyPlanCellToGrowIndex = -1;

            LostPartsOfBodyPlan = null;
            CompoundsNeededForNextCell = null;
            CompoundsUsedForMulticellularGrowth = null;
            TotalNeededForMulticellularGrowth = null;
            ResumeBodyPlanAfterReplacingLost = null;
            EnoughResourcesForBudding = false;

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
            throw new NotImplementedException();

            // Commented out code
            // ReSharper disable once CommentTypo
            /*if (Colony == null)
            {
                MicrobeColony.CreateColonyForMicrobe(this);

                if (Colony == null)
                    throw new Exception("An issue occured during colony creation!");
            }

            var template = CastedMulticellularSpecies.Cells[nextBodyPlanCellToGrowIndex];

            var cell = CreateMulticellularColonyMemberCell(template.CellType);
            cell.MulticellularBodyPlanPartIndex = multicellularGrowth.NextBodyPlanCellToGrowIndex;

            // We don't reset our state here in case we want to be in engulf mode
            // TODO: grab this from the colony
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

            ++multicellularGrowth.NextBodyPlanCellToGrowIndex;
            multicellularGrowth.CompoundsNeededForNextCell = null;*/
        }

        public static void BecomeFullyGrownMulticellularColony(this ref MulticellularGrowth multicellularGrowth)
        {
            while (!multicellularGrowth.IsFullyGrownMulticellular)
            {
                multicellularGrowth.AddMulticellularGrowthCell();
            }
        }

        public static void ResetMulticellularProgress(this ref MulticellularGrowth multicellularGrowth,
            in Entity entity, IWorldSimulation worldSimulation)
        {
            // Clear variables

            // The first cell is the last to duplicate (budding reproduction) so the body plan starts filling at index 1
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
            var species = colonyEntity.Get<EarlyMulticellularSpeciesMember>().Species;

            var lostPartIndex = lostCell.Get<EarlyMulticellularSpeciesMember>().MulticellularBodyPlanPartIndex;

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

            var usedForProgress = new Dictionary<Compound, float>();

            if (multicellularGrowth.CompoundsNeededForNextCell != null)
            {
                var totalNeededForCurrentlyGrowingCell = multicellularGrowth.GetCompoundsNeededForNextCell(species);

                foreach (var entry in totalNeededForCurrentlyGrowingCell)
                {
                    var compound = entry.Key;
                    var neededAmount = entry.Value;

                    if (multicellularGrowth.CompoundsNeededForNextCell!.TryGetValue(compound, out var left))
                    {
                        var alreadyUsed = neededAmount - left;

                        if (alreadyUsed > 0)
                            usedForProgress.Add(compound, alreadyUsed);
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
                if (entry.Value > MathUtils.EPSILON)
                    compoundRefundLocation.AddCompound(entry.Key, entry.Value);
            }

            // Adjust the already used compound amount to lose the progress we made for the current cell and also
            // towards the lost cell, this we the total progress bar should be correct
            if (multicellularGrowth.CompoundsUsedForMulticellularGrowth != null)
            {
                var totalNeededForLostCell = species.Cells[lostPartIndex]
                    .CellType
                    .CalculateTotalComposition();

                foreach (var compound in multicellularGrowth.CompoundsUsedForMulticellularGrowth.Keys.ToArray())
                {
                    var totalUsed = multicellularGrowth.CompoundsUsedForMulticellularGrowth[compound];

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

                    multicellularGrowth.CompoundsUsedForMulticellularGrowth[compound] = totalUsed;
                }
            }
        }

        public static Dictionary<Compound, float> GetCompoundsNeededForNextCell(
            this ref MulticellularGrowth multicellularGrowth, EarlyMulticellularSpecies species)
        {
            return species
                .Cells[
                    multicellularGrowth.IsFullyGrownMulticellular ? 0 : multicellularGrowth.NextBodyPlanCellToGrowIndex]
                .CellType.CalculateTotalComposition();
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
