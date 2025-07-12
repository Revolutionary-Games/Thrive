namespace AutoEvo;

using System;
using System.Collections.Generic;
using Godot;
using Xoshiro.PRNG64;

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

    private readonly Miche.InsertWorkingMemory insertWorkingMemory = new();

    private readonly List<Miche> predatorCalculationMemory2 = new();
    private readonly List<Species> predatorPressuresTemporary = new();

    // TODO: switch to named tuple elements
    private readonly List<Tuple<MicrobeSpecies, double>> temporaryMutations1 = new();
    private readonly List<Tuple<MicrobeSpecies, double>> temporaryMutations2 = new();

    private readonly List<MicrobeSpecies> lastGeneratedMutations = new();

    private readonly Stack<SelectionPressure> pressureStack = new();

    private readonly MutationSorter mutationSorter;
    private readonly GenerateMutationsWorkingMemory generateMutationsWorkingMemory = new();
    private readonly Random random;

    private readonly List<Mutation> mutationsToTry = new();
    private readonly HashSet<MicrobeSpecies> handledMutations = new();

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
                mutationsToTry.RemoveAll(m => m.ParentSpecies == m.MutatedSpecies);

                // Then shuffle and take only as many mutations as we want to try
                mutationsToTry.Shuffle(random);

                while (mutationsToTry.Count > TotalMutationsToTry)
                {
                    mutationsToTry.RemoveAt(mutationsToTry.Count - 1);
                }

                foreach (var mutation in mutationsToTry)
                {
                    mutation.MutatedSpecies.OnEdited();
                }

                step = Step.MutationTest;
                break;
            }

            case Step.MutationTest:
            {
                // Add these mutant species into a new miche to test them
                foreach (var mutation in mutationsToTry)
                {
                    // WARNING: this modifies the miche tree meaning that no other step may be running at the same time
                    // that uses the miche tree for the same patch. And no further auto-evo steps after this can use
                    // the original miche tree state.
                    miche.InsertSpecies(mutation.MutatedSpecies, patch, null, cache, false, insertWorkingMemory);
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
                    // TODO: make sure that the handledMutations contains check here properly detects duplicate
                    // mutations (microbe species equality comparison may not be fully setup for this use case)
                    if (!newOccupantsWorkMemory.Contains(mutation.MutatedSpecies) ||
                        !handledMutations.Add(mutation.MutatedSpecies))
                    {
                        continue;
                    }

                    var newPopulation =
                        MichePopulation.CalculateMicrobePopulationInPatch(mutation.MutatedSpecies, miche!, patch,
                            cache);

                    if (newPopulation > Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION)
                    {
                        results.AddPossibleMutation(mutation.MutatedSpecies,
                            new KeyValuePair<Patch, long>(patch, newPopulation), mutation.AddType,
                            mutation.ParentSpecies);
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

    private static void PruneMutations(List<Tuple<MicrobeSpecies, double>> addResultsTo, MicrobeSpecies baseSpecies,
        List<Tuple<MicrobeSpecies, double>> mutated, Patch patch, SimulationCache cache,
        IEnumerable<SelectionPressure> selectionPressures)
    {
        foreach (var potentialVariant in mutated)
        {
            var combinedScores = 0.0;
            foreach (var pastPressure in selectionPressures)
            {
                // Caching a score for a species very likely to be pruned wastes memory
                var newScore = pastPressure.Score(potentialVariant.Item1, patch, cache);
                var oldScore = cache.GetPressureScore(pastPressure, patch, baseSpecies);

                // Break if the mutation fails a new pressure check
                if (newScore <= 0 && oldScore > 0)
                {
                    combinedScores = -1;
                    break;
                }

                combinedScores += pastPressure.WeightedComparedScores(newScore, oldScore);
            }

            if (combinedScores >= 0)
            {
                addResultsTo.Add(potentialVariant);
            }
        }
    }

    private static void GetTopMutations(List<Tuple<MicrobeSpecies, double>> result,
        List<Tuple<MicrobeSpecies, double>> mutated, int amount, MutationSorter sorter)
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
        double totalMP = 100 * worldSettings.AIMutationMultiplier;

        generateMutationsWorkingMemory.Clear();
        pressureStack.Clear();

        SpeciesDependentPressures(pressureStack, miche!, microbeSpecies);

        var inputSpecies = generateMutationsWorkingMemory.GetMutationsAtDepth(0);
        inputSpecies.Add(Tuple.Create(microbeSpecies, totalMP));

        GenerateMutations(microbeSpecies, miche!, 1);
    }

    private void SpeciesDependentPressures(Stack<SelectionPressure> dataReceiver, Miche targetMiche, Species species)
    {
        predatorPressuresTemporary.Clear();

        PredatorsOf(predatorPressuresTemporary, targetMiche, species);

        foreach (var predator in predatorPressuresTemporary)
        {
            // TODO: Make that weight a constant
            dataReceiver.Push(new AvoidPredationSelectionPressure(predator, 5.0f));
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
    ///   Adds a new list of all possible species that might emerge in response to the provided pressures,
    ///   as well as a copy of the original species to <see cref="mutationsToTry"\>.
    /// </summary>
    private void GenerateMutations(MicrobeSpecies baseSpecies, Miche currentMiche, int depth)
    {
        var inputSpecies = generateMutationsWorkingMemory.GetMutationsAtDepth(depth - 1);

        pressureStack.Push(currentMiche.Pressure);

        var outputSpecies = generateMutationsWorkingMemory.GetMutationsAtDepth(depth);
        outputSpecies.Clear();
        outputSpecies.AddRange(inputSpecies);

        mutationSorter.Setup(baseSpecies, pressureStack);

        var mutations = currentMiche.Pressure.Mutations;
        bool lawk = worldSettings.LAWK;

        temporaryMutations1.Clear();
        temporaryMutations1.AddRange(outputSpecies);

        foreach (var mutationStrategy in mutations)
        {
            // temporaryMutations1.Clear();
            // temporaryMutations1.AddRange(outputSpecies);

            for (int i = 0; i < Constants.AUTO_EVO_MAX_MUTATION_RECURSIONS; ++i)
            {
                temporaryMutations2.Clear();

                foreach (var speciesTuple in temporaryMutations1)
                {
                    // TODO: this seems like the longest part, so splitting this into multiple steps (maybe bundling
                    // up mutation strategies) would be good to have the auto-evo steps flow more smoothly
                    var mutated = mutationStrategy.MutationsOf(speciesTuple.Item1, speciesTuple.Item2, lawk, random,
                        patch.Biome);

                    if (mutated != null)
                    {
                        PruneMutations(temporaryMutations2, speciesTuple.Item1, mutated, patch, cache,
                            pressureStack);
                    }
                }

                // TODO: Make these a performance setting?
                if (temporaryMutations2.Count > Constants.MAX_VARIANTS_PER_MUTATION)
                {
                    GetTopMutations(temporaryMutations1, temporaryMutations2,
                        Constants.MAX_VARIANTS_PER_MUTATION / 2, mutationSorter);
                }
                else
                {
                    temporaryMutations1.Clear();
                    PruneMutations(temporaryMutations1, baseSpecies, temporaryMutations2, patch, cache, pressureStack);
                }

                outputSpecies.AddRange(temporaryMutations1);

                if (outputSpecies.Count > Constants.MAX_VARIANTS_IN_MUTATIONS)
                {
                    GetTopMutations(temporaryMutations2, outputSpecies,
                        Constants.MAX_VARIANTS_IN_MUTATIONS / 2, mutationSorter);

                    outputSpecies.Clear();
                    outputSpecies.AddRange(temporaryMutations2);
                }

                if (temporaryMutations1.Count == 0)
                    break;

                if (!mutationStrategy.Repeatable)
                    break;
            }
        }

        if (currentMiche.IsLeafNode())
        {
            lastGeneratedMutations.Clear();

            GetTopMutations(temporaryMutations1, outputSpecies, worldSettings.AutoEvoConfiguration.MutationsPerSpecies,
                mutationSorter);
            foreach (var topMutation in temporaryMutations1)
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

                variant.Colour = new Color(Math.Clamp((float)(oldColour.R + redShift), 0, 1),
                    Math.Clamp((float)(oldColour.G + greenShift), 0, 1),
                    Math.Clamp((float)(oldColour.B + blueShift), 0, 1));
            }

            var resultType = (currentMiche.Occupant == baseSpecies) ? RunResults.NewSpeciesType.SplitDueToMutation :
                RunResults.NewSpeciesType.FillNiche;

            foreach (var species in lastGeneratedMutations)
            {
                mutationsToTry.Add(new Mutation(baseSpecies, species, resultType));
            }
        }
        else
        {
            foreach (var child in currentMiche.Children)
            {
                GenerateMutations(baseSpecies, child, depth + 1);
            }

            pressureStack.Pop();
        }
    }

    private record struct Mutation(MicrobeSpecies ParentSpecies, MicrobeSpecies MutatedSpecies,
        RunResults.NewSpeciesType AddType);

    /// <summary>
    ///   Working memory used to reduce memory allocations in <see cref="ModifyExistingSpecies.GenerateMutations"/>
    /// </summary>
    private class GenerateMutationsWorkingMemory
    {
        private readonly List<List<Tuple<MicrobeSpecies, double>>> currentSpecies = new();

        public List<Tuple<MicrobeSpecies, double>> GetMutationsAtDepth(int depth)
        {
            while (currentSpecies.Count <= depth)
                currentSpecies.Add(new());

            var result = currentSpecies[depth];

            return result;
        }

        public void Clear()
        {
            foreach (var speciesList in currentSpecies)
            {
                speciesList.Clear();
            }
        }
    }

    private class MutationSorter(Patch patch, SimulationCache cache) : IComparer<Tuple<MicrobeSpecies, double>>
    {
        // This isn't the cleanest but this class is just optimized for performance so if someone forgets to set up
        // this then bad things will happen
        private IEnumerable<SelectionPressure> pressures = null!;
        private MicrobeSpecies baseSpecies = null!;

        public void Setup(MicrobeSpecies species, IEnumerable<SelectionPressure> selectionPressures)
        {
            pressures = selectionPressures;
            baseSpecies = species;
        }

        public int Compare(Tuple<MicrobeSpecies, double>? x, Tuple<MicrobeSpecies, double>? y)
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
                strengthX += pressure.Score(x.Item1, patch, cache) /
                    cache.GetPressureScore(pressure, patch, baseSpecies) * pressure.Weight;

                strengthY += pressure.Score(y.Item1, patch, cache) /
                    cache.GetPressureScore(pressure, patch, baseSpecies) * pressure.Weight;
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
