namespace AutoEvo;

using System;
using System.Linq;

/// <summary>
///   Step that finds the best migration for a single species
/// </summary>
public class FindBestMigration : VariantTryingStep
{
    private readonly IAutoEvoConfiguration configuration;
    private readonly WorldGenerationSettings worldSettings;
    private readonly PatchMap map;
    private readonly Species species;
    private readonly Random random;
    private readonly SimulationCache cache;

    public FindBestMigration(IAutoEvoConfiguration configuration, WorldGenerationSettings worldSettings,
        PatchMap map, Species species, Random random, int migrationsToTry,
        bool allowNoMigration) : base(migrationsToTry, allowNoMigration)
    {
        this.configuration = configuration;
        this.worldSettings = worldSettings;
        this.map = map;
        this.species = species;
        this.random = new Random(random.Next());
        cache = new SimulationCache(worldSettings);
    }

    public override bool CanRunConcurrently => true;

    protected override void OnBestResultFound(RunResults results, IAttemptResult bestVariant)
    {
        var variant = (AttemptResult)bestVariant;

        if (variant.Migration == null)
            return;

        results.AddMigrationResultForSpecies(species, variant.Migration);
    }

    protected override IAttemptResult TryCurrentVariant()
    {
        var config = new SimulationConfiguration(configuration, map, worldSettings,
            Constants.AUTO_EVO_VARIANT_SIMULATION_STEPS);

        config.SetPatchesToRunBySpeciesPresence(species);

        PopulationSimulation.Simulate(config, cache, random);

        var population = config.Results.GetGlobalPopulation(species);

        return new AttemptResult(null, population);
    }

    protected override IAttemptResult TryVariant()
    {
        var migration = GetRandomMigration();

        // Move generation can randomly fail
        if (migration == null)
            return new AttemptResult(null, -1);

        var config = new SimulationConfiguration(configuration, map, worldSettings,
            Constants.AUTO_EVO_VARIANT_SIMULATION_STEPS);

        config.SetPatchesToRunBySpeciesPresence(species);
        config.PatchesToRun.Add(migration.From);
        config.PatchesToRun.Add(migration.To);

        config.Migrations.Add(new Tuple<Species, SpeciesMigration>(species, migration));

        // TODO: this could be faster to just simulate the source and destination patches
        // (assuming in the future no global effects of migrations are added, which would need a full patch map
        // simulation anyway). However that would need to take into account that the no-migration variant,
        // as it simulates all patches, would always result in higher populations
        PopulationSimulation.Simulate(config, cache, random);

        var population = config.Results.GetGlobalPopulation(species);

        return new AttemptResult(migration, population);
    }

    /// <summary>
    ///   Generates a valid move (or null) for the species. Implements an algorithm for AI species to select
    ///   beneficial migrations. For now this is just random
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This should be relative cheap and shouldn't be perfect. This is executed multiple times and the best
    ///     migration (with the most resulting global population) is selected.
    ///   </para>
    /// </remarks>
    private SpeciesMigration? GetRandomMigration()
    {
        int attemptsLeft = 10;

        while (attemptsLeft > 0)
        {
            --attemptsLeft;

            // Randomly select starting patch
            var entry = map.Patches.Where(p => p.Value.SpeciesInPatch.ContainsKey(species))
                .OrderBy(_ => random.Next()).Take(1).ToList();

            if (entry.Count > 0)
            {
                Patch patch = entry[0].Value;

                var population = patch.GetSpeciesSimulationPopulation(species);
                if (population < Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION)
                    continue;

                if (patch.Adjacent.Count < 1)
                    continue;

                // Select a random adjacent target patch
                // TODO: could prefer patches this species is not already in or about to go extinct, or really anything
                // other than random selection
                var target = patch.Adjacent.ToList().Random(random);

                // Calculate random amount of population to send
                int moveAmount = (int)random.Next(population * Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION_FRACTION,
                    population * Constants.AUTO_EVO_MAXIMUM_MOVE_POPULATION_FRACTION);

                if (moveAmount > 0)
                {
                    // Move is a success
                    return new SpeciesMigration(patch, target, moveAmount);
                }
            }
        }

        // Could not find a valid move
        return null;
    }

    private class AttemptResult : IAttemptResult
    {
        public AttemptResult(SpeciesMigration? migration, long score)
        {
            Migration = migration;
            Score = score;
        }

        public SpeciesMigration? Migration { get; }
        public long Score { get; }
    }
}
