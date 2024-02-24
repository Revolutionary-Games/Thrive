namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;

    public class SpeciesMigration : IRunStep
    {
        private Patch patch;
        private SimulationCache cache;
        private WorldGenerationSettings worldSettings;
        private PatchMap map;

        public ModifyExistingSpecies(Patch patch, WorldGenerationSettings worldSettings, PatchMap map,
            SimulationCache cache)
        {
            this.patch = patch;
            this.cache = cache;
            this.worldSettings = worldSettings;
            this.map = map;
        }

        public int TotalSteps => 1;

        public bool CanRunConcurrently => false;

        public bool RunStep(RunResults results)
        {
            new SpeciesMigration()
            results.AddMigrationResultForSpecies(species, variant.Migration);

            return true;
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
                var entry = map.Patches.Where(pair => pair.Value.SpeciesInPatch.ContainsKey(species))
                    .OrderBy(_ => random.Next()).Take(1).ToList();

                if (entry.Count > 0)
                {
                    Patch patch = entry[0].Value;

                    var population = patch.GetSpeciesSimulationPopulation(species);
                    if (population < Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION)
                        continue;

                    // Select a random adjacent target patch
                    // TODO: could prefer patches this species is not already
                    // in or about to go extinct, or really anything other
                    // than random selection
                    var target = patch.Adjacent.ToList().Random(random);

                    if (target == null)
                        continue;

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
    }
}
