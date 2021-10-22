namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///   Attempts to increase the biodiversity of a patch by force-splitting an existing species there or creating a
    ///   new offshoot species from a
    /// </summary>
    public class IncreaseBiodiversity : IRunStep
    {
        private readonly PatchMap map;
        private readonly Patch patch;
        private readonly AutoEvoConfiguration configuration;
        private readonly Random random;

        private readonly Mutations mutations = new Mutations();

        private bool tryCurrentPatch = true;
        private bool createdASpecies;

        public IncreaseBiodiversity(PatchMap map, Patch patch, Random random, AutoEvoConfiguration configuration)
        {
            this.map = map;
            this.patch = patch;
            this.configuration = configuration;
            this.random = new Random(random.Next());
        }

        public int TotalSteps => 2;
        public bool CanRunConcurrently => true;

        public bool RunStep(RunResults results)
        {
            if (tryCurrentPatch)
            {
                CheckCurrentPatchSpecies(results);

                tryCurrentPatch = false;
                return false;
            }

            if (createdASpecies)
                return true;

            CheckNeighbourPatchSpecies(results);

            return true;
        }

        private void CheckCurrentPatchSpecies(RunResults results)
        {
            foreach (var candidateSpecies in patch.SpeciesInPatch)
            {
                if (candidateSpecies.Value < configuration.NewBiodiversityIncreasingSpeciesPopulation)
                    continue;

                var found = TryBiodiversitySplit(candidateSpecies.Key, true);

                if (found == null)
                    continue;

                OnSpeciesCreated(found, candidateSpecies.Key, results);

                if (configuration.UseBiodiversityForceSplit)
                {
                    // TODO: implement this
                    throw new NotImplementedException(
                        "Marking biodiversity increase as split is not implemented");
                }

                break;
            }
        }

        private void CheckNeighbourPatchSpecies(RunResults results)
        {
            var alreadyCheckedSpecies = new HashSet<Species>(patch.SpeciesInPatch.Select(p => p.Key));

            foreach (var neighbour in patch.Adjacent)
            {
                foreach (var candidateSpecies in neighbour.SpeciesInPatch)
                {
                    if (candidateSpecies.Value < configuration.NewBiodiversityIncreasingSpeciesPopulation ||
                        alreadyCheckedSpecies.Contains(candidateSpecies.Key))
                        continue;

                    if (random.NextDouble() > configuration.BiodiversityFromNeighbourPatchChance)
                        continue;

                    alreadyCheckedSpecies.Add(candidateSpecies.Key);

                    var found = TryBiodiversitySplit(candidateSpecies.Key, false);

                    if (found == null)
                        continue;

                    OnSpeciesCreated(found, candidateSpecies.Key, results);

                    if (!configuration.BiodiversityNearbyPatchIsFreePopulation)
                    {
                        // TODO: implement this
                        throw new NotImplementedException(
                            "adding population penalty to neighbour patch is not implemented");
                    }

                    break;
                }
            }
        }

        private MicrobeSpecies TryBiodiversitySplit(Species splitFrom, bool inCurrentPatch)
        {
            var config = new SimulationConfiguration(map, Constants.AUTO_EVO_VARIANT_SIMULATION_STEPS);

            var split = (MicrobeSpecies)splitFrom.Clone();

            if (configuration.BiodiversitySplitIsMutated)
                mutations.CreateMutatedSpecies((MicrobeSpecies)splitFrom, split);

            // Set the starting population in the patch
            split.Population = configuration.NewBiodiversityIncreasingSpeciesPopulation;

            config.ExtraSpecies.Add(split);
            config.PatchesToRun.Add(patch);

            if (inCurrentPatch)
            {
                // TODO: should we apply the population reduction to splitFrom?
            }

            PopulationSimulation.Simulate(config);

            var population = config.Results.GetPopulationInPatch(split, patch);

            if (population < configuration.NewBiodiversityIncreasingSpeciesPopulation)
                return null;

            // TODO: could compare the original species population here to determine if this change is beneficial to
            // it as well (in which case a non-force species split could be done)

            // Successfully found a species to create in order to increase biodiversity
            return split;
        }

        private void OnSpeciesCreated(Species species, Species fromSpecies, RunResults results)
        {
            results.AddNewSpecies(species,
                new[]
                {
                    new KeyValuePair<Patch, long>(patch, configuration.NewBiodiversityIncreasingSpeciesPopulation),
                },
                RunResults.NewSpeciesType.FillNiche, fromSpecies);

            createdASpecies = true;
        }
    }
}
