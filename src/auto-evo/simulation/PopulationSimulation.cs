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
        private static readonly Compound Sunlight = SimulationParameters.Instance.GetCompound("sunlight");
        private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");
        private static readonly Compound HydrogenSulfide = SimulationParameters.Instance.GetCompound("hydrogensulfide");
        private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
        private static readonly Compound Iron = SimulationParameters.Instance.GetCompound("iron");
        private static readonly Compound Oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");

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

            var biome = patch.Biome;

            var sunlightInPatch = biome.Compounds[Sunlight].Dissolved * Constants.AUTO_EVO_SUNLIGHT_ENERGY_AMOUNT;

            var hydrogenSulfideInPatch = biome.Compounds[HydrogenSulfide].Density
                * biome.Compounds[HydrogenSulfide].Amount * Constants.AUTO_EVO_COMPOUND_ENERGY_AMOUNT;

            var glucoseInPatch = (biome.Compounds[Glucose].Density
                * biome.Compounds[Glucose].Amount
                + patch.GetTotalChunkCompoundAmount(Glucose)) * Constants.AUTO_EVO_COMPOUND_ENERGY_AMOUNT;

            var ironInPatch = patch.GetTotalChunkCompoundAmount(Iron) * Constants.AUTO_EVO_COMPOUND_ENERGY_AMOUNT;

            // Begin of new auto-evo prototype algorithm

            var speciesEnergies = new Dictionary<MicrobeSpecies, float>(species.Count);

            var totalPhotosynthesisScore = 0.0f;
            var totalChemosynthesisScore = 0.0f;
            var totalChemolithautotrophyScore = 0.0f;
            var totalGlucoseScore = 0.0f;

            var totalPredationScore = 0.0f;

            // Calculate the total scores of each type in the current patch
            foreach (var currentSpecies in species)
            {
                totalPhotosynthesisScore += GetCompoundUseScore(currentSpecies, Sunlight);
                totalChemosynthesisScore += GetCompoundUseScore(currentSpecies, HydrogenSulfide);
                totalChemolithautotrophyScore += GetCompoundUseScore(currentSpecies, Iron);
                totalGlucoseScore += GetCompoundUseScore(currentSpecies, Glucose);
                totalPredationScore += GetPredationScore(currentSpecies);
            }

            // Avoid division by 0
            totalPhotosynthesisScore = Math.Max(MathUtils.EPSILON, totalPhotosynthesisScore);
            totalChemosynthesisScore = Math.Max(MathUtils.EPSILON, totalChemosynthesisScore);
            totalChemolithautotrophyScore = Math.Max(MathUtils.EPSILON, totalChemolithautotrophyScore);
            totalGlucoseScore = Math.Max(MathUtils.EPSILON, totalGlucoseScore);
            totalPredationScore = Math.Max(MathUtils.EPSILON, totalPredationScore);

            // Calculate the share of environmental energy captured by each species
            var energyAvailableForPredation = 0.0f;

            foreach (var currentSpecies in species)
            {
                var currentSpeciesEnergy = 0.0f;

                currentSpeciesEnergy += sunlightInPatch
                    * GetCompoundUseScore(currentSpecies, Sunlight) / totalPhotosynthesisScore;

                currentSpeciesEnergy += hydrogenSulfideInPatch
                    * GetCompoundUseScore(currentSpecies, HydrogenSulfide) / totalChemosynthesisScore;

                currentSpeciesEnergy += ironInPatch
                    * GetCompoundUseScore(currentSpecies, Iron) / totalChemolithautotrophyScore;

                currentSpeciesEnergy += glucoseInPatch
                    * GetCompoundUseScore(currentSpecies, Glucose) / totalGlucoseScore;

                energyAvailableForPredation += currentSpeciesEnergy * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;
                speciesEnergies.Add(currentSpecies, currentSpeciesEnergy);
            }

            // Calculate the share of predation done by each species
            // Then update populations
            foreach (var currentSpecies in species)
            {
                speciesEnergies[currentSpecies] += energyAvailableForPredation
                    * GetPredationScore(currentSpecies) / totalPredationScore;
                speciesEnergies[currentSpecies] -= energyAvailableForPredation / species.Count;

                var newPopulation = (long)(speciesEnergies[currentSpecies]
                    / Math.Pow(currentSpecies.Organelles.Count, 1.3f));

                // Can't survive without enough population
                if (newPopulation < Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION)
                    newPopulation = 0;

                populations.AddPopulationResultForSpecies(currentSpecies, patch, newPopulation);
            }
        }

        private static float GetPredationScore(MicrobeSpecies species)
        {
            var predationScore = 0.0f;

            foreach (var organelle in species.Organelles)
            {
                if (organelle.Definition.HasComponentFactory<PilusComponentFactory>())
                {
                    predationScore += Constants.AUTO_EVO_PILUS_PREDATION_SCORE;
                    continue;
                }

                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    if (process.Process.Outputs.ContainsKey(Oxytoxy))
                    {
                        predationScore += Constants.AUTO_EVO_TOXIN_PREDATION_SCORE;
                    }
                }
            }

            return predationScore;
        }

        private static float GetCompoundUseScore(MicrobeSpecies species, Compound compound)
        {
            var compoundUseScore = 0.0f;

            foreach (var organelle in species.Organelles)
            {
                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    if (process.Process.Inputs.ContainsKey(compound))
                    {
                        if (process.Process.Outputs.ContainsKey(Glucose))
                        {
                            compoundUseScore += process.Process.Outputs[Glucose]
                                / process.Process.Inputs[compound] / Constants.AUTO_EVO_GLUCOSE_USE_SCORE_DIVISOR;
                        }

                        if (process.Process.Outputs.ContainsKey(ATP))
                        {
                            compoundUseScore += process.Process.Outputs[ATP]
                                / process.Process.Inputs[compound] / Constants.AUTO_EVO_ATP_USE_SCORE_DIVISOR;
                        }
                    }
                }
            }

            return compoundUseScore;
        }
    }
}
