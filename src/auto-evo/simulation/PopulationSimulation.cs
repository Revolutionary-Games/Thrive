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

        public static void Simulate(SimulationConfiguration parameters)
        {
            var random = new Random();

            var speciesToSimulate = CopyInitialPopulationsToResults(parameters);

            while (parameters.StepsLeft > 0)
            {
                RunSimulationStep(parameters, speciesToSimulate, random);
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

            // Prepare population numbers for each patch for each of the included species
            foreach (var entry in parameters.OriginalMap.Patches)
            {
                var patch = entry.Value;

                foreach (var currentSpecies in species)
                {
                    long currentPopulation = patch.GetSpeciesPopulation(currentSpecies);

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
                                    currentPopulation = patch.GetSpeciesPopulation(parameters.ExcludedSpecies[i]);
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
                    parameters.Results.AddPopulationResultForSpecies(currentSpecies, patch, currentPopulation);
                }
            }

            return species;
        }

        private static void RunSimulationStep(SimulationConfiguration parameters, List<Species> species, Random random)
        {
            foreach (var entry in parameters.OriginalMap.Patches)
            {
                // Simulate the species in each patch taking into account the already computed populations
                SimulatePatchStep(parameters.Results, entry.Value,
                    species.Where(item => parameters.Results.GetPopulationInPatch(item, entry.Value) > 0).ToList(),
                    random);
            }
        }

        /// <summary>
        ///   The heart of the simulation that handles the processed parameters and calculates future populations.
        /// </summary>
        private static void SimulatePatchStep(RunResults populations, Patch patch, List<Species> genericSpecies,
            Random random)
        {
            _ = random;

            // Skip if there aren't any species in this patch
            if (genericSpecies.Count < 1)
                return;

            // This algorithm version is for microbe species
            var species = genericSpecies.Select(s => (MicrobeSpecies)s).ToList();

            var energyBySpecies = new Dictionary<MicrobeSpecies, float>();
            foreach (var currentSpecies in species)
            {
                energyBySpecies[currentSpecies] = 0.0f;
            }

            var niches = new List<FoodSource>
            {
                new EnvironmentalFoodSource(patch, "sunlight", Constants.AUTO_EVO_SUNLIGHT_ENERGY_AMOUNT),
                new CompoundFoodSource(patch, Glucose),
                new CompoundFoodSource(patch, HydrogenSulfide),
                new CompoundFoodSource(patch, Iron),
            };

            foreach (var currentSpecies in species)
            {
                niches.Add(new HeterotrophicFoodSource(patch, currentSpecies));
            }

            foreach (var niche in niches)
            {
                // If there isn't a source of energy here, no need for more calculations
                if (niche.TotalEnergyAvailable() <= MathUtils.EPSILON)
                {
                    continue;
                }

                var fitnessBySpecies = new Dictionary<MicrobeSpecies, float>();
                var totalNicheFitness = 0.0f;
                foreach (var currentSpecies in species)
                {
                    // Softly enforces https://en.wikipedia.org/wiki/Competitive_exclusion_principle
                    // by exaggerating fitness differences
                    var thisSpeciesFitness = Mathf.Max(Mathf.Pow(niche.FitnessScore(currentSpecies), 2.5f), 0.0f);
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
                    energyBySpecies[currentSpecies] +=
                        fitnessBySpecies[currentSpecies] * niche.TotalEnergyAvailable() / totalNicheFitness;
                }
            }

            foreach (var currentSpecies in species)
            {
                // Modify populations based on energy
                var newPopulation = (long)(energyBySpecies[currentSpecies]
                    / ProcessSystem.ComputeEnergyBalance(
                        currentSpecies.Organelles.Organelles,
                        patch.Biome, currentSpecies.MembraneType).FinalBalanceStationary);

                // Severely penalize a species that can't osmoregulate
                if (ProcessSystem.ComputeEnergyBalance(currentSpecies.Organelles,
                    patch.Biome, currentSpecies.MembraneType).FinalBalance < 0)
                {
                    newPopulation /= 10;
                }

                // Can't survive without enough population
                if (newPopulation < Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION)
                    newPopulation = 0;

                populations.AddPopulationResultForSpecies(currentSpecies, patch, newPopulation);
            }
        }
    }
}
