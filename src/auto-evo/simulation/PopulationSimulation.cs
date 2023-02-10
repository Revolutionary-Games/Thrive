namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;

    /// <summary>
    ///   Main class for the population simulation part.
    ///   This contains the algorithm for determining how much population species gain or lose
    /// </summary>
    public static class PopulationSimulation
    {
        private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");
        private static readonly Compound HydrogenSulfide = SimulationParameters.Instance.GetCompound("hydrogensulfide");
        private static readonly Compound Iron = SimulationParameters.Instance.GetCompound("iron");
        private static readonly Compound Sunlight = SimulationParameters.Instance.GetCompound("sunlight");
        private static readonly Compound Temperature = SimulationParameters.Instance.GetCompound("temperature");

        public static void Simulate(SimulationConfiguration parameters, SimulationCache? existingCache,
            Random randomSource)
        {
            if (existingCache?.MatchesSettings(parameters.WorldSettings) == false)
                throw new ArgumentException("Given cache doesn't match world settings");

            // This only seems to help a bit, so caching entirely in an auto-evo task by adding the cache parameter
            // to IRunStep.RunStep might not be worth the effort at all
            var cache = existingCache ?? new SimulationCache(parameters.WorldSettings);

            var random = new Random(randomSource.Next());

            var speciesToSimulate = CopyInitialPopulationsToResults(parameters);

            IEnumerable<KeyValuePair<int, Patch>> patchesToSimulate = parameters.OriginalMap.Patches;

            // Skip patches not configured to be simulated in order to run faster
            if (parameters.PatchesToRun.Count > 0)
            {
                patchesToSimulate = patchesToSimulate.Where(p => parameters.PatchesToRun.Contains(p.Value));
            }

            var patchesList = patchesToSimulate.ToList();

            while (parameters.StepsLeft > 0)
            {
                RunSimulationStep(parameters, speciesToSimulate, patchesList, random, cache,
                    parameters.AutoEvoConfiguration, parameters.WorldSettings);
                --parameters.StepsLeft;
            }
        }

        /// <summary>
        ///   Populates the initial population numbers taking config into account
        /// </summary>
        private static List<Species> CopyInitialPopulationsToResults(SimulationConfiguration parameters)
        {
            var species = new List<Species>();

            // Copy non excluded species
            foreach (var candidateSpecies in parameters.OriginalMap.FindAllSpeciesWithPopulation())
            {
                if (parameters.ExcludedSpecies.Contains(candidateSpecies))
                    continue;

                species.Add(candidateSpecies);
            }

            // Copy extra species
            species.AddRange(parameters.ExtraSpecies);

            foreach (var entry in species)
            {
                // Trying to find where a null comes from https://github.com/Revolutionary-Games/Thrive/issues/3004
                if (entry == null)
                    throw new Exception("Species in a simulation run is null");
            }

            // Prepare population numbers for each patch for each of the included species
            var patches = parameters.OriginalMap.Patches.Values;

            var results = parameters.Results;

            foreach (var currentSpecies in species)
            {
                var currentResult = results.GetSpeciesResultForInternalUse(currentSpecies);

                foreach (var patch in patches)
                {
                    long currentPopulation = patch.GetSpeciesSimulationPopulation(currentSpecies);

                    // If this is an extra species, this first takes the
                    // population from excluded species that match its index, if that
                    // doesn't exist then the global population number (from Species) is used
                    if (currentPopulation == 0 && parameters.ExtraSpecies.Contains(currentSpecies))
                    {
                        bool useGlobal = true;

                        for (int i = 0; i < parameters.ExtraSpecies.Count; ++i)
                        {
                            if (parameters.ExtraSpecies[i] == currentSpecies)
                            {
                                if (parameters.ExcludedSpecies.Count > i)
                                {
                                    currentPopulation =
                                        patch.GetSpeciesSimulationPopulation(parameters.ExcludedSpecies[i]);
                                    useGlobal = false;
                                }

                                break;
                            }
                        }

                        if (useGlobal)
                            currentPopulation = currentSpecies.Population;
                    }

                    // Apply migrations
                    foreach (var migration in parameters.Migrations)
                    {
                        if (migration.Item1 == currentSpecies)
                        {
                            if (migration.Item2.From == patch)
                            {
                                currentPopulation -= migration.Item2.Population;
                            }
                            else if (migration.Item2.To == patch)
                            {
                                currentPopulation += migration.Item2.Population;
                            }
                        }
                    }

                    // All species even ones not in a patch need to have their population numbers added
                    // as the simulation expects to be able to get the populations
                    currentResult.NewPopulationInPatches[patch] = currentPopulation;
                }
            }

            return species;
        }

        private static void RunSimulationStep(SimulationConfiguration parameters, List<Species> species,
            IEnumerable<KeyValuePair<int, Patch>> patchesToSimulate, Random random, SimulationCache cache,
            IAutoEvoConfiguration autoEvoConfiguration, WorldGenerationSettings worldSettings)
        {
            foreach (var entry in patchesToSimulate)
            {
                // Simulate the species in each patch taking into account the already computed populations
                SimulatePatchStep(parameters, entry.Value,
                    species.Where(item => parameters.Results.GetPopulationInPatch(item, entry.Value) > 0),
                    random, cache, autoEvoConfiguration, worldSettings);
            }
        }

        /// <summary>
        ///   The heart of the simulation that handles the processed parameters and calculates future populations.
        /// </summary>
        private static void SimulatePatchStep(SimulationConfiguration simulationConfiguration, Patch patch,
            IEnumerable<Species> genericSpecies, Random random, SimulationCache cache,
            IAutoEvoConfiguration autoEvoConfiguration, WorldGenerationSettings worldSettings)
        {
            _ = random;

            var populations = simulationConfiguration.Results;
            bool trackEnergy = simulationConfiguration.CollectEnergyInformation;

            // This algorithm version is for microbe species
            // TODO: add simulation for multicellular
            var species = genericSpecies.Select(s => s as MicrobeSpecies).Where(s => s != null).Select(s => s!)
                .ToList();

            // Skip if there aren't any species in this patch
            if (species.Count < 1)
                return;

            var energyBySpecies = new Dictionary<MicrobeSpecies, float>();
            foreach (var currentSpecies in species)
            {
                energyBySpecies[currentSpecies] = 0.0f;
            }

            bool strictCompetition = autoEvoConfiguration.StrictNicheCompetition;

            var niches = new List<FoodSource>
            {
                new EnvironmentalFoodSource(patch, Sunlight, Constants.AUTO_EVO_SUNLIGHT_ENERGY_AMOUNT),
                new EnvironmentalFoodSource(patch, Temperature, Constants.AUTO_EVO_THERMOSYNTHESIS_ENERGY_AMOUNT),
                new CompoundFoodSource(patch, Glucose),
                new CompoundFoodSource(patch, HydrogenSulfide),
                new CompoundFoodSource(patch, Iron),
                new ChunkFoodSource(patch, "marineSnow"),
                new ChunkFoodSource(patch, "ironSmallChunk"),
                new ChunkFoodSource(patch, "ironBigChunk"),
            };

            foreach (var currentSpecies in species)
            {
                niches.Add(new HeterotrophicFoodSource(patch, currentSpecies, cache));
            }

            foreach (var niche in niches)
            {
                // If there isn't a source of energy here, no need for more calculations
                if (niche.TotalEnergyAvailable() <= MathUtils.EPSILON)
                    continue;

                var fitnessBySpecies = new Dictionary<MicrobeSpecies, float>();
                var totalNicheFitness = 0.0f;
                foreach (var currentSpecies in species)
                {
                    float thisSpeciesFitness;

                    if (strictCompetition)
                    {
                        // Softly enforces https://en.wikipedia.org/wiki/Competitive_exclusion_principle
                        // by exaggerating fitness differences
                        thisSpeciesFitness =
                            Mathf.Max(Mathf.Pow(niche.FitnessScore(
                                currentSpecies, cache, worldSettings), 2.5f), 0.0f);
                    }
                    else
                    {
                        thisSpeciesFitness = Mathf.Max(
                            niche.FitnessScore(currentSpecies, cache, worldSettings), 0.0f);
                    }

                    fitnessBySpecies[currentSpecies] = thisSpeciesFitness;
                    totalNicheFitness += thisSpeciesFitness;
                }

                // If no species can get energy this way, no need for more calculations
                if (totalNicheFitness <= MathUtils.EPSILON)
                {
                    continue;
                }

                foreach (var currentSpecies in species)
                {
                    var energy = fitnessBySpecies[currentSpecies] * niche.TotalEnergyAvailable() / totalNicheFitness;

                    // If this species can't gain energy here, don't count it (this also prevents it from appearing
                    // in food sources (if that's not what we want), if the species doesn't use this food source
                    if (energy <= MathUtils.EPSILON)
                        continue;

                    energyBySpecies[currentSpecies] += energy;

                    if (trackEnergy)
                    {
                        populations.AddTrackedEnergyForSpecies(currentSpecies, patch, niche,
                            fitnessBySpecies[currentSpecies], energy, totalNicheFitness);
                    }
                }
            }

            foreach (var currentSpecies in species)
            {
                var energyBalanceInfo = cache.GetEnergyBalanceForSpecies(currentSpecies, patch.Biome);
                var individualCost = energyBalanceInfo.TotalConsumptionStationary + energyBalanceInfo.TotalMovement
                    * currentSpecies.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY;

                // Modify populations based on energy
                var newPopulation = (long)(energyBySpecies[currentSpecies]
                    / individualCost);

                if (trackEnergy)
                {
                    populations.AddTrackedEnergyConsumptionForSpecies(currentSpecies, patch, newPopulation,
                        energyBySpecies[currentSpecies], individualCost);
                }

                // TODO: this is a hack for now to make the player experience better, try to get the same rules working
                // for the player and AI species in the future.
                if (currentSpecies.PlayerSpecies)
                {
                    // Severely penalize a species that can't osmoregulate
                    if (energyBalanceInfo.FinalBalanceStationary < 0)
                    {
                        newPopulation /= 10;
                    }
                }
                else
                {
                    // Severely penalize a species that can't move indefinitely
                    if (energyBalanceInfo.FinalBalance < 0)
                    {
                        newPopulation /= 10;
                    }
                }

                // Can't survive without enough population
                if (newPopulation < Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION)
                    newPopulation = 0;

                populations.AddPopulationResultForSpecies(currentSpecies, patch, newPopulation);
            }
        }
    }
}
