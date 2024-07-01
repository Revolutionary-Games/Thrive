﻿namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
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
            RunSimulationStep(parameters, speciesToSimulate, patchesList, random, cache);
            --parameters.StepsLeft;
        }
    }

    public static int CalculateMicrobePopulationInPatch(MicrobeSpecies species, Miche miche,
        BiomeConditions biomeConditions, SimulationCache cache)
    {
        var energyBalanceInfo = cache.GetEnergyBalanceForSpecies(species, biomeConditions);
        var individualCost = energyBalanceInfo.TotalConsumptionStationary + energyBalanceInfo.TotalMovement
            * species.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY;

        var leafNodes = miche.GetLeafNodes();

        var miches = leafNodes.Where(x => x.Occupant == species).Select(x => x.BackTraversal())
            .SelectMany(x => x).Distinct().ToList();

        float totalEnergy = 0;

        foreach (var subMiche in miches)
        {
            var micheEnergy = subMiche.Pressure.GetEnergy();

            if (micheEnergy <= 0)
                continue;

            totalEnergy += micheEnergy;
        }

        return (int)(totalEnergy / individualCost);
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
        IEnumerable<KeyValuePair<int, Patch>> patchesToSimulate, Random random, SimulationCache cache)
    {
        foreach (var entry in patchesToSimulate)
        {
            // Simulate the species in each patch taking into account the already computed populations
            SimulatePatchStep(parameters, entry.Value,
                species.Where(s => parameters.Results.GetPopulationInPatch(s, entry.Value) > 0), random, cache);
        }
    }

    /// <summary>
    ///   The heart of the simulation that handles the processed parameters and calculates future populations.
    /// </summary>
    private static void SimulatePatchStep(SimulationConfiguration simulationConfiguration, Patch patch,
        IEnumerable<Species> genericSpecies, Random random, SimulationCache cache)
    {
        _ = random;

        var populations = simulationConfiguration.Results;
        bool trackEnergy = simulationConfiguration.CollectEnergyInformation;

        populations.MicheByPatch.TryGetValue(patch, out var miche);

        foreach (var s in simulationConfiguration.ExtraSpecies)
            miche!.InsertSpecies((MicrobeSpecies)s, cache);

        var species = genericSpecies.ToList();

        // Skip if there aren't any species in this patch
        if (species.Count < 1)
            return;

        var leafNodes = miche!.GetLeafNodes().Where(x => x.Occupant != null).ToList();

        foreach (var currentSpecies in species)
        {
            var energyBalanceInfo = cache.GetEnergyBalanceForSpecies((MicrobeSpecies)currentSpecies, patch.Biome);
            var individualCost = energyBalanceInfo.TotalConsumptionStationary + energyBalanceInfo.TotalMovement
                * currentSpecies.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY;

            var miches = leafNodes.Where(x => x.Occupant == currentSpecies).Select(x => x.BackTraversal())
                .SelectMany(x => x).Distinct().ToList();

            float totalEnergy = 0;

            foreach (var subMiche in miches)
            {
                var micheEnergy = subMiche.Pressure.GetEnergy();

                if (micheEnergy <= 0)
                    continue;

                totalEnergy += micheEnergy;

                if (trackEnergy)
                {
                    populations.AddTrackedEnergyForSpecies(currentSpecies, patch, subMiche.Pressure,
                        subMiche.Pressure.Score((MicrobeSpecies)currentSpecies, cache), micheEnergy);
                }
            }

            long newPopulation = (long)(totalEnergy / individualCost);

            if (currentSpecies.PlayerSpecies)
                newPopulation = simulationConfiguration.Results.GetPopulationInPatch(currentSpecies, patch);

            if (trackEnergy)
            {
                populations.AddTrackedEnergyConsumptionForSpecies(currentSpecies, patch, newPopulation,
                    totalEnergy, individualCost);
            }

            // Can't survive without enough population
            if (newPopulation < Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION)
                newPopulation = 0;

            populations.AddPopulationResultForSpecies(currentSpecies, patch, newPopulation);
        }
    }
}