namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using Xoshiro.PRNG64;
using static CommonMutationFunctions;

/// <summary>
///   Uses miches to create a mutation for an existing species. Also creates new species from existing ones as
///   mutations.
/// </summary>
public class ModifyExistingSpecies : IRunStep
{
    // TODO: Possibly make this a performance setting?
    private const int TotalMutationsToTry = 20;
    private const int FinalMutationPhases = 3;
    private const int InitialMutationStepEstimatePerSpecies = 256;

    private static int autoEvoCacheCounter;

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

    private readonly List<Mutant> temporaryMutations1 = new();
    private readonly List<Mutant> temporaryMutations2 = new();

    private readonly List<MicrobeSpecies> lastGeneratedMutations = new();
    private readonly List<MicrobeSpecies> outOfSyncSpecies = new();

    private readonly Stack<SelectionPressure> pressureStack = new();

    private readonly MutationSorter mutationSorter;
    private readonly GenerateMutationsWorkingMemory generateMutationsWorkingMemory = new();
    private readonly Random random;

    private readonly List<Mutation> mutationsToTry = new();
    private readonly HashSet<MicrobeSpecies> handledMutations = new();

    private readonly List<Miche> nonEmptyLeafNodes = new();
    private readonly List<Miche> emptyLeafNodes = new();

    private Dictionary<Species, long>.Enumerator speciesEnumerator;

    private Miche? miche;
    private SpeciesMutationGenerationState? currentMutationGeneration;

    private Step step;
    private int mutationStepEstimatePerSpecies = InitialMutationStepEstimatePerSpecies;
    private int remainingPatchSpeciesToGenerate;
    private int remainingOutOfSyncSpeciesToGenerate;
    private int potentialOutOfSyncSpeciesToGenerate;
    private bool collectedOutOfSyncSpecies;

    public ModifyExistingSpecies(Patch patch, SimulationCache cache, WorldGenerationSettings worldSettings,
        Random randomSeed)
    {
        this.patch = patch;
        this.cache = cache;
        this.worldSettings = worldSettings;

        mutationSorter = new MutationSorter(patch, cache);

        random = new XoShiRo256starstar(randomSeed.NextInt64());

        foreach (var species in patch.SpeciesInPatch.Keys)
        {
            if (species is MicrobeSpecies)
                ++remainingPatchSpeciesToGenerate;
        }

        SetTotalStepsEstimate(remainingPatchSpeciesToGenerate * mutationStepEstimatePerSpecies + FinalMutationPhases);
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

    private enum MutationGenerationFramePhase
    {
        Initialize,
        ProcessStrategies,
        FinalizeLeaf,
        PrepareChildren,
        ProcessChildren,
    }

    /// <summary>
    ///   See <see cref="Step"/> for explanation on the step count
    /// </summary>
    public int TotalSteps { get; private set; }

    public bool CanRunConcurrently => true;

    public static int GetNextAutoEvoAttemptCacheNumber()
    {
        // This wraps around after 4 billion species tries, which should at most cause a small glitch in auto-evo
        // score caching
        return Interlocked.Increment(ref autoEvoCacheCounter);
    }

    public bool RunStep(RunResults results)
    {
        // Setup miche data if missing
        if (miche == null)
        {
            miche = results.GetMicheForPatch(patch);

            miche.GetOccupants(speciesWorkMemory);

            miche.GetLeafNodes(nonEmptyLeafNodes, emptyLeafNodes, x => x.Occupant != null);

            mutationStepEstimatePerSpecies = CalculateMutationStepEstimatePerSpecies(miche);
            potentialOutOfSyncSpeciesToGenerate = Math.Max(0, speciesWorkMemory.Count - patch.SpeciesInPatch.Count);
            UpdateMutationStepEstimate();
        }

        // This auto-evo step is split into sub steps so that each run doesn't take many seconds like it would
        // otherwise
        switch (step)
        {
            case Step.Mutations:
            {
                if (currentMutationGeneration == null && !TryStartNextMutationGeneration())
                {
                    step = Step.MutationFilter;
                    SetTotalStepsEstimate(FinalMutationPhases);
                    speciesEnumerator = patch.SpeciesInPatch.GetEnumerator();
                }
                else
                {
                    if (currentMutationGeneration != null)
                    {
                        if (currentMutationGeneration.RunStep())
                        {
                            currentMutationGeneration = null;
                        }

                        UpdateMutationStepEstimate();
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

#if DEBUG
                    if (mutation.MutatedSpecies.AutoEvoAttemptCache == 0)
                        throw new Exception("Mutation has no cache number, so missing a call to OnAttemptedInAutoEvo");
#endif
                }

                SetTotalStepsEstimate(2);
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

                SetTotalStepsEstimate(1);
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
                        // Only apply a new name and colour to results that are actually kept
                        MutationLogicFunctions.NameNewMicrobeSpecies(mutation.MutatedSpecies, mutation.ParentSpecies);
                        MutationLogicFunctions.ColourNewMicrobeSpecies(random, mutation.MutatedSpecies,
                            mutation.ParentSpecies);

                        results.AddPossibleMutation(mutation.MutatedSpecies,
                            new KeyValuePair<Patch, long>(patch, newPopulation), mutation.AddType,
                            mutation.ParentSpecies);
                    }
                }

                SetTotalStepsEstimate(0);
                return true;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }

        // More steps to run
        return false;
    }

    private static void PruneMutations(List<Mutant> addResultsTo, MicrobeSpecies baseSpecies,
        List<Mutant> mutated, Patch patch, SimulationCache cache,
        Stack<SelectionPressure> selectionPressures)
    {
        foreach (var potentialVariant in mutated)
        {
            var combinedScores = 0.0;
            foreach (var pastPressure in selectionPressures)
            {
                // Caching a score for a species very likely to be pruned wastes memory
                var newScore = pastPressure.Score(potentialVariant.Species, patch, cache);
                var oldScore = cache.GetPressureScore(pastPressure, patch, baseSpecies);

                // Break if the mutation fails a new pressure check
                if (newScore <= 0 && oldScore > 0)
                {
                    combinedScores = -1;
                    break;
                }

                // Never prune if the mutation succeeds a new pressure check
                // Because the score cannot be compared with a parent that does not fill the same niche
                if (newScore > 0 && oldScore <= 0)
                {
                    combinedScores = 1;
                    break;
                }

                combinedScores += pastPressure.WeightedComparedScores(newScore, oldScore);
            }

            // Not pruning species that don't affect the score can inject more
            // variety into the species generated
            if (combinedScores >= 0)
            {
                addResultsTo.Add(potentialVariant);
            }
        }
    }

    private static void GetTopMutations(List<Mutant> result,
        List<Mutant> mutated, int amount, MutationSorter sorter)
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

    private void AddRandomMutations(List<Mutant> result,
        List<Mutant> mutated, int amount)
    {
        mutated.Shuffle(random);

        foreach (var tuple in mutated)
        {
            result.Add(tuple);
            if (result.Count >= amount)
                break;
        }
    }

    private int CalculateMutationStepEstimatePerSpecies(Miche currentMiche)
    {
        var steps = 2 + currentMiche.Pressure.Mutations.Count * Constants.AUTO_EVO_MAX_MUTATION_RECURSIONS;

        if (currentMiche.IsLeafNode())
            return steps;

        steps += currentMiche.Children.Count + 1;

        foreach (var child in currentMiche.Children)
        {
            steps += CalculateMutationStepEstimatePerSpecies(child);
        }

        return steps;
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

    private bool TryStartNextMutationGeneration()
    {
        while (speciesEnumerator.MoveNext())
        {
            if (speciesEnumerator.Current.Key is not MicrobeSpecies microbeSpecies)
                continue;

            --remainingPatchSpeciesToGenerate;
            currentMutationGeneration = new SpeciesMutationGenerationState(this, microbeSpecies, miche!,
                mutationStepEstimatePerSpecies);
            return true;
        }

        if (!collectedOutOfSyncSpecies)
        {
            collectedOutOfSyncSpecies = true;
            outOfSyncSpecies.Clear();

            foreach (var species in speciesWorkMemory)
            {
                if (patch.SpeciesInPatch.ContainsKey(species))
                    continue;

                GD.PrintErr("Miche tree has a species not in the patch populations, still calculating mutations " +
                    "for it");

                if (species is MicrobeSpecies microbeSpecies)
                {
                    outOfSyncSpecies.Add(microbeSpecies);
                }
            }

            remainingOutOfSyncSpeciesToGenerate = outOfSyncSpecies.Count;
            potentialOutOfSyncSpeciesToGenerate = remainingOutOfSyncSpeciesToGenerate;
        }

        if (remainingOutOfSyncSpeciesToGenerate < 1)
            return false;

        var outOfSyncIndex = outOfSyncSpecies.Count - remainingOutOfSyncSpeciesToGenerate;
        --remainingOutOfSyncSpeciesToGenerate;

        currentMutationGeneration = new SpeciesMutationGenerationState(this, outOfSyncSpecies[outOfSyncIndex], miche!,
            mutationStepEstimatePerSpecies);
        return true;
    }

    private void UpdateMutationStepEstimate()
    {
        var remainingOutOfSyncSpecies = collectedOutOfSyncSpecies ?
            remainingOutOfSyncSpeciesToGenerate :
            potentialOutOfSyncSpeciesToGenerate;

        SetTotalStepsEstimate((currentMutationGeneration?.RemainingStepEstimate ?? 0) +
            (remainingPatchSpeciesToGenerate + remainingOutOfSyncSpecies) * mutationStepEstimatePerSpecies +
            FinalMutationPhases);
    }

    private void SetTotalStepsEstimate(int estimate)
    {
        if (estimate > TotalSteps)
            TotalSteps = estimate;
    }

    private void ProcessMutationRecursionStep(MicrobeSpecies baseSpecies, Mutant baseSpeciesMutant,
        MutationGenerationFrame frame)
    {
        var mutationStrategy = frame.Mutations[frame.MutationStrategyIndex];

        if (!frame.HasActiveStrategy)
        {
            temporaryMutations1.Clear();
            temporaryMutations1.AddRange(frame.OutputSpecies);
            temporaryMutations1.Add(baseSpeciesMutant);
            frame.HasActiveStrategy = true;
            frame.RecursionIndex = 0;
        }

        temporaryMutations2.Clear();

        foreach (var speciesTuple in temporaryMutations1)
        {
            var mutated = mutationStrategy.MutationsOf(speciesTuple.Species, speciesTuple.MP, worldSettings.LAWK,
                random, patch.Biome);

            if (mutated == null)
                continue;

            foreach (var tuple in mutated)
            {
#if DEBUG
                if (tuple.Species.AutoEvoAttemptCache != 0)
                    throw new Exception("Mutation shouldn't have a cache number yet");
#endif

                tuple.Species.OnAttemptedInAutoEvo(true);
            }

            PruneMutations(temporaryMutations2, speciesTuple.Species, mutated, patch, cache, pressureStack);
        }

        temporaryMutations1.Clear();
        PruneMutations(temporaryMutations1, baseSpecies, temporaryMutations2, patch, cache, pressureStack);

        var maxVariants = Constants.MAX_VARIANTS_IN_MUTATIONS;
        var halfMaxVariants = maxVariants / 2;

        if (temporaryMutations1.Count + frame.OutputSpecies.Count > maxVariants)
        {
            PruneMutations(temporaryMutations1, baseSpecies, frame.OutputSpecies, patch, cache, pressureStack);
            GetTopMutations(temporaryMutations2, temporaryMutations1, halfMaxVariants, mutationSorter);

            var remainingVariants = halfMaxVariants - temporaryMutations1.Count;
            if (remainingVariants > 0)
            {
                temporaryMutations1.Clear();
                GetTopMutations(temporaryMutations1, frame.OutputSpecies, remainingVariants, mutationSorter);
                temporaryMutations2.AddRange(temporaryMutations1);
                AddRandomMutations(temporaryMutations2, frame.OutputSpecies, halfMaxVariants);
            }
            else
            {
                AddRandomMutations(temporaryMutations2, temporaryMutations1, halfMaxVariants / 2);
                temporaryMutations1.Clear();
                GetTopMutations(temporaryMutations1, frame.OutputSpecies, halfMaxVariants / 2, mutationSorter);
                temporaryMutations2.AddRange(temporaryMutations1);
            }

            frame.OutputSpecies.Clear();
            frame.OutputSpecies.AddRange(temporaryMutations2);
        }
        else
        {
            frame.OutputSpecies.AddRange(temporaryMutations1);
        }

        ++frame.RecursionIndex;

        if (temporaryMutations1.Count != 0 && mutationStrategy.Repeatable &&
            frame.RecursionIndex < Constants.AUTO_EVO_MAX_MUTATION_RECURSIONS)
        {
            return;
        }

        frame.HasActiveStrategy = false;
        ++frame.MutationStrategyIndex;
    }

    private void FinalizeLeafMutations(MicrobeSpecies baseSpecies, Miche currentMiche, List<Mutant> outputSpecies)
    {
        lastGeneratedMutations.Clear();

        GetTopMutations(temporaryMutations1, outputSpecies, worldSettings.AutoEvoConfiguration.MutationsPerSpecies,
            mutationSorter);
        foreach (var topMutation in temporaryMutations1)
        {
            lastGeneratedMutations.Add(topMutation.Species);
        }

        var resultType = currentMiche.Occupant == baseSpecies ?
            RunResults.NewSpeciesType.SplitDueToMutation :
            RunResults.NewSpeciesType.FillNiche;

        foreach (var species in lastGeneratedMutations)
        {
            mutationsToTry.Add(new Mutation(baseSpecies, species, resultType));
        }
    }

    private void PrepareChildren(MicrobeSpecies baseSpecies, List<Mutant> outputSpecies)
    {
        temporaryMutations1.Clear();
        PruneMutations(temporaryMutations1, baseSpecies, outputSpecies, patch, cache, pressureStack);
        outputSpecies.Clear();
        outputSpecies.AddRange(temporaryMutations1);
    }

    private record struct Mutation(MicrobeSpecies ParentSpecies, MicrobeSpecies MutatedSpecies,
        RunResults.NewSpeciesType AddType);

    private sealed class MutationGenerationFrame
    {
        public readonly Miche Miche;
        public readonly int Depth;
        public readonly bool LastChild;

        public MutationGenerationFramePhase Phase;
        public IMutationStrategy<MicrobeSpecies>[] Mutations = null!;
        public List<Mutant> OutputSpecies = null!;
        public Mutant BaseSpeciesMutant = null!;
        public int MutationStrategyIndex;
        public int RecursionIndex;
        public int ChildIndex;
        public bool HasActiveStrategy;

        public MutationGenerationFrame(Miche miche, int depth, bool lastChild)
        {
            Miche = miche;
            Depth = depth;
            LastChild = lastChild;
        }
    }

    private class SpeciesMutationGenerationState
    {
        private readonly ModifyExistingSpecies owner;
        private readonly MicrobeSpecies baseSpecies;
        private readonly Stack<MutationGenerationFrame> frameStack = new();

        public SpeciesMutationGenerationState(ModifyExistingSpecies owner, MicrobeSpecies baseSpecies, Miche miche,
            int stepEstimate)
        {
            this.owner = owner;
            this.baseSpecies = baseSpecies;

            owner.generateMutationsWorkingMemory.Clear();
            owner.pressureStack.Clear();

            var inputSpecies = owner.generateMutationsWorkingMemory.GetMutationsAtDepth(0);
            inputSpecies.Add(new Mutant(baseSpecies,
                Constants.BASE_MUTATION_POINTS * owner.worldSettings.AIMutationMultiplier));

            frameStack.Push(new MutationGenerationFrame(miche, 1, false));
            RemainingStepEstimate = stepEstimate;
        }

        public int RemainingStepEstimate { get; private set; }

        public bool RunStep()
        {
            if (RemainingStepEstimate > 0)
                --RemainingStepEstimate;

            var currentFrame = frameStack.Peek();

            switch (currentFrame.Phase)
            {
                case MutationGenerationFramePhase.Initialize:
                {
                    var inputSpecies = owner.generateMutationsWorkingMemory.GetMutationsAtDepth(currentFrame.Depth - 1);

                    currentFrame.OutputSpecies =
                        owner.generateMutationsWorkingMemory.GetMutationsAtDepth(currentFrame.Depth);
                    currentFrame.OutputSpecies.Clear();
                    currentFrame.OutputSpecies.AddRange(inputSpecies);
                    currentFrame.BaseSpeciesMutant = new Mutant(baseSpecies,
                        Constants.BASE_MUTATION_POINTS * owner.worldSettings.AIMutationMultiplier);

                    owner.pressureStack.Push(currentFrame.Miche.Pressure);
                    owner.mutationSorter.Setup(baseSpecies, owner.pressureStack);

                    currentFrame.Mutations = currentFrame.Miche.Pressure.Mutations.ToArray();
                    currentFrame.Mutations.Shuffle(owner.random);
                    currentFrame.Phase = MutationGenerationFramePhase.ProcessStrategies;
                    break;
                }

                case MutationGenerationFramePhase.ProcessStrategies:
                {
                    if (currentFrame.MutationStrategyIndex >= currentFrame.Mutations.Length)
                    {
                        currentFrame.Phase = currentFrame.Miche.IsLeafNode() ?
                            MutationGenerationFramePhase.FinalizeLeaf :
                            MutationGenerationFramePhase.PrepareChildren;
                        break;
                    }

                    owner.ProcessMutationRecursionStep(baseSpecies, currentFrame.BaseSpeciesMutant, currentFrame);
                    break;
                }

                case MutationGenerationFramePhase.FinalizeLeaf:
                {
                    owner.FinalizeLeafMutations(baseSpecies, currentFrame.Miche, currentFrame.OutputSpecies);
                    FinishCurrentFrame();
                    break;
                }

                case MutationGenerationFramePhase.PrepareChildren:
                {
                    owner.PrepareChildren(baseSpecies, currentFrame.OutputSpecies);
                    currentFrame.Phase = MutationGenerationFramePhase.ProcessChildren;
                    break;
                }

                case MutationGenerationFramePhase.ProcessChildren:
                {
                    if (currentFrame.OutputSpecies.Count == 0 ||
                        currentFrame.ChildIndex >= currentFrame.Miche.Children.Count)
                    {
                        FinishCurrentFrame();
                        break;
                    }

                    var child = currentFrame.Miche.Children[currentFrame.ChildIndex];
                    var isLastChild = currentFrame.ChildIndex == currentFrame.Miche.Children.Count - 1;
                    ++currentFrame.ChildIndex;
                    frameStack.Push(new MutationGenerationFrame(child, currentFrame.Depth + 1, isLastChild));
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return frameStack.Count == 0;
        }

        private void FinishCurrentFrame()
        {
            var finishedFrame = frameStack.Pop();
            owner.pressureStack.Pop();

            if (finishedFrame.LastChild)
            {
                owner.generateMutationsWorkingMemory.ClearDepth(finishedFrame.Depth - 1);
            }
        }
    }

    /// <summary>
    ///   Working memory used to reduce memory allocations in <see cref="GenerateMutations"/>.
    /// </summary>
    private class GenerateMutationsWorkingMemory
    {
        private readonly List<List<Mutant>> currentSpecies = new();

        public List<Mutant> GetMutationsAtDepth(int depth)
        {
            while (currentSpecies.Count <= depth)
                currentSpecies.Add(new List<Mutant>());

            var result = currentSpecies[depth];

            return result;
        }

        public void ClearDepth(int depth)
        {
            currentSpecies[depth].Clear();
        }

        public void Clear()
        {
            foreach (var speciesList in currentSpecies)
            {
                speciesList.Clear();
            }
        }
    }

    private class MutationSorter(Patch patch, SimulationCache cache) : IComparer<Mutant>
    {
        // This isn't the cleanest, but this class is just optimized for performance, so if someone forgets to set up
        // this, then bad things will happen

        // This directly references to the stack type to avoid an enumerator allocation in the foreach loop in Compare
        private Stack<SelectionPressure> pressures = null!;
        private MicrobeSpecies baseSpecies = null!;

        public void Setup(MicrobeSpecies species, Stack<SelectionPressure> selectionPressures)
        {
            pressures = selectionPressures;
            baseSpecies = species;
        }

        public int Compare(Mutant? x, Mutant? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (y is null)
                return -1;
            if (x is null)
                return 1;

            var strengthX = 0.0f;
            var strengthY = 0.0f;

            foreach (var pressure in pressures)
            {
                strengthX += cache.GetPressureScore(pressure, patch, x.Species) /
                    cache.GetPressureScore(pressure, patch, baseSpecies) * pressure.Weight;

                strengthY += cache.GetPressureScore(pressure, patch, y.Species) /
                    cache.GetPressureScore(pressure, patch, baseSpecies) * pressure.Weight;
            }

            if (strengthX > strengthY)
                return -1;

            if (strengthY > strengthX)
                return 1;

            if (x.MP > y.MP)
                return -1;

            if (x.MP < y.MP)
                return 1;

            return 0;
        }
    }
}
