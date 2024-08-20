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

        // TODO: take in a random seed (would help to make sure the random cannot result in the same sequence as
        // this class instances are allocated in a pretty tight loop
        random = new Random();
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    public bool RunStep(RunResults results)
    {
        var miche = results.GetMicheForPatch(patch);

        var occupantsSet = new HashSet<Species>();
        miche.GetOccupants(occupantsSet);

        if (occupantsSet.Count < 1)
            return true;

        var occupants = occupantsSet.ToList();

        var species = occupants.Random(random);

        // Player has a separate GUI to control their migrations purposefully so auto-evo doesn't do it automatically
        if (species.PlayerSpecies)
            return true;

        var population = patch.GetSpeciesSimulationPopulation(species);
        if (population < Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION)
            return true;

        // Select a random adjacent target patch
        // TODO: could prefer patches this species is not already in or about to go extinct, or really anything other
        // than random selection
        var target = patch.Adjacent.Random(random);
        var targetMiche = results.GetMicheForPatch(target);

        // Calculate random amount of population to send
        var moveAmount = (long)random.Next(population * Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION_FRACTION,
            population * Constants.AUTO_EVO_MAXIMUM_MOVE_POPULATION_FRACTION);

        var scores = new Dictionary<Species, float>();
        miche.SetupScores(scores, occupantsSet);

        if (moveAmount > 0 && targetMiche.InsertSpecies(species, patch, scores, cache, true, occupantsSet))
        {
            results.AddMigrationResultForSpecies(species, new SpeciesMigration(patch, target, moveAmount));
            return true;
        }

        return true;
    }
}
