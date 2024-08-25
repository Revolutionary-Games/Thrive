namespace AutoEvo;

using System;
using System.Collections.Generic;
using Xoshiro.PRNG32;

/// <summary>
///   Step that generates species migrations for each species
/// </summary>
public class MigrateSpecies : IRunStep
{
    private readonly Species species;
    private readonly PatchMap map;
    private readonly WorldGenerationSettings worldSettings;
    private readonly SimulationCache cache;
    private readonly Random random;
    private readonly Miche.InsertWorkingMemory insertWorkingMemory;

    public MigrateSpecies(Species species, PatchMap map, WorldGenerationSettings worldSettings, SimulationCache cache,
        Random randomSource)
    {
        this.species = species;
        this.cache = cache;
        this.map = map;
        this.worldSettings = worldSettings;

        insertWorkingMemory = new Miche.InsertWorkingMemory();

        random = new XoShiRo128starstar(randomSource.NextInt64());
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    public bool RunStep(RunResults results)
    {
        // Player has a separate GUI to control their migrations purposefully so auto-evo doesn't do it automatically
        if (species.PlayerSpecies)
            return true;

        // To limit species migrations to the actual limit, we need to pick random patches to try to migrate from
        // to ensure all patches have a chance to eventually send some population
        int attemptsLeft = worldSettings.AutoEvoConfiguration.MoveAttemptsPerSpecies;

        var sourcePatches = new List<Patch>();

        // To prevent generating duplicate migrations this needs to remember target patches. This limitation is a
        // data model limitation where a single target patch cannot have a species migrate to it form multiple patches
        // at once.
        var usedTargets = new HashSet<Patch>();

        foreach (var patch in map.Patches.Values)
        {
            if (!patch.SpeciesInPatch.ContainsKey(species))
                continue;

            sourcePatches.Add(patch);
        }

        sourcePatches.Shuffle(random);

        // To not randomly pick the same adjacent patch multiple times as a migration target
        var shuffledNeighbours = new List<Patch>();

        foreach (var patch in sourcePatches)
        {
            if (attemptsLeft <= 0)
            {
                // Stop checking once all attempts are used up even if there are still patches this species is in
                break;
            }

            var population = patch.GetSpeciesSimulationPopulation(species);

            if (population < Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION)
                continue;

            // Try all neighbour patches in random order
            shuffledNeighbours.Clear();
            foreach (var adjacent in patch.Adjacent)
            {
                shuffledNeighbours.Add(adjacent);
            }

            // TODO: could prefer patches this species is not already in or about to go extinct, or really anything
            // other than random selection
            shuffledNeighbours.Shuffle(random);

            foreach (var target in shuffledNeighbours)
            {
                // Skip checking population send to the same patch multiple times
                if (usedTargets.Contains(target))
                    continue;

                --attemptsLeft;
                var targetMiche = results.GetMicheForPatch(target);

                // Calculate random amount of population to send
                var moveAmount = (long)random.Next(population * Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION_FRACTION,
                    population * Constants.AUTO_EVO_MAXIMUM_MOVE_POPULATION_FRACTION);

                if (moveAmount > 0 && targetMiche.InsertSpecies(species, patch, null, cache, true, insertWorkingMemory))
                {
                    results.AddMigrationResultForSpecies(species, new SpeciesMigration(patch, target, moveAmount));
                    usedTargets.Add(target);

                    // Only one migration per patch
                    break;
                }

                if (attemptsLeft <= 0)
                    break;
            }
        }

        return true;
    }
}
