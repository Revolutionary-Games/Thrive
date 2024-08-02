namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Mutation = System.Tuple<MicrobeSpecies, MicrobeSpecies, RunResults.NewSpeciesType>;

public class ModifyExistingSpecies : IRunStep
{
    private readonly Patch patch;
    private readonly SimulationCache cache;

    private readonly WorldGenerationSettings worldSettings;

    public ModifyExistingSpecies(Patch patch, SimulationCache cache, WorldGenerationSettings worldSettings)
    {
        this.patch = patch;
        this.cache = cache;
        this.worldSettings = worldSettings;
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    public static List<Tuple<MicrobeSpecies, float>> PruneMutations(MicrobeSpecies baseSpecies,
        List<Tuple<MicrobeSpecies, float>> mutated, SimulationCache cache, List<SelectionPressure> selectionPressures)
    {
        var outputSpecies = new List<Tuple<MicrobeSpecies, float>>();

        foreach (var potentialVariant in mutated)
        {
            var combinedScores = 0.0;
            foreach (var pastPressure in selectionPressures)
            {
                var newScore = cache.GetPressureScore(pastPressure, potentialVariant.Item1);
                var oldScore = cache.GetPressureScore(pastPressure, baseSpecies);

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
        return mutated.OrderByDescending(x =>
                selectionPressures.Select(pressure =>
                    cache.GetPressureScore(pressure, x.Item1) / cache.GetPressureScore(pressure, baseSpecies) *
                    pressure.Strength).Sum())
            .ThenBy(x => x.Item2).Take(amount);
    }

    /// <summary>
    ///   Returns a new list of all possible species that might emerge in response to the provided pressures,
    ///   as well as a copy of the original species.
    /// </summary>
    /// <returns>List of viable variants, and the provided species</returns>
    public List<MicrobeSpecies> GenerateMutations(MicrobeSpecies baseSpecies, int amount,
        SimulationCache cache, List<SelectionPressure> selectionPressures, Random random)
    {
        float totalMP = 100 * worldSettings.AIMutationMultiplier;

        var viableVariants = new List<Tuple<MicrobeSpecies, float>>
            { Tuple.Create(baseSpecies, totalMP) };

        // Auto-Evo assumes that the order mutations are applied doesn't matter when it actually can affect
        // results quite a bit. Maybe they should have weights?
        var mutationStrategies =
            selectionPressures.SelectMany(x => x.Mutations).Distinct().OrderBy(_ => random.Next()).ToList();

        var outputSpecies = new List<Tuple<MicrobeSpecies, float>>();

        foreach (var mutationStrategy in mutationStrategies)
        {
            var inputSpecies = viableVariants;

            for (int i = 0; i < Constants.AUTO_EVO_MAX_MUTATION_RECURSIONS; i++)
            {
                outputSpecies.Clear();

                foreach (var speciesTuple in inputSpecies)
                {
                    var mutated = mutationStrategy.MutationsOf(speciesTuple.Item1, speciesTuple.Item2);

                    outputSpecies.AddRange(PruneMutations(speciesTuple.Item1, mutated, cache, selectionPressures));
                }

                // TODO: Make these a performance setting?
                if (outputSpecies.Count > 60)
                {
                    inputSpecies = GetTopMutations(baseSpecies, outputSpecies, 60 / 2, cache, selectionPressures)
                        .ToList();
                }
                else
                {
                    inputSpecies = PruneMutations(baseSpecies, outputSpecies, cache, selectionPressures);
                }

                viableVariants.AddRange(inputSpecies);

                if (viableVariants.Count > 120)
                {
                    viableVariants = GetTopMutations(baseSpecies, viableVariants, 120 / 2, cache, selectionPressures)
                        .ToList();
                }

                if (outputSpecies.Count == 0)
                    break;

                if (!mutationStrategy.Repeatable)
                    break;
            }
        }

        foreach (var variant in viableVariants.Select(x => x.Item1))
        {
            MutationLogicFunctions.NameNewMicrobeSpecies(variant, baseSpecies);

            var oldColour = variant.Colour;

            var redShift = (random.NextDouble() - 0.5f) * Constants.AUTO_EVO_COLOR_CHANGE_MAX_STEP;
            var greenShift = (random.NextDouble() - 0.5f) * Constants.AUTO_EVO_COLOR_CHANGE_MAX_STEP;
            var blueShift = (random.NextDouble() - 0.5f) * Constants.AUTO_EVO_COLOR_CHANGE_MAX_STEP;

            variant.Colour = new Color(Mathf.Clamp((float)(oldColour.R + redShift), 0, 1),
                Mathf.Clamp((float)(oldColour.G + greenShift), 0, 1),
                Mathf.Clamp((float)(oldColour.B + blueShift), 0, 1));
        }

        return GetTopMutations(baseSpecies, viableVariants, amount, cache, selectionPressures).Select(x => x.Item1)
            .ToList();
    }

    public bool RunStep(RunResults results)
    {
        // TODO: Have this be passed in
        var random = new Random();

        var miche = results.GetMicheForPatch(patch);

        var oldOccupants = new HashSet<Species>();
        miche.GetOccupants(oldOccupants);

        // TODO: Possibly make this a performance setting?
        const int totalMutationsToTry = 20;

        var mutationsToTry = new List<Mutation>();

        var leafNodes = new List<Miche>();
        miche.GetLeafNodes(leafNodes, x => x.Occupant != null);

        var emptyLeafNodes = new List<Miche>();
        miche.GetLeafNodes(emptyLeafNodes, x => x.Occupant == null);

        foreach (var species in oldOccupants)
        {
            if (species is not MicrobeSpecies microbeSpecies)
                continue;

            foreach (var traversal in leafNodes.Where(x => x.Occupant == species).Select(x => x.BackTraversal()))
            {
                var pressures = traversal.Select(x => x.Pressure).ToList();

                pressures.AddRange(SpeciesDependentPressures(miche, species));

                var variants = GenerateMutations(microbeSpecies, worldSettings.AutoEvoConfiguration.MutationsPerSpecies,
                    cache, pressures, random);

                mutationsToTry.AddRange(variants.Select(speciesToAdd => new Mutation(microbeSpecies,
                    speciesToAdd, RunResults.NewSpeciesType.SplitDueToMutation)).ToList());
            }
        }

        // This section of the code tries to mutate species into unfilled miches
        // Not exactly realistic, but more diversity is more fun for the player
        var emptyTraversals = emptyLeafNodes.Select(x => x.BackTraversal()).ToList();

        foreach (var species in oldOccupants)
        {
            if (species is not MicrobeSpecies microbeSpecies)
                continue;

            foreach (var traversal in emptyTraversals)
            {
                var pressures = traversal.Select(x => x.Pressure).ToList();

                pressures.AddRange(SpeciesDependentPressures(miche, species));

                var variants = GenerateMutations(microbeSpecies, worldSettings.AutoEvoConfiguration.MutationsPerSpecies,
                    cache,
                    pressures, random);

                mutationsToTry.AddRange(variants.Select(speciesToAdd => new Mutation(microbeSpecies,
                    speciesToAdd, RunResults.NewSpeciesType.FillNiche)).ToList());
            }
        }

        mutationsToTry = mutationsToTry.Where(x => x.Item1 != x.Item2).OrderBy(_ => random.Next())
            .Take(totalMutationsToTry).ToList();

        var newMiche = miche.DeepCopy();

        foreach (var mutation in mutationsToTry)
        {
            mutation.Item2.OnEdited();

            newMiche.InsertSpecies(mutation.Item2, cache);
        }

        var newOccupants = new HashSet<Species>();
        newMiche.GetOccupants(newOccupants);

        var handledMutations = new HashSet<MicrobeSpecies>();

        // This gets the best mutation for each species.
        // All other mutations will split off to form a new species.
        foreach (var species in oldOccupants)
        {
            if (newOccupants.Contains(species) || species.PlayerSpecies)
                continue;

            Mutation? bestMutation = null;
            var bestScore = 0.0f;

            var parentTraversal = leafNodes.Where(x => x.Occupant == species).Select(x => x.BackTraversal()).ToList();

            foreach (var mutation in mutationsToTry)
            {
                if (mutation.Item1 != species)
                    continue;

                var combinedScores = 0.0f;

                foreach (var traversal in parentTraversal)
                {
                    foreach (var currentMiche in traversal)
                    {
                        var pressure = currentMiche.Pressure;

                        var newScore = cache.GetPressureScore(pressure, mutation.Item2);
                        var oldScore = cache.GetPressureScore(pressure, species);

                        // Break if mutation fails a pressure
                        if (newScore <= 0)
                        {
                            combinedScores = -1;
                            break;
                        }

                        combinedScores += pressure.WeightedComparedScores(newScore, oldScore);
                    }

                    if (combinedScores < 0)
                        break;
                }

                if (combinedScores > bestScore)
                {
                    bestScore = combinedScores;
                    bestMutation = mutation;
                }
            }

            if (bestMutation != null)
            {
                GD.Print("result");
                handledMutations.Add(bestMutation.Item2);
                results.AddMutationResultForSpecies(bestMutation.Item1, bestMutation.Item2);
            }
        }

        foreach (var mutation in mutationsToTry)
        {
            // Before adding the results for the species we verify the mutations were not overridden in the miche tree
            // by a better mutation. This prevents species from instantly going extinct.
            if (newOccupants.Contains(mutation.Item2) && !handledMutations.Contains(mutation.Item2))
            {
                handledMutations.Add(mutation.Item2);

                var newPopulation =
                    MichePopulation.CalculateMicrobePopulationInPatch(mutation.Item2, newMiche, patch.Biome, cache);

                if (newPopulation > Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION)
                {
                    results.AddNewSpecies(mutation.Item2, [new KeyValuePair<Patch, long>(patch, newPopulation)],
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
            .Select(x => new AvoidPredationSelectionPressure(x, 5.0f, patch)).ToList());
    }

    /// <summary>
    ///   Returns a new list of all species that have filled a predation miche to eat the provided species.
    /// </summary>
    /// <param name="micheTree">Miche tree to search</param>
    /// <param name="species">Species to search for Predation miches of</param>
    /// <returns>List of species</returns>
    private List<Species> PredatorsOf(Miche micheTree, Species species)
    {
        var predators = new HashSet<Species>();

        var leafNodes = new List<Miche>();
        micheTree.GetLeafNodes(leafNodes);

        foreach (var miche in leafNodes)
        {
            if (miche.Pressure is PredationEffectivenessPressure pressure && pressure.Prey == species)
            {
                miche.GetOccupants(predators);
            }
        }

        return predators.ToList();
    }
}
