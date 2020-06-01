namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///   Main class for the population simulation part.
    ///   This contains the algorithm for determining how much population species gain or lose
    /// </summary>
    public static class PopulationSimulation
    {
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
                    int currentPopulation = patch.GetSpeciesPopulation(currentSpecies);

                    // If this is an extra species, this first takes the
                    // population from extra species that match its index, if that
                    // doesn't exist then the population number is used
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
<<<<<<< HEAD
                    species.Where(item => parameters.Results.GetPopulationInPatch(item, entry.Value) > 0).ToList(),
=======
                    species.Where((item) => parameters.Results.GetPopulationInPatch(item, entry.Value) > 0).ToList(),
>>>>>>> Readd random parameter, write floats as 0.0f, remove high and low species boosts.
                    random);
            }
        }

        /// <summary>
        ///   The heart of the simulation that handles the processed parameters and calculates future populations.
        /// </summary>
        private static void SimulatePatchStep(RunResults populations, Patch patch, List<Species> species, Random random)
        {
            _ = random;

            // Skip if there aren't any species in this patch
            if (species.Count < 1)
                return;

            var biome = patch.Biome;

            var sunlightInPatch = biome.Compounds["sunlight"].Dissolved * 100000;
            var hydrogenSulfideInPatch = biome.Compounds["hydrogensulfide"].Density
                * biome.Compounds["hydrogensulfide"].Amount * 1000;

            // TODO: this is where the proper auto-evo algorithm goes

            var speciesEnergies = new Dictionary<MicrobeSpecies, float>(species.Count);

            var totalPhotosynthesisScore = 0.0f;
            var totalChemosynthesisScore = 0.0f;
            var totalPredationScore = 0.0f;

            // Calculate the total scores of each type in the current patch
            foreach (MicrobeSpecies currentMicrobeSpecies in species)
            {
                totalPhotosynthesisScore += GetPhotosynthesisScore(currentMicrobeSpecies);
                totalChemosynthesisScore += GetChemosynthesisScore(currentMicrobeSpecies);
                totalPredationScore += GetPredationScore(currentMicrobeSpecies);
            }

            // Avoid division by 0
            totalPhotosynthesisScore = Math.Max(MathUtils.EPSILON, totalPhotosynthesisScore);
            totalChemosynthesisScore = Math.Max(MathUtils.EPSILON, totalChemosynthesisScore);
            totalPredationScore = Math.Max(MathUtils.EPSILON, totalPredationScore);

            // Calculate the share of environmental energy captured by each species
            var energyAvailableForPredation = 0.0f;

            foreach (MicrobeSpecies currentMicrobeSpecies in species)
            {
                var currentSpeciesEnergy = 0.0f;

                currentSpeciesEnergy += sunlightInPatch
                    * GetPhotosynthesisScore(currentMicrobeSpecies) / totalPhotosynthesisScore;
                
                currentSpeciesEnergy += hydrogenSulfideInPatch
                    * GetChemosynthesisScore(currentMicrobeSpecies) / totalChemosynthesisScore;

                energyAvailableForPredation += 0.5f * currentSpeciesEnergy;
                speciesEnergies.Add(currentMicrobeSpecies, currentSpeciesEnergy);
            }

            // Calculate the share of predation done by each species
            // Then update populations
            foreach (MicrobeSpecies currentMicrobeSpecies in species)
            {
                speciesEnergies[currentMicrobeSpecies] += energyAvailableForPredation
                    * GetPredationScore(currentMicrobeSpecies) / totalPredationScore;

                var newPopulation = (int)(speciesEnergies[currentMicrobeSpecies]
                    / Math.Pow(currentMicrobeSpecies.Organelles.Count(), 1.3f));

                populations.AddPopulationResultForSpecies(currentMicrobeSpecies, patch, newPopulation);
            }
        }

        private static float GetPhotosynthesisScore(MicrobeSpecies species)
        {
            var photosynthesisScore = 0.0f;

            var sunlight = SimulationParameters.Instance.GetCompound("sunlight");
            var glucose = SimulationParameters.Instance.GetCompound("glucose");

            foreach (var organelle in species.Organelles)
            {
                //get photosynthesis process here

                var processesDoneByOrganelle = organelle.Definition.RunnableProcesses;

                foreach (var process in processesDoneByOrganelle)
                {
                    if (process.Process.Inputs.ContainsKey(sunlight.InternalName) && process.Process.Outputs.ContainsKey(glucose.InternalName))
                    {
                        photosynthesisScore += process.Process.Outputs[glucose.InternalName];
                    }
                } 
            }

            return photosynthesisScore;
        }

        private static float GetPredationScore(MicrobeSpecies species)
        {
            var predationScore = 0.0f;
            foreach (var organelle in species.Organelles)
            {
                if (organelle.Definition.HasComponentFactory<PilusComponentFactory>())
                {
                    predationScore += 1;
                }
            }

            return predationScore;
        }

        private static float GetChemosynthesisScore(MicrobeSpecies species)
        {
            var chemosynthesisScore = 0.0f;

            var hydrogenSulfide = SimulationParameters.Instance.GetCompound("hydrogensulfide");
            var glucose = SimulationParameters.Instance.GetCompound("glucose");

            foreach (var organelle in species.Organelles)
            {
                //get chemosynthesis process here
                var processesDoneByOrganelle = organelle.Definition.RunnableProcesses;

                foreach (var process in processesDoneByOrganelle)
                {
                    if (process.Process.Inputs.ContainsKey(hydrogenSulfide.InternalName) && process.Process.Outputs.ContainsKey(glucose.InternalName))
                    {
                        chemosynthesisScore += process.Process.Outputs[glucose.InternalName];
                    }
                }
            }

            return chemosynthesisScore;
        }
    }
}
