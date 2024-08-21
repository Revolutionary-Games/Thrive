namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using Xoshiro.PRNG32;

/// <summary>
///   Step that generates species migrations for each patch
/// </summary>
/// <remarks>
///   <para>
///     TODO: this can currently cause migrations for a single species from many patches to move to a single patch
///     this is unsupported by <see cref="RunResults.GetMigrationsTo"/> so these migrations are removed by
///     <see cref="RemoveInvalidMigrations"/> but it would be better if this class was fixed instead, probably. The old
///     approach processed migrations *per species* instead of per patch to not hit in to the same data model
///     limitation. To track how much this happens a debug print can be uncommented in
///     RemoveDuplicateTargetPatchMigrations.
///   </para>
/// </remarks>
public class MigrateSpecies : IRunStep
{
    private readonly Patch patch;
    private readonly SimulationCache cache;
    private readonly Random random;

    private readonly HashSet<Species> speciesWorkMemory = new();

    /// <summary>
    ///   To avoid generating duplicate migrations, this remembers
    /// </summary>
    private readonly List<(Species Species, Patch Patch)> alreadyDoneMigrations = new();

    private int stepsDone;

    private List<Species>? occupants;

    public MigrateSpecies(Patch patch, SimulationCache cache, Random randomSource)
    {
        this.patch = patch;
        this.cache = cache;

        // TODO: take in a random seed (would help to make sure the random cannot result in the same sequence as
        // this class instances are allocated in a pretty tight loop
        random = new XoShiRo128starstar(randomSource.Next());
    }

    public bool Done => stepsDone >= Constants.AUTO_EVO_MOVE_ATTEMPTS;

    public int TotalSteps => Constants.AUTO_EVO_MOVE_ATTEMPTS;

    public bool CanRunConcurrently => true;

    public bool RunStep(RunResults results)
    {
        ++stepsDone;

        if (occupants == null)
        {
            var miche = results.GetMicheForPatch(patch);

            var occupantsSet = new HashSet<Species>();
            miche.GetOccupants(occupantsSet);

            occupants = occupantsSet.ToList();
        }

        if (occupants.Count < 1)
            return Done;

        var species = occupants.Random(random);

        // Player has a separate GUI to control their migrations purposefully so auto-evo doesn't do it automatically
        if (species.PlayerSpecies)
            return Done;

        // TODO: should a single species be allowed to migrate to multiple patches at once or just one migration in
        // general?
        /*foreach (var (doneSpecies, _) in alreadyDoneMigrations)
        {
            if (doneSpecies == species)
                return Done;
        }*/

        var population = patch.GetSpeciesSimulationPopulation(species);
        if (population < Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION)
            return Done;

        // Select a random adjacent target patch
        // TODO: could prefer patches this species is not already in or about to go extinct, or really anything other
        // than random selection
        var target = patch.Adjacent.Random(random);

        foreach (var (doneSpecies, donePatch) in alreadyDoneMigrations)
        {
            if (doneSpecies == species && donePatch == target)
                return Done;
        }

        var targetMiche = results.GetMicheForPatch(target);

        // Calculate random amount of population to send
        var moveAmount = (long)random.Next(population * Constants.AUTO_EVO_MINIMUM_MOVE_POPULATION_FRACTION,
            population * Constants.AUTO_EVO_MAXIMUM_MOVE_POPULATION_FRACTION);

        if (moveAmount > 0 && targetMiche.InsertSpecies(species, patch, null, cache, true, speciesWorkMemory))
        {
            results.AddMigrationResultForSpecies(species, new SpeciesMigration(patch, target, moveAmount));
            alreadyDoneMigrations.Add((species, target));
        }

        return Done;
    }
}
