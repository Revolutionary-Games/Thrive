namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///   Step that finds the best mutation for a single species
    /// </summary>
    public class FindBestMutation : VariantTryingStep
    {
        private readonly IAutoEvoConfiguration configuration;
        private readonly WorldGenerationSettings worldSettings;
        private readonly PatchMap map;
        private readonly Species species;
        private readonly Random random;
        private readonly float splitThresholdFraction;
        private readonly int splitThresholdAmount;
        private readonly SimulationCache cache;

        private readonly Mutations mutations = new();

        public FindBestMutation(IAutoEvoConfiguration configuration,
            WorldGenerationSettings worldSettings, PatchMap map, Species species, Random random,
            int mutationsToTry, bool allowNoMutation,
            float splitThresholdFraction, int splitThresholdAmount)
            : base(mutationsToTry, allowNoMutation, splitThresholdAmount > 0)
        {
            this.configuration = configuration;
            this.worldSettings = worldSettings;
            this.map = map;
            this.species = species;
            this.random = new Random(random.Next());
            this.splitThresholdFraction = splitThresholdFraction;
            this.splitThresholdAmount = splitThresholdAmount;
            cache = new SimulationCache(worldSettings);
        }

        public override bool CanRunConcurrently => true;

        protected override void OnBestResultFound(RunResults results, IAttemptResult bestVariant)
        {
            var best = (AttemptResult)bestVariant;
            results.AddMutationResultForSpecies(species, best.Mutation);

            if (splitThresholdAmount > 0)
            {
                HandleSpeciesSplit(results, best, GetSecondBest());
            }
        }

        protected override IAttemptResult TryCurrentVariant()
        {
            var config = new SimulationConfiguration(configuration, map, worldSettings,
                Constants.AUTO_EVO_VARIANT_SIMULATION_STEPS);

            config.SetPatchesToRunBySpeciesPresence(species);

            PopulationSimulation.Simulate(config, cache, random);

            return new AttemptResult(null, config.Results.GetPopulationInPatches(species));
        }

        protected override IAttemptResult TryVariant()
        {
            var mutated = (MicrobeSpecies)species.Clone();
            mutations.CreateMutatedSpecies((MicrobeSpecies)species, mutated,
                worldSettings.AIMutationMultiplier, worldSettings.LAWK);

            var config = new SimulationConfiguration(configuration, map, worldSettings,
                Constants.AUTO_EVO_VARIANT_SIMULATION_STEPS);

            config.SetPatchesToRunBySpeciesPresence(species);
            config.ExcludedSpecies.Add(species);
            config.ExtraSpecies.Add(mutated);

            PopulationSimulation.Simulate(config, cache, random);

            return new AttemptResult(mutated, config.Results.GetPopulationInPatches(mutated));
        }

        /// <summary>
        ///   Check if second best mutation was good enough to split the species
        /// </summary>
        /// <param name="results">Where to store the split result if successful</param>
        /// <param name="best">The best found run</param>
        /// <param name="secondBest">The second best run</param>
        private void HandleSpeciesSplit(RunResults results, AttemptResult best, IAttemptResult? secondBest)
        {
            if (secondBest == null)
                return;

            // Don't split if the overall population is low
            if (best.Score < Constants.AUTO_EVO_MINIMUM_SPECIES_SIZE_BEFORE_SPLIT)
                return;

            // Or species is in just one patch
            if (best.PatchScores.Count < 2)
                return;

            // TODO: should this check be actually done? Would it be better if we just look for patches where
            // secondBest did well compared to the best?

            // Only split if the score is good enough
            if (secondBest.Score + splitThresholdAmount < best.Score)
                return;

            if (secondBest.Score * (1 + splitThresholdFraction) < best.Score)
                return;

            var data = (AttemptResult)secondBest;

            // If second best was the no mutation case, then don't split on that
            // Warning disabled because this is a tweak variable
#pragma warning disable 162

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse HeuristicUnreachableCode
            if (data.Mutation == null && !Constants.AUTO_EVO_ALLOW_SPECIES_SPLIT_ON_NO_MUTATION)
                return;
#pragma warning restore 162

            // Find patches where the second best did better to switch over to the new species
            var bestBetter = new List<Patch>();
            var secondBetter = new List<Patch>();

            // Used to track in which patch the best and second best where the closest
            (Patch? Patch, long Difference) closestMatch = (null, 0);

            foreach (var patch in best.PatchScores.Select(p => p.Key).Concat(data.PatchScores.Select(p => p.Key))
                         .Distinct())
            {
                if (!best.PatchScores.TryGetValue(patch, out long bestScore))
                    bestScore = 0;

                if (!data.PatchScores.TryGetValue(patch, out long secondScore))
                    secondScore = 0;

                if (closestMatch.Patch == null || closestMatch.Difference < secondScore - bestScore)
                {
                    closestMatch = (patch, secondScore - bestScore);
                }

                if (secondScore >= bestScore)
                {
                    secondBetter.Add(patch);
                }
                else
                {
                    bestBetter.Add(patch);
                }
            }

            if (secondBetter.Count < 1)
            {
                if (closestMatch.Patch == null)
                    return;

                secondBetter.Add(closestMatch.Patch);
                if (!bestBetter.Remove(closestMatch.Patch))
                    throw new Exception("Couldn't remove a list item that should have been there");
            }

            // Don't allow entirely convert, otherwise we have a logic error in our best finding
            // This doesn't throw as if the results are the same (or we later add a threshold to favour second best)
            // we may end up in this situation
            if (bestBetter.Count < 1)
            {
                if (secondBetter.Count > 1)
                {
                    bestBetter.Add(secondBetter[0]);
                    if (!secondBetter.Remove(secondBetter[0]))
                        throw new Exception("Couldn't remove a list item that should have been there");
                }
                else
                {
                    return;
                }
            }

            if (data.Mutation == null)
            {
                if (best.Mutation == null)
                    throw new Exception($"Logic error in {nameof(FindBestMigration)} fallback best mutation is null");

                // Original species wants to split off
                // So flip this around to make the mutated copy split off
                results.AddMutationResultForSpecies(species, null);
                results.AddSplitResultForSpecies(species, best.Mutation, bestBetter);
            }
            else
            {
                results.AddSplitResultForSpecies(species, data.Mutation, secondBetter);
            }
        }

        private class AttemptResult : IAttemptResult
        {
            public AttemptResult(Species? mutation, IEnumerable<KeyValuePair<Patch, long>> patchScores)
            {
                Mutation = mutation;
                PatchScores = patchScores.ToDictionary(p => p.Key, p => p.Value);
                Score = PatchScores.Sum(p => p.Value);
            }

            public Species? Mutation { get; }
            public long Score { get; }

            public Dictionary<Patch, long> PatchScores { get; }
        }
    }
}
