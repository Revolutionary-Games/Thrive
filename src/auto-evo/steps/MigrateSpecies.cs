namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Step that generates species migrations for each patch
/// </summary>
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
        var miche = results.GetMicheForPatch(patch);

        var occupantsSet = new HashSet<Species>();
        miche.GetOccupants(occupantsSet);

        var occupants = occupantsSet.ToList();

        if (occupants.Count == 0)
            return true;

        var species = occupants.Random(random);

        // TODO: Make this a game option?
        if (species.PlayerSpecies)
            return true;

        var population = patch.GetSpeciesSimulationPopulation(species);
        if (population < Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION)
            return true;

        // Select a random adjacent target patch
        // TODO: could prefer patches this species is not already
        // in or about to go extinct, or really anything other
        // than random selection
        var target = patch.Adjacent.ToList().Random(random);
        var targetMiche = results.GetMicheForPatch(target);

        // Calculate random amount of population to send
        int moveAmount = (int)random.Next(population * Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION_FRACTION,
            population * Constants.AUTO_EVO_MAXIMUM_MOVE_POPULATION_FRACTION);

        if (moveAmount > 0 && targetMiche.InsertSpecies(species, patch, cache, true))
        {
            results.AddMigrationResultForSpecies(species, new SpeciesMigration(patch, target, moveAmount));
            return true;
        }

        return true;
    }
}
