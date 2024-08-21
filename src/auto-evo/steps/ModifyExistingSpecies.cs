namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Mutation = System.Tuple<MicrobeSpecies, MicrobeSpecies, RunResults.NewSpeciesType>;

/// <summary>
///   Uses miches to create a mutation for an existing species. Also creates new species from existing ones as
///   mutations.
/// </summary>
public class ModifyExistingSpecies : IRunStep
{
    private readonly Patch patch;
    private readonly SimulationCache cache;

    private readonly WorldGenerationSettings worldSettings;

    private readonly HashSet<Species> speciesWorkMemory = new();
    private readonly HashSet<Species> newOccupantsWorkMemory = new();

    // TODO: this class was originally written with a bunch of LINQ and temporary memory allocations. This has been
    // converted to use persistent memory to avoid allocations but maybe the algorithm should be reworked with the
    // new constraints in mind to get rid of the mess of fields

    private readonly HashSet<Species> predatorCalculationMemory = new();
    private readonly List<Miche> predatorCalculationMemory2 = new();
    private readonly List<Species> predatorPressuresTemporary = new();

    private readonly List<IMutationStrategy<MicrobeSpecies>> tempMutationStrategies = new();

    // TODO: switch to named tuple elements
    private readonly List<Tuple<MicrobeSpecies, float>> temporaryMutations1 = new();
    private readonly List<Tuple<MicrobeSpecies, float>> temporaryMutations2 = new();

    private readonly List<MicrobeSpecies> lastGeneratedMutations = new();

    private readonly List<Miche> currentTraversal = new();

    private readonly List<SelectionPressure> temporaryPressures = new();

    private readonly List<Tuple<MicrobeSpecies, float>> temporaryResultForTopMutations = new();

    private readonly MutationSorter mutationSorter;

    public ModifyExistingSpecies(Patch patch, SimulationCache cache, WorldGenerationSettings worldSettings)
    {
        this.patch = patch;
        this.cache = cache;
        this.worldSettings = worldSettings;

        mutationSorter = new MutationSorter(patch, cache);
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    public bool RunStep(RunResults results)
    {
        // TODO: Have this be passed in
        var random = new Random();

        var miche = results.GetMicheForPatch(patch);

        miche.GetOccupants(speciesWorkMemory);

        // TODO: Possibly make this a performance setting?
        const int totalMutationsToTry = 20;

        var mutationsToTry = new List<Mutation>();

        var nonEmptyLeafNodes = new List<Miche>();
        var emptyLeafNodes = new List<Miche>();
        miche.GetLeafNodes(nonEmptyLeafNodes, emptyLeafNodes, x => x.Occupant != null);

        // For each existing species, add adaptations based on the existing pressures
        foreach (var species in speciesWorkMemory)
        {
            if (species is not MicrobeSpecies microbeSpecies)
                continue;

            // The traversal end up being re-calculated quite many times, but this way we avoid quite a lot of memory
            // allocations

            foreach (var nonEmptyLeaf in nonEmptyLeafNodes)
            {
                if (nonEmptyLeaf.Occupant == species)
                    continue;

                currentTraversal.Clear();
                nonEmptyLeaf.BackTraversal(currentTraversal);

                temporaryPressures.Clear();
                foreach (var traversalMiche in currentTraversal)
                {
                    temporaryPressures.Add(traversalMiche.Pressure);
                }

                SpeciesDependentPressures(temporaryPressures, miche, species);

                var variants = GenerateMutations(microbeSpecies,
                    worldSettings.AutoEvoConfiguration.MutationsPerSpecies, temporaryPressures, random);

                foreach (var variant in variants)
                {
                    mutationsToTry.Add(new Mutation(microbeSpecies, variant,
                        RunResults.NewSpeciesType.SplitDueToMutation));
                }
            }

            // This section of the code tries to mutate species into unfilled miches
            // Not exactly realistic, but more diversity is more fun for the player
            foreach (var emptyLeafNode in emptyLeafNodes)
            {
                currentTraversal.Clear();
                emptyLeafNode.BackTraversal(currentTraversal);

                temporaryPressures.Clear();
                foreach (var traversalMiche in currentTraversal)
                {
                    temporaryPressures.Add(traversalMiche.Pressure);
                }

                SpeciesDependentPressures(temporaryPressures, miche, species);

                var variants = GenerateMutations(microbeSpecies,
                    worldSettings.AutoEvoConfiguration.MutationsPerSpecies, temporaryPressures, random);

                foreach (var variant in variants)
                {
                    mutationsToTry.Add(new Mutation(microbeSpecies, variant, RunResults.NewSpeciesType.FillNiche));
                }
            }
        }

        // Disregard "mutations" that result in identical species"
        mutationsToTry.RemoveAll(m => m.Item1 == m.Item2);

        // Then shuffle and take only as many mutations as we want to try
        mutationsToTry.Shuffle(random);

        while (mutationsToTry.Count > totalMutationsToTry)
        {
            mutationsToTry.RemoveAt(mutationsToTry.Count - 1);
        }

        // Add these mutant species into a new miche
        // TODO: add some way to avoid the deep copy here
        var newMiche = miche.DeepCopy();
        var workMemory = new HashSet<Species>();

        foreach (var mutation in mutationsToTry)
        {
            mutation.Item2.OnEdited();

            newMiche.InsertSpecies(mutation.Item2, patch, null, cache, false, workMemory);
        }

        newOccupantsWorkMemory.Clear();
        newMiche.GetOccupants(newOccupantsWorkMemory);

        var handledMutations = new HashSet<MicrobeSpecies>();

        var speciesSpecificLeaves = new List<Miche>();

        // This gets the best mutation for each species.
        // All other mutations will split off to form a new species.
        foreach (var species in speciesWorkMemory)
        {
            if (newOccupantsWorkMemory.Contains(species) || species.PlayerSpecies)
                continue;

            Mutation? bestMutation = null;
            var bestScore = 0.0f;

            speciesSpecificLeaves.Clear();

            foreach (var nonEmptyLeafNode in nonEmptyLeafNodes)
            {
                if (nonEmptyLeafNode.Occupant == species)
                    speciesSpecificLeaves.Add(nonEmptyLeafNode);
            }

            foreach (var mutation in mutationsToTry)
            {
                if (mutation.Item1 != species)
                    continue;

                var combinedScores = 0.0f;

                foreach (var traversalToDo in speciesSpecificLeaves)
                {
                    currentTraversal.Clear();
                    traversalToDo.BackTraversal(currentTraversal);

                    foreach (var currentMiche in currentTraversal)
                    {
                        var pressure = currentMiche.Pressure;

                        var newScore = cache.GetPressureScore(pressure, patch, mutation.Item2);
                        var oldScore = cache.GetPressureScore(pressure, patch, species);

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
                handledMutations.Add(bestMutation.Item2);
                results.AddMutationResultForSpecies(bestMutation.Item1, bestMutation.Item2);
            }
        }

        foreach (var mutation in mutationsToTry)
        {
            // Before adding the results for the species we verify the mutations were not overridden in the miche tree
            // by a better mutation. This prevents species from instantly going extinct.
            if (newOccupantsWorkMemory.Contains(mutation.Item2) && handledMutations.Add(mutation.Item2))
            {
                var newPopulation =
                    MichePopulation.CalculateMicrobePopulationInPatch(mutation.Item2, newMiche, patch, cache);

                if (newPopulation > Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION)
                {
                    results.AddNewSpecies(mutation.Item2, [new KeyValuePair<Patch, long>(patch, newPopulation)],
                        mutation.Item3, mutation.Item1);
                }
            }
        }

        return true;
    }

    private static void PruneMutations(List<Tuple<MicrobeSpecies, float>> addResultsTo, MicrobeSpecies baseSpecies,
        List<Tuple<MicrobeSpecies, float>> mutated, Patch patch, SimulationCache cache,
        List<SelectionPressure> selectionPressures)
    {
        foreach (var potentialVariant in mutated)
        {
            var combinedScores = 0.0;
            foreach (var pastPressure in selectionPressures)
            {
                var newScore = cache.GetPressureScore(pastPressure, patch, potentialVariant.Item1);
                var oldScore = cache.GetPressureScore(pastPressure, patch, baseSpecies);

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
                addResultsTo.Add(potentialVariant);
            }
        }
    }

    private static void GetTopMutations(List<Tuple<MicrobeSpecies, float>> result,
        List<Tuple<MicrobeSpecies, float>> mutated, int amount, MutationSorter sorter)
    {
        result.Clear();

        mutated.Sort(sorter);

        foreach (var tuple in mutated)
        {
            result.Add(tuple);
            if (result.Count >= amount)
                break;
        }
    }

    private void SpeciesDependentPressures(List<SelectionPressure> dataReceiver, Miche miche, Species species)
    {
        predatorPressuresTemporary.Clear();

        PredatorsOf(predatorPressuresTemporary, miche, species);

        foreach (var predator in predatorPressuresTemporary)
        {
            // TODO: Make that weight a constant
            dataReceiver.Add(new AvoidPredationSelectionPressure(predator, 5.0f));
        }
    }

    /// <summary>
    ///   Returns a new list of all species that have filled a predation miche to eat the provided species.
    /// </summary>
    /// <param name="result">List of species</param>
    /// <param name="micheTree">Miche tree to search</param>
    /// <param name="species">Species to search for Predation miches of</param>
    private void PredatorsOf(List<Species> result, Miche micheTree, Species species)
    {
        result.Clear();

        predatorCalculationMemory.Clear();
        predatorCalculationMemory2.Clear();

        micheTree.GetLeafNodes(predatorCalculationMemory2);

        foreach (var miche in predatorCalculationMemory2)
        {
            if (miche.Pressure is PredationEffectivenessPressure pressure && pressure.Prey == species)
            {
                miche.GetOccupants(predatorCalculationMemory);
            }
        }

        foreach (var predator in predatorCalculationMemory)
        {
            result.Add(predator);
        }
    }

    /// <summary>
    ///   Returns a new list of all possible species that might emerge in response to the provided pressures,
    ///   as well as a copy of the original species.
    /// </summary>
    /// <returns>List of viable variants, and the provided species</returns>
    private List<MicrobeSpecies> GenerateMutations(MicrobeSpecies baseSpecies, int amount,
        List<SelectionPressure> selectionPressures, Random random)
    {
        float totalMP = 100 * worldSettings.AIMutationMultiplier;

        temporaryMutations1.Clear();
        temporaryMutations1.Add(Tuple.Create(baseSpecies, totalMP));
        var viableVariants = temporaryMutations1;

        // Auto-evo assumes that the order mutations are applied doesn't matter when it actually can affect
        // results quite a bit. Maybe they should have weights?
        tempMutationStrategies.Clear();

        // Collect and shuffle unique strategies
        foreach (var selectionPressure in selectionPressures)
        {
            foreach (var mutation in selectionPressure.Mutations)
            {
                if (!tempMutationStrategies.Contains(mutation))
                    tempMutationStrategies.Add(mutation);
            }
        }

        mutationSorter.Setup(baseSpecies, selectionPressures);

        tempMutationStrategies.Shuffle(random);

        foreach (var mutationStrategy in tempMutationStrategies)
        {
            var inputSpecies = viableVariants;

            for (int i = 0; i < Constants.AUTO_EVO_MAX_MUTATION_RECURSIONS; i++)
            {
                temporaryMutations2.Clear();

                foreach (var speciesTuple in inputSpecies)
                {
                    var mutated = mutationStrategy.MutationsOf(speciesTuple.Item1, speciesTuple.Item2);

                    PruneMutations(temporaryMutations2, speciesTuple.Item1, mutated, patch, cache,
                        selectionPressures);
                }

                // TODO: Make these a performance setting?
                if (temporaryMutations2.Count > Constants.MAX_VARIANTS_PER_MUTATION)
                {
                    GetTopMutations(temporaryResultForTopMutations, temporaryMutations2,
                        Constants.MAX_VARIANTS_PER_MUTATION / 2, mutationSorter);

                    // TODO: switch to a set of rotating buffers to avoid memory allocations here
                    inputSpecies = temporaryResultForTopMutations.ToList();
                }
                else
                {
                    // TODO: switch to a set of rotating buffers to avoid memory allocations here
                    inputSpecies = new List<Tuple<MicrobeSpecies, float>>();
                    PruneMutations(inputSpecies, baseSpecies, temporaryMutations2, patch, cache, selectionPressures);
                }

                viableVariants.AddRange(inputSpecies);

                if (viableVariants.Count > Constants.MAX_VARIANTS_IN_MUTATIONS)
                {
                    GetTopMutations(temporaryResultForTopMutations, viableVariants,
                        Constants.MAX_VARIANTS_IN_MUTATIONS / 2, mutationSorter);

                    // TODO: switch to a set of rotating buffers to avoid memory allocations here
                    viableVariants = temporaryResultForTopMutations.ToList();
                }

                if (temporaryMutations2.Count == 0)
                    break;

                if (!mutationStrategy.Repeatable)
                    break;
            }
        }

        lastGeneratedMutations.Clear();

        GetTopMutations(temporaryResultForTopMutations, viableVariants, amount, mutationSorter);
        foreach (var topMutation in temporaryResultForTopMutations)
        {
            lastGeneratedMutations.Add(topMutation.Item1);
        }

        // TODO: could maybe optimize things by only giving name and colour changes for mutations that are selected
        // in the end
        foreach (var variant in lastGeneratedMutations)
        {
            if (variant == baseSpecies)
                continue;

            MutationLogicFunctions.NameNewMicrobeSpecies(variant, baseSpecies);

            var oldColour = variant.Colour;

            var redShift = (random.NextDouble() - 0.5f) * Constants.AUTO_EVO_COLOR_CHANGE_MAX_STEP;
            var greenShift = (random.NextDouble() - 0.5f) * Constants.AUTO_EVO_COLOR_CHANGE_MAX_STEP;
            var blueShift = (random.NextDouble() - 0.5f) * Constants.AUTO_EVO_COLOR_CHANGE_MAX_STEP;

            variant.Colour = new Color(Mathf.Clamp((float)(oldColour.R + redShift), 0, 1),
                Mathf.Clamp((float)(oldColour.G + greenShift), 0, 1),
                Mathf.Clamp((float)(oldColour.B + blueShift), 0, 1));
        }

        return lastGeneratedMutations;
    }

    private class MutationSorter(Patch patch, SimulationCache cache) : IComparer<Tuple<MicrobeSpecies, float>>
    {
        // This isn't the cleanest but this class is just optimized for performance so if someone forgets to set up
        // this then bad things will happen
        private List<SelectionPressure> pressures = null!;
        private MicrobeSpecies baseSpecies = null!;

        public void Setup(MicrobeSpecies species, List<SelectionPressure> selectionPressures)
        {
            pressures = selectionPressures;
            baseSpecies = species;
        }

        public int Compare(Tuple<MicrobeSpecies, float>? x, Tuple<MicrobeSpecies, float>? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (y is null)
                return 1;
            if (x is null)
                return -1;

            var strengthX = 0.0f;
            var strengthY = 0.0f;

            foreach (var pressure in pressures)
            {
                strengthX += cache.GetPressureScore(pressure, patch, x.Item1) /
                    cache.GetPressureScore(pressure, patch, baseSpecies) * pressure.Strength;

                strengthY += cache.GetPressureScore(pressure, patch, y.Item1) /
                    cache.GetPressureScore(pressure, patch, baseSpecies) * pressure.Strength;
            }

            if (strengthX > strengthY)
                return 1;

            if (strengthY > strengthX)
                return -1;

            // Second float in tuple is apparently compared in ascending order
            // TODO: switch to named tuples to figure out what is going on here
            if (x.Item2 > y.Item2)
                return -1;

            if (x.Item2 < y.Item2)
                return 1;

            return 0;
        }
    }
}
