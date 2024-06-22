namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;

public class ModifyExistingSpecies : IRunStep
{
    public Patch Patch;
    public SimulationCache Cache;

    public ModifyExistingSpecies(Patch patch, SimulationCache cache)
    {
        Patch = patch;
        Cache = cache;
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    /// <summary>
    ///   Returns a new list of all possible species that might emerge in response to the provided pressures,
    ///   as well as a copy of the origonal species.
    /// </summary>
    /// <returns>List of viable variants, and the provided species</returns>
    public static List<MicrobeSpecies> ViableVariants(RunResults results,
        MicrobeSpecies species,
        Patch patch,
        MutationLibrary mutationLibrary,
        SimulationCache cache,
        List<SelectionPressure> selectionPressures)
    {
        // find the initial scores
        var pressureScores = new Dictionary<SelectionPressure, float>();
        foreach (var curPressure in selectionPressures)
        {
            pressureScores[curPressure] = curPressure.Score(species, cache);
        }

        var viableVariants = new List<MicrobeSpecies> { species };

        var pressuresSoFar = new List<SelectionPressure>();
        foreach (var curPressure in selectionPressures)
        {
            pressuresSoFar.Add(curPressure);

            // For each viable variant, get a new variants that at least improve score a little bit
            List<MicrobeSpecies> potentialVariants = viableVariants.Select(startVariant =>
                    MutationsFrom(startVariant, curPressure, mutationLibrary))
                .SelectMany(x => x).ToList();

            potentialVariants.AddRange(viableVariants);

            // Prune variants that hurt the previous scores too much
            viableVariants = PruneInviableSpecies(potentialVariants, pressuresSoFar, species, cache);
        }

        foreach (var variant in viableVariants)
        {
            MutationLogicFunctions.NameNewMicrobeSpecies(variant, species);
        }

        return viableVariants.OrderByDescending(x =>
            selectionPressures.Select(pressure =>
                pressure.Score(x, cache) / pressureScores[pressure] * pressure.Strength).Sum() +
            (x == species ? 0.01f : 0.0f)).ToList();
    }

    /// <summary>
    ///   Returns new list containing only species from the provided list that don't score too badly in the
    ///   provided list of selection pressures.
    /// </summary>
    /// <returns>List of species not ruled to be inviable.</returns>
    public static List<MicrobeSpecies> PruneInviableSpecies(List<MicrobeSpecies> potentialVariants,
        List<SelectionPressure> selectionPressures,
        MicrobeSpecies baseSpecies,
        SimulationCache cache)
    {
        var viableVariants = new List<MicrobeSpecies>();
        foreach (var potentialVariant in potentialVariants)
        {
            var combinedScores = 0.0;
            foreach (var pastPressure in selectionPressures)
            {
                var newScore = pastPressure.Score(potentialVariant, cache);
                var oldScore = pastPressure.Score(baseSpecies, cache);
                combinedScores += pastPressure.WeightedComparedScores(newScore, oldScore);
            }

            if (combinedScores >= 0)
            {
                // potentialVariant.Colour = new Color((float)new Random().NextDouble(), 0.5f, 0.5f);

                viableVariants.Add(potentialVariant);
            }
        }

        return viableVariants;
    }

    public bool RunStep(RunResults results)
    {
        // Have this be passed in
        var random = new Random();

        var currentMiche = results.MicheByPatch[Patch];

        // TODO: Make sure this is actually necessary
        var oldMiche = currentMiche.DeepCopy();

        // TODO: Put these in auto evo config
        const int possibleMutationsPerSpecies = 3;
        const int totalMutationsToTry = 15;

        var mutationsToTry = new List<Tuple<MicrobeSpecies, MicrobeSpecies>>();

        var leafNodes = oldMiche.AllLeafNodes().Where(x => x.Occupant != null).ToList();

        foreach (var species in oldMiche.AllOccupants())
        {
            foreach (var traversal in leafNodes.Where(x => x.Occupant == species).Select(x => x.BackTraversal()))
            {
                var partlist = new MutationLibrary(species);

                var pressures = traversal.Select(x => x.Pressure).ToList();

                pressures.AddRange(SpeciesDependentPressures(oldMiche, species));

                var variants = ViableVariants(results, species, Patch, partlist, Cache, pressures);

                mutationsToTry.AddRange(variants.Take(possibleMutationsPerSpecies)
                    .Select(speciesToAdd => new Tuple<MicrobeSpecies, MicrobeSpecies>(species, speciesToAdd))
                    .ToList());
            }
        }

        var alreadyHandledSpecies = new List<MicrobeSpecies>();

        mutationsToTry = mutationsToTry.OrderBy(_ => random.Next()).Take(totalMutationsToTry).ToList();

        foreach (var pair in mutationsToTry)
        {
            if (pair.Item1 == pair.Item2)
                continue;

            pair.Item2.OnEdited();

            if (!alreadyHandledSpecies.Contains(pair.Item1) && currentMiche.InsertSpecies(pair.Item2, Cache))
            {
                alreadyHandledSpecies.Add(pair.Item1);

                // Check if the mutated species overwrote the old one in all miches
                if (!pair.Item1.PlayerSpecies && !currentMiche.AllOccupants().Contains(pair.Item1))
                {
                    results.AddMutationResultForSpecies(pair.Item1, pair.Item2);
                }
                else
                {
                    results.AddNewSpecies(pair.Item2,
                        [
                            new KeyValuePair<Patch, long>(Patch, 1000),
                        ],
                        RunResults.NewSpeciesType.FillNiche, pair.Item1);
                }
            }
        }

        return true;
    }

    private static List<MicrobeSpecies> MutationsFrom(MicrobeSpecies species, SelectionPressure selectionPressure,
        MutationLibrary mutationLibrary)
    {
        return selectionPressure.Mutations
            .SelectMany(mutationStrategy => mutationStrategy.MutationsOf(species, mutationLibrary)).ToList();
    }

    private List<SelectionPressure> SpeciesDependentPressures(Miche miche, Species species)
    {
        return new List<SelectionPressure>(PredatorsOf(miche, species)
            .Select(x => new AvoidPredationSelectionPressure(x, 5.0f, Patch)).ToList());
    }

    /// <summary>
    ///   Returns a new list of all species that have filled a predation miche to eat the provided species.
    /// </summary>
    /// <param name="miche">Miche to search</param>
    /// <param name="species">Species to search for Predation miches of</param>
    /// <returns>List of species</returns>
    private List<Species> PredatorsOf(Miche miche, Species species)
    {
        var retval = new List<Species>();

        // TODO: Make this WAY more efficient
        foreach (var traversal in miche.AllTraversals())
        {
            foreach (var curMiche in traversal)
            {
                if (curMiche.Pressure is PredationEffectivenessPressure pressure && pressure.Prey == species)
                {
                    retval.AddRange(curMiche.AllOccupants());
                }
            }
        }

        return retval;
    }
}
