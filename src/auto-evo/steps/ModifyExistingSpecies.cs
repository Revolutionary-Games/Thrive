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
    public static List<MicrobeSpecies> GenerateMutations(MicrobeSpecies baseSpecies, int amount,
        SimulationCache cache, List<SelectionPressure> selectionPressures)
    {
        var random = new Random();

        // TODO: Possibly make that 100 a difficulty setting
        var viableVariants = new List<Tuple<MicrobeSpecies, float>>
            { Tuple.Create(baseSpecies, 100.0f) };

        // AutoEvo assumes that the order mutations are applied doesn't matter when it actually can affect
        // results quite a bit. Maybe they should have weights?
        var mutationStrategies =
            selectionPressures.SelectMany(x => x.Mutations).Distinct().OrderBy(_ => random.Next()).ToList();

        foreach (var mutationStrategy in mutationStrategies)
        {
            var inputSpecies = viableVariants;

            var i = 0;

            while (true)
            {
                var outputSpecies = new List<Tuple<MicrobeSpecies, float>>();

                foreach (var speciesTuple in inputSpecies)
                {
                    var mutated = mutationStrategy.MutationsOf(speciesTuple.Item1, speciesTuple.Item2);

                    outputSpecies.AddRange(PruneMutations(speciesTuple.Item1, mutated, cache, selectionPressures));
                }

                // TODO: Given we limit the size already these could be made into arrays and with some proper juggling
                // we could avoid allocations
                // TODO: Make these a performance setting?
                if (inputSpecies.Count > 500)
                {
                    inputSpecies = GetTopMutations(baseSpecies, viableVariants, 250, cache, selectionPressures)
                        .ToList();
                }
                else
                {
                    inputSpecies = PruneMutations(baseSpecies, outputSpecies, cache, selectionPressures);
                }

                viableVariants.AddRange(inputSpecies);

                if (viableVariants.Count > 1000)
                {
                    viableVariants = GetTopMutations(baseSpecies, viableVariants, 500, cache, selectionPressures)
                        .ToList();
                }

                if (outputSpecies.Count == 0)
                    break;

                if (!mutationStrategy.Repeatable)
                    break;

                // Sanity check to prevent hanging
                if (i > 100)
                    throw new Exception("Mutation Loop Never Broke");

                // FIXME: Somehow RemoveOrganelle keeps triggering this
                if (i > 11)
                {
                    GD.Print(mutationStrategy.GetType());
                    GD.Print(outputSpecies.Select(x => x.Item2).Max());
                }

                i++;
            }
        }

        var mutatedSpecies = viableVariants.Select(x => x.Item1).ToList();

        foreach (var variant in mutatedSpecies)
        {
            MutationLogicFunctions.NameNewMicrobeSpecies(variant, baseSpecies);

            var oldColour = variant.Colour;

            var redShift = random.NextDouble() * 0.1 - 0.05;
            var greenShift = random.NextDouble() * 0.1 - 0.05;
            var blueShift = random.NextDouble() * 0.1 - 0.05;

            variant.Colour = new Color(Mathf.Clamp((float)(oldColour.R + redShift), 0, 1),
                Mathf.Clamp((float)(oldColour.G + greenShift), 0, 1),
                Mathf.Clamp((float)(oldColour.B + blueShift), 0, 1));
        }

        return GetTopMutations(baseSpecies, viableVariants, amount, cache, selectionPressures).Select(x => x.Item1)
            .ToList();
    }

    public static List<Tuple<MicrobeSpecies, float>> PruneMutations(MicrobeSpecies baseSpecies,
        List<Tuple<MicrobeSpecies, float>> mutated, SimulationCache cache, List<SelectionPressure> selectionPressures)
    {
        var outputSpecies = new List<Tuple<MicrobeSpecies, float>>();

        foreach (var potentialVariant in mutated)
        {
            var combinedScores = 0.0;
            foreach (var pastPressure in selectionPressures)
            {
                var newScore = pastPressure.Score(potentialVariant.Item1, cache);
                var oldScore = pastPressure.Score(baseSpecies, cache);

                // Break if mutation fails a new pressure
                if (newScore <= 0 && oldScore > 0)
                {
                    combinedScores = -1;
                    break;
                }

                combinedScores += pastPressure.WeightedComparedScores(newScore, oldScore);
            }

            if (combinedScores > 0)
            {
                outputSpecies.Add(potentialVariant);
            }
        }

        return outputSpecies;
    }

    public static IEnumerable<Tuple<MicrobeSpecies, float>> GetTopMutations(MicrobeSpecies baseSpecies,
        List<Tuple<MicrobeSpecies, float>> mutated, int amount, SimulationCache cache,
        List<SelectionPressure> selectionPressures)
    {
        // Find the initial scores
        var pressureScores = new Dictionary<SelectionPressure, float>();
        foreach (var curPressure in selectionPressures)
        {
            pressureScores[curPressure] = curPressure.Score(baseSpecies, cache);
        }

        return mutated.OrderByDescending(x =>
                selectionPressures.Select(pressure =>
                    pressure.Score(x.Item1, cache) / pressureScores[pressure] * pressure.Strength).Sum())
            .ThenBy(x => x.Item2).Take(amount);
    }

    public bool RunStep(RunResults results)
    {
        // Have this be passed in
        var random = new Random();

        var oldMiche = results.MicheByPatch[Patch];
        var oldOccupants = oldMiche.GetOccupants().Distinct().ToList();

        // TODO: Put these in auto evo config
        const int possibleMutationsToTryPerSpecies = 3;
        const int totalMutationsToTry = 20;

        var mutationsToTry = new List<Mutation>();

        var leafNodes = oldMiche.GetLeafNodes().Where(x => x.Occupant != null).ToList();

        foreach (var species in oldOccupants)
        {
            foreach (var traversal in leafNodes.Where(x => x.Occupant == species).Select(x => x.BackTraversal()))
            {
                var pressures = traversal.Select(x => x.Pressure).ToList();

                pressures.AddRange(SpeciesDependentPressures(oldMiche, species));

                var variants = GenerateMutations(species, possibleMutationsToTryPerSpecies, Cache, pressures);

                mutationsToTry.AddRange(variants.Select(speciesToAdd => new Mutation(species, speciesToAdd,
                    RunResults.NewSpeciesType.SplitDueToMutation)).ToList());
            }
        }

        // This section of the code tries to mutate species into unfilled miches
        // Not exactly realistic, but more diversity is more fun for the player
        var emptyTraversals = oldMiche.GetLeafNodes().Where(x => x.Occupant == null)
            .Select(x => x.BackTraversal()).ToList();

        foreach (var species in oldOccupants)
        {
            foreach (var traversal in emptyTraversals)
            {
                var pressures = traversal.Select(x => x.Pressure).ToList();

                pressures.AddRange(SpeciesDependentPressures(oldMiche, species));

                var variants = GenerateMutations(species, possibleMutationsToTryPerSpecies, Cache, pressures);

                mutationsToTry.AddRange(variants.Select(speciesToAdd => new Mutation(species, speciesToAdd,
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

        var newOccupants = newMiche.GetOccupants().ToList();

        var handledMutations = new HashSet<MicrobeSpecies>();

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

            foreach (var mutation in mutationsToTry)
            {
                if (mutation.Item1 == species)
                    continue;

                var combinedScores = 0.0f;

                foreach (var traversal in parentTraversal)
                {
                    foreach (var miche in traversal)
                    {
                        var pressure = miche.Pressure;

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
        // by a better mutation. This prevents species from instantly going extinct.
        foreach (var mutation in mutationsToTry)
        {
            if (newOccupants.Contains(mutation.Item2) && !handledMutations.Contains(mutation.Item2))
            {
                handledMutations.Add(mutation.Item2);

                var newPopulation =
                    MichePopulation.CalculateMicrobePopulationInPatch(mutation.Item2, newMiche, Patch.Biome, Cache);

                if (newPopulation > Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION)
                {
                    results.AddNewSpecies(mutation.Item2, [new KeyValuePair<Patch, long>(Patch, newPopulation)],
                        mutation.Item3, mutation.Item1);
                }
            }
        }

        return true;
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
        var predators = new List<Species>();

        // TODO: Make this WAY more efficient
        foreach (var traversal in miche.AllTraversals())
        {
            foreach (var curMiche in traversal)
            {
                if (curMiche.Pressure is PredationEffectivenessPressure pressure && pressure.Prey == species)
                {
                    predators.AddRange(curMiche.GetOccupants());
                }
            }
        }

        return predators;
    }
}
