namespace AutoEvo;

using System;
using System.Linq;

public class MigrateSpecies : IRunStep
{
    private Patch patch;
    private SimulationCache cache;
    private Random random;

    public MigrateSpecies(Patch patch, SimulationCache cache)
    {
        this.patch = patch;
        this.cache = cache;
        random = new Random();
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    public bool RunStep(RunResults results)
    {
        // Move this to config
        const int moveAttempts = 5;

        var miche = results.MicheByPatch[patch];

        var occupants = miche.AllOccupants().Distinct().ToList();

        if (occupants.Count == 0)
            return true;

        for (int i = 0; i < moveAttempts; i++)
        {
            var species = occupants.ToList().Random(random);

            var population = patch.GetSpeciesSimulationPopulation(species);
            if (population < Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION)
                continue;

            // Select a random adjacent target patch
            // TODO: could prefer patches this species is not already
            // in or about to go extinct, or really anything other
            // than random selection
            var target = patch.Adjacent.ToList().Random(random);

            // possibly very overkill
            var newMiche = results.MicheByPatch[target].DeepCopy();

            // Calculate random amount of population to send
            int moveAmount = (int)random.Next(population * Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION_FRACTION,
                population * Constants.AUTO_EVO_MAXIMUM_MOVE_POPULATION_FRACTION);

            if (moveAmount > 0 && newMiche.InsertSpecies(species, cache))
            {
                results.AddMigrationResultForSpecies(species, new SpeciesMigration(patch, target, moveAmount));
                return true;
            }
        }

        return true;
    }
}
