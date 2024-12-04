﻿namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Xoshiro.PRNG64;

/// <summary>
///   Main class for miche based population simulation.
///   This contains the algorithm for determining how much population species gain or lose
/// </summary>
public static class MichePopulation
{
    public static void Simulate(SimulationConfiguration parameters, SimulationCache? existingCache,
        Random randomSource)
    {
        if (existingCache?.MatchesSettings(parameters.WorldSettings) == false)
            throw new ArgumentException("Given cache doesn't match world settings");

        // This only seems to help a bit, so caching entirely in an auto-evo task by adding the cache parameter
        // to IRunStep.RunStep might not be worth the effort at all
        var cache = existingCache ?? new SimulationCache(parameters.WorldSettings);

        var insertWorkMemory = new Miche.InsertWorkingMemory();

        var random = new XoShiRo256starstar(randomSource.NextInt64());

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
            RunSimulationStep(parameters, speciesToSimulate, patchesList, insertWorkMemory, random, cache);
            --parameters.StepsLeft;
        }
    }

    /// <summary>
    ///   Estimates the initial population numbers
    /// </summary>
    public static int CalculateMicrobePopulationInPatch(Species species, Miche miche,
        Patch patch, SimulationCache cache)
    {
        // This assumes that only leaf nodes have energy, but the way Selection Pressures are designed
        // this is a reasonable assumption
        var leafNodes = new List<Miche>();
        miche.GetLeafNodes(leafNodes, x => x.Occupant == species);

        var individualCost = CalculateMicrobeIndividualCost(species, patch.Biome, cache);

        return (int)(leafNodes.Sum(x => x.Pressure.GetEnergy(patch)) / individualCost);
    }

    /// <summary>
    ///   Calculates the energy cost of an individual of a species. Used to calculate populations from total gained
    ///   energy per species.
    /// </summary>
    /// <returns>Cost of individual population unit of a species</returns>
    public static float CalculateMicrobeIndividualCost(Species species, BiomeConditions biomeConditions,
        SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            throw new ArgumentException("Unhandled species type passed");

        var energyBalanceInfo = cache.GetEnergyBalanceForSpecies(microbeSpecies, biomeConditions);

        return energyBalanceInfo.TotalConsumptionStationary + energyBalanceInfo.TotalMovement
            * species.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY;
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
            if (parameters.ReplacedSpecies.TryGetValue(candidateSpecies, out var replacement))
            {
                // Copy replacement species to simulate
                species.Add(replacement);

#if DEBUG
                if (replacement.ID != candidateSpecies.ID)
                {
                    GD.PrintErr("Replacement species ID should match the replaced species, otherwise bad " +
                        "things will happen");
                }
#endif
                continue;
            }

            species.Add(candidateSpecies);
        }

        // TODO: this can probably be removed soon finally
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

                // If this is a replacement species, this instead takes the
                if (currentPopulation == 0)
                {
                    Species? isExtraFor = null;

                    foreach (var tuple in parameters.ReplacedSpecies)
                    {
                        if (tuple.Value == currentSpecies)
                        {
                            isExtraFor = tuple.Key;
                            break;
                        }
                    }

                    if (isExtraFor != null)
                    {
                        currentPopulation = patch.GetSpeciesSimulationPopulation(isExtraFor);
                    }
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
        IEnumerable<KeyValuePair<int, Patch>> patchesToSimulate, Miche.InsertWorkingMemory insertWorkingMemory,
        Random random, SimulationCache cache)
    {
        foreach (var entry in patchesToSimulate)
        {
            // Simulate the species in each patch taking into account the already computed populations
            SimulatePatchStep(parameters, entry.Value,
                species.Where(s => parameters.Results.GetPopulationInPatch(s, entry.Value) > 0), insertWorkingMemory,
                random, cache);
        }
    }

    /// <summary>
    ///   The heart of the simulation that handles the processed parameters and calculates future populations.
    /// </summary>
    private static void SimulatePatchStep(SimulationConfiguration simulationConfiguration, Patch patch,
        IEnumerable<Species> genericSpecies, Miche.InsertWorkingMemory insertWorkingMemory, Random random,
        SimulationCache cache)
    {
        _ = random;

        var populations = simulationConfiguration.Results;
        bool trackEnergy = simulationConfiguration.CollectEnergyInformation;

        // Note that this modifies the miche tree while simulating
        var miche = populations.GetMicheForPatch(patch);

        var species = new HashSet<Species>();

        // TODO: switch this to something else that doesn't require a memory allocation to iterate
        foreach (var currentSpecies in genericSpecies)
        {
            if (simulationConfiguration.ReplacedSpecies.TryGetValue(currentSpecies, out var replacement))
            {
                miche.InsertSpecies(replacement, patch, null, cache, false, insertWorkingMemory);

                species.Add(replacement);
                continue;
            }

            species.Add(currentSpecies);
        }

        // Skip if there aren't any species in this patch
        if (species.Count < 1)
            return;

        var leafNodes = new List<Miche>();

        // TODO: When supporting multicellular species replace the is MicrobeSpecies with a null check
        miche.GetLeafNodes(leafNodes, x => x.Occupant is MicrobeSpecies);

        // TODO: check if energy should be calculated as doubles because the summed numbers can get pretty high that
        // might benefit from extra precision
        var energyDictionary = species.ToDictionary(x => x, _ => 0.0f);

        var currentBackTraversal = new List<Miche>();
        var scoresDictionary = new Dictionary<Species, float>();

        foreach (var node in leafNodes)
        {
            float totalScore = 0;

            scoresDictionary.Clear();
            foreach (var entry in species)
            {
                scoresDictionary[entry] = 0;
            }

            currentBackTraversal.Clear();
            node.BackTraversal(currentBackTraversal);
            foreach (var currentSpecies in species)
            {
                if (currentSpecies is not MicrobeSpecies microbeSpecies)
                    continue;

                var traversalScore = 0.0f;

                foreach (var currentMiche in currentBackTraversal)
                {
                    var rawScore = cache.GetPressureScore(currentMiche.Pressure, patch, microbeSpecies);

                    if (rawScore <= 0)
                    {
                        traversalScore = 0;
                        break;
                    }

                    // Weighted score is intentionally not used here as negatives break everything
                    var score = rawScore /
                        cache.GetPressureScore(currentMiche.Pressure, patch, (MicrobeSpecies)node.Occupant!) *
                        currentMiche.Pressure.Weight;

                    if (simulationConfiguration.WorldSettings.AutoEvoConfiguration.StrictNicheCompetition)
                        score *= score;

                    traversalScore += score;
                }

                totalScore += traversalScore;
                scoresDictionary[currentSpecies] += traversalScore;
            }

            // No need to process species if there isn't any score to give any energy
            if (totalScore <= 0)
                continue;

            foreach (var currentSpecies in species)
            {
                var micheEnergy = node.Pressure.GetEnergy(patch) * (scoresDictionary[currentSpecies] / totalScore);

                if (trackEnergy && micheEnergy > 0)
                {
                    populations.AddTrackedEnergyForSpecies(currentSpecies, patch, node.Pressure,
                        scoresDictionary[currentSpecies], totalScore, micheEnergy);
                }

                energyDictionary[currentSpecies] += micheEnergy;
            }
        }

        foreach (var currentSpecies in species)
        {
            if (currentSpecies is not MicrobeSpecies microbeSpecies)
            {
                var population = simulationConfiguration.Results.GetPopulationInPatch(currentSpecies, patch);
                populations.AddPopulationResultForSpecies(currentSpecies, patch, population);
                continue;
            }

            var individualCost = CalculateMicrobeIndividualCost(microbeSpecies, patch.Biome, cache);
            long newPopulation = (long)(energyDictionary[currentSpecies] / individualCost);

            // Remove any species that don't hold a miche
            // Probably should make this a setting and throw a multiplier here
            // It does give a large speed-up though
            bool isOccupantSomewhere = false;

            foreach (var leafNode in leafNodes)
            {
                if (leafNode.Occupant == currentSpecies)
                {
                    isOccupantSomewhere = true;
                    break;
                }
            }

            if (!isOccupantSomewhere && !currentSpecies.PlayerSpecies)
                newPopulation = 0;

            // Can't survive without enough population
            if (newPopulation < Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION)
                newPopulation = 0;

            if (trackEnergy)
            {
                populations.AddTrackedEnergyConsumptionForSpecies(currentSpecies, patch,
                    energyDictionary[currentSpecies], individualCost);
            }

            populations.AddPopulationResultForSpecies(currentSpecies, patch, newPopulation);
        }
    }
}
