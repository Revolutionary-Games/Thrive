namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Handles growth in multicellular cell colonies
    /// </summary>
    [With(typeof(EarlyMulticellularSpeciesMember))]
    [With(typeof(MulticellularGrowth))]
    public sealed class MulticellularGrowthSystem : AEntitySetSystem<float>
    {
        public MulticellularGrowthSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            // TODO: implement

            // TODO: when spawning a new cell to add to colony it needs to be ensured that its membrane is ready before
            // attach to calculate the attach position
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
    }
}
