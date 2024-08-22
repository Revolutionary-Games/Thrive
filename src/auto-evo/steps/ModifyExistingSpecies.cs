namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Xoshiro.PRNG64;
using Mutation = System.Tuple<MicrobeSpecies, MicrobeSpecies, RunResults.NewSpeciesType>;

/// <summary>
///   Uses miches to create a mutation for an existing species. Also creates new species from existing ones as
///   mutations.
/// </summary>
public class ModifyExistingSpecies : IRunStep
{
    // TODO: Possibly make this a performance setting?
    private const int TotalMutationsToTry = 20;

    private readonly Patch patch;
    private readonly SimulationCache cache;

    private readonly WorldGenerationSettings worldSettings;

    private readonly HashSet<Species> speciesWorkMemory = new();
    private readonly HashSet<Species> newOccupantsWorkMemory = new();

    // TODO: this class was originally written with a bunch of LINQ and temporary memory allocations. This has been
    // converted to use persistent memory to avoid allocations but maybe the algorithm should be reworked with the
    // new constraints in mind to get rid of the mess of fields

    private readonly HashSet<Species> workMemory = new();

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
    private readonly Random random;

    private readonly List<Mutation> mutationsToTry = new();
    private readonly HashSet<MicrobeSpecies> handledMutations = new();

    private readonly List<Miche> speciesSpecificLeaves = new();

    private readonly int expectedSpeciesCount;

    private readonly List<Miche> nonEmptyLeafNodes = new();
    private readonly List<Miche> emptyLeafNodes = new();

    private Dictionary<Species, long>.Enumerator speciesEnumerator;

    private Miche? miche;

    private Step step;

    public ModifyExistingSpecies(Patch patch, SimulationCache cache, WorldGenerationSettings worldSettings,
        Random randomSeed)
    {
        this.patch = patch;
        this.cache = cache;
        this.worldSettings = worldSettings;

        mutationSorter = new MutationSorter(patch, cache);

        random = new XoShiRo256starstar(randomSeed.NextInt64());

        // Patch species count is used to know how many steps there are to perform
        expectedSpeciesCount = patch.SpeciesInPatch.Count;
        speciesEnumerator = patch.SpeciesInPatch.GetEnumerator();
    }

    private enum Step
    {
        /// <summary>
        ///   This runs once for each species (plus one base case)
        /// </summary>
        Mutations,

        MutationFilter,

        MutationTest,

        FinalApply,
    }

    /// <summary>
    ///   See <see cref="Step"/> for explanation on the step count
    /// </summary>
    public int TotalSteps => 4 + expectedSpeciesCount;

    public bool CanRunConcurrently => true;

    public bool RunStep(RunResults results)
    {
        // Setup miche data if missing
        if (miche == null)
        {
            miche = results.GetMicheForPatch(patch);

            miche.GetOccupants(speciesWorkMemory);

            miche.GetLeafNodes(nonEmptyLeafNodes, emptyLeafNodes, x => x.Occupant != null);
        }

        // This auto-evo step is split into sub steps so that each run doesn't take many seconds like it would
        // otherwise
        switch (step)
        {
            case Step.Mutations:
            {
                // For each existing species, add adaptations based on the existing pressures

                // Process mutations for one species per step
                if (speciesEnumerator.MoveNext())
                {
                    var species = speciesEnumerator.Current.Key;

                    if (species is MicrobeSpecies microbeSpecies)
                    {
                        GetMutationsForSpecies(microbeSpecies);
                    }
                }
                else
                {
                    // All mutations generated, next is the step to try them
                    step = Step.MutationFilter;

                    // Reset this for another enumeration later
                    speciesEnumerator = patch.SpeciesInPatch.GetEnumerator();

                    // Just for safety generate any mutations still missing in case the miche data and species in patch
                    // are not in sync
                    foreach (var species in speciesWorkMemory)
                    {
                        if (patch.SpeciesInPatch.ContainsKey(species))
                            continue;

                        // This is really only a problem as this doesn't allow the splitting into steps to work well
                        GD.PrintErr("Miche tree has a species not in the patch populations, still calculating " +
                            "mutations for it");

                        if (species is MicrobeSpecies microbeSpecies)
                        {
                            GetMutationsForSpecies(microbeSpecies);
                        }
                    }
                }

                break;
            }

            case Step.MutationFilter:
            {
                // Disregard "mutations" that result in identical species"
                mutationsToTry.RemoveAll(m => m.Item1 == m.Item2);

                // Then shuffle and take only as many mutations as we want to try
                mutationsToTry.Shuffle(random);

                while (mutationsToTry.Count > TotalMutationsToTry)
                {
                    mutationsToTry.RemoveAt(mutationsToTry.Count - 1);
                }

                foreach (var mutation in mutationsToTry)
                {
                    mutation.Item2.OnEdited();
                }

                step = Step.MutationTest;
                break;
            }

            case Step.MutationTest:
            {
                // Add these mutant species into a new miche to test them
                foreach (var mutation in mutationsToTry)
                {
                    miche.InsertSpecies(mutation.Item2, patch, null, cache, false, workMemory);
                }

                newOccupantsWorkMemory.Clear();
                miche.GetOccupants(newOccupantsWorkMemory);

                step = Step.FinalApply;
                break;
            }

            case Step.FinalApply:
            {
                foreach (var mutation in mutationsToTry)
                {
                    // Before adding the results for the species we verify the mutations were not overridden in the
                    // miche tree by a better mutation. This prevents species from instantly going extinct.
                    if (!newOccupantsWorkMemory.Contains(mutation.Item2) || !handledMutations.Add(mutation.Item2))
                        continue;

                    var newPopulation =
                        MichePopulation.CalculateMicrobePopulationInPatch(mutation.Item2, miche!, patch,
                            cache);

                    if (newPopulation > Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION)
                    {
                        results.AddPossibleMutation(mutation.Item2,
                            [new KeyValuePair<Patch, long>(patch, newPopulation)], mutation.Item3, mutation.Item1);
                    }
                }

                return true;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }

        // More steps to run
        return false;
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

    private void GetMutationsForSpecies(MicrobeSpecies microbeSpecies)
    {
        // The traversal end up being re-calculated quite many times, but this way we avoid quite a lot of memory
        // allocations

        foreach (var nonEmptyLeaf in nonEmptyLeafNodes)
        {
            if (nonEmptyLeaf.Occupant == microbeSpecies)
                continue;

            currentTraversal.Clear();
            nonEmptyLeaf.BackTraversal(currentTraversal);

            temporaryPressures.Clear();
            foreach (var traversalMiche in currentTraversal)
            {
                temporaryPressures.Add(traversalMiche.Pressure);
            }

            SpeciesDependentPressures(temporaryPressures, miche!, microbeSpecies);

            var variants = GenerateMutations(microbeSpecies,
                worldSettings.AutoEvoConfiguration.MutationsPerSpecies, temporaryPressures);

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

            SpeciesDependentPressures(temporaryPressures, miche!, microbeSpecies);

            var variants = GenerateMutations(microbeSpecies,
                worldSettings.AutoEvoConfiguration.MutationsPerSpecies, temporaryPressures);

            foreach (var variant in variants)
            {
                mutationsToTry.Add(new Mutation(microbeSpecies, variant, RunResults.NewSpeciesType.FillNiche));
            }
        }
    }

    private void SpeciesDependentPressures(List<SelectionPressure> dataReceiver, Miche targetMiche, Species species)
    {
        predatorPressuresTemporary.Clear();

        PredatorsOf(predatorPressuresTemporary, targetMiche, species);

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

        workMemory.Clear();
        predatorCalculationMemory2.Clear();

        micheTree.GetLeafNodes(predatorCalculationMemory2);

        foreach (var potentialMiche in predatorCalculationMemory2)
        {
            if (potentialMiche.Pressure is PredationEffectivenessPressure pressure && pressure.Prey == species)
            {
                potentialMiche.GetOccupants(workMemory);
            }
        }

        foreach (var predator in workMemory)
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
        List<SelectionPressure> selectionPressures)
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
                    // TODO: this seems like the longest part, so splitting this into multiple steps (maybe bundling
                    // up mutation strategies) would be good to have the auto-evo steps flow more smoothly
                    var mutated = mutationStrategy.MutationsOf(speciesTuple.Item1, speciesTuple.Item2);

                    if (mutated != null)
                    {
                        PruneMutations(temporaryMutations2, speciesTuple.Item1, mutated, patch, cache,
                            selectionPressures);
                    }
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
