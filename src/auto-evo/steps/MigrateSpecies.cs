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

    private readonly HashSet<Species> speciesWorkMemory = new();

    public MigrateSpecies(Species species, PatchMap map, WorldGenerationSettings worldSettings, SimulationCache cache,
        Random randomSource)
    {
        this.species = species;
        this.cache = cache;
        this.map = map;
        this.worldSettings = worldSettings;

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
        var attemptsLeft = worldSettings.AutoEvoConfiguration.MoveAttemptsPerSpecies;

        foreach (var patch in map.Patches.Values)
        {
            if (!patch.SpeciesInPatch.ContainsKey(species))
                continue;

            var population = patch.GetSpeciesSimulationPopulation(species);

            if (population < Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION)
                continue;

            for (var i = 0; i < attemptsLeft; ++i)
            {
                // Select a random adjacent target patch
                // TODO: could prefer patches this species is not already in or about to go extinct, or really anything
                // other than random selection
                var target = patch.Adjacent.Random(random);
                var targetMiche = results.GetMicheForPatch(target);

                // Calculate random amount of population to send
                var moveAmount = (long)random.Next(population * Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION_FRACTION,
                    population * Constants.AUTO_EVO_MAXIMUM_MOVE_POPULATION_FRACTION);

                if (moveAmount > 0 && targetMiche.InsertSpecies(species, patch, null, cache, true, speciesWorkMemory))
                {
                    results.AddMigrationResultForSpecies(species, new SpeciesMigration(patch, target, moveAmount));
                    break;
                }
            }
        }

        return true;
    }
}
