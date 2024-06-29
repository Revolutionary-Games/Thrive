namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Mutation = System.Tuple<MicrobeSpecies, MicrobeSpecies, RunResults.NewSpeciesType>;

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
    ///   as well as a copy of the original species.
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

        Random random = new Random();
        foreach (var variant in viableVariants)
        {
            MutationLogicFunctions.NameNewMicrobeSpecies(variant, species);

            var oldColour = species.Colour;

            var redShift = random.NextDouble() * 0.1 - 0.05;
            var greenShift = random.NextDouble() * 0.1 - 0.05;
            var blueShift = random.NextDouble() * 0.1 - 0.05;

            variant.Colour = new Color(Mathf.Clamp((float)(oldColour.R + redShift), 0, 1),
                Mathf.Clamp((float)(oldColour.G + greenShift), 0, 1),
                Mathf.Clamp((float)(oldColour.B + blueShift), 0, 1));
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

                // Break if mutation fails a new pressure
                if (newScore <= 0 && oldScore > 0)
                {
                    combinedScores = -1;
                    break;
                }

                combinedScores += pastPressure.WeightedComparedScores(newScore, oldScore);
            }

            if (combinedScores >= 0)
            {
                viableVariants.Add(potentialVariant);
            }
        }

        return viableVariants;
    }

    public bool RunStep(RunResults results)
    {
        // Have this be passed in
        var random = new Random();

        var oldMiche = results.MicheByPatch[Patch];
        var oldOccupants = oldMiche.AllOccupants().ToList();

        // TODO: Put these in auto evo config
        const int possibleMutationsPerSpecies = 3;
        const int totalMutationsToTry = 20;

        var mutationsToTry = new List<Mutation>();

        var leafNodes = oldMiche.AllLeafNodes().Where(x => x.Occupant != null).ToList();

        foreach (var species in oldOccupants)
        {
            foreach (var traversal in leafNodes.Where(x => x.Occupant == species).Select(x => x.BackTraversal()))
            {
                var partlist = new MutationLibrary(species);

                var pressures = traversal.Select(x => x.Pressure).ToList();

                pressures.AddRange(SpeciesDependentPressures(oldMiche, species));

                var variants = ViableVariants(results, species, Patch, partlist, Cache, pressures);

                mutationsToTry.AddRange(variants.Take(possibleMutationsPerSpecies)
                    .Select(speciesToAdd => new Mutation(species, speciesToAdd,
                        RunResults.NewSpeciesType.SplitDueToMutation)).ToList());
            }
        }

        // This section of the code tries to mutate species into unfilled miches
        // Not exactly realistic, but more diversity is more fun for the player
        var emptytraversals = oldMiche.AllLeafNodes().Where(x => x.Occupant == null)
            .Select(x => x.BackTraversal()).ToList();

        foreach (var species in oldOccupants)
        {
            foreach (var traversal in emptytraversals)
            {
                var partlist = new MutationLibrary(species);

                var pressures = traversal.Select(x => x.Pressure).ToList();

                pressures.AddRange(SpeciesDependentPressures(oldMiche, species));

                var variants = ViableVariants(results, species, Patch, partlist, Cache, pressures);

                mutationsToTry.AddRange(variants.Take(possibleMutationsPerSpecies)
                    .Select(speciesToAdd => new Mutation(species, speciesToAdd,
                        RunResults.NewSpeciesType.FillNiche)).ToList());
            }
        }

        var newMiche = oldMiche.DeepCopy();

        mutationsToTry = mutationsToTry.Where(x => x.Item1 != x.Item2).OrderBy(_ => random.Next())
            .Take(totalMutationsToTry).ToList();

        foreach (var mutation in mutationsToTry)
        {
            mutation.Item2.OnEdited();

            newMiche.InsertSpecies(mutation.Item2, Cache);
        }

        var newOccupants = newMiche.AllOccupants().ToList();

        var handledMutations = new List<MicrobeSpecies>();

        // This gets the best mutation for each species.
        // All other mutations will split off to form a new species.
        foreach (var species in oldOccupants)
        {
            if (!newOccupants.Contains(species) || species.PlayerSpecies)
                continue;

            Mutation? bestMutation = null;
            var bestScore = 0.0f;

            var parentTraversal = leafNodes.Where(x => x.Occupant == species).Select(x => x.BackTraversal()).ToList();
            var parentPressures = parentTraversal.SelectMany(x => x).Select(x => x.Pressure).Distinct().ToList();

            // find the initial scores of the parent
            var pressureScores = new Dictionary<SelectionPressure, float>();
            foreach (var pressure in parentPressures)
            {
                pressureScores[pressure] = pressure.Score(species, Cache);
            }

            foreach (var mutation in mutationsToTry.Where(x => x.Item1 == species))
            {
                var combinedScores = 0.0f;

                foreach (var traversal in parentTraversal)
                {
                    var pressures = traversal.Select(x => x.Pressure).ToList();

                    foreach (var pressure in pressures)
                    {
                        var newScore = pressure.Score(mutation.Item2, Cache);
                        var oldScore = pressureScores[pressure];

                        // Break if mutation fails a pressure
                        if (newScore <= 0)
                        {
                            combinedScores = -1;
                            goto break_loop;
                        }

                        combinedScores += pressure.WeightedComparedScores(newScore, oldScore);
                    }
                }

            break_loop:
                if (combinedScores > bestScore)
                {
                    bestScore = combinedScores;
                    bestMutation = mutation;
                }
            }

            if (bestMutation != null)
            {
                handledMutations.Add(bestMutation.Item2);
                results.AddMutationResultForSpecies(bestMutation.Item1, bestMutation.Item2);
            }
        }

        // Before adding the results for the species we verify the mutations were not overridden in the miche tree
        // by a better mutation. This pervents species from instantly going extinict.
        foreach (var mutation in mutationsToTry)
        {
            if (newOccupants.Contains(mutation.Item2) && !handledMutations.Contains(mutation.Item2))
            {
                handledMutations.Add(mutation.Item2);
                results.AddNewSpecies(mutation.Item2, [new KeyValuePair<Patch, long>(Patch, 1000)],
                    mutation.Item3, mutation.Item1);
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
        // TODO: Make that weight a constant
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
