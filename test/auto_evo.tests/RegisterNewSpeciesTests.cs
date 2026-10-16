using System;
using System.Collections.Generic;
using AutoEvo;
using GdUnit4;
using SharedBase.Archive;
using Xoshiro.PRNG64;
using static GdUnit4.Assertions;

/// <summary>
///   Checks that final miche occupants use the identities selected during species registration.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class RegisterNewSpeciesTests
{
    /// <summary>
    ///   Runs a selected same-stage mutation through population calculation, registration and application, checking
    ///   that the parent keeps its identity while receiving the candidate's population and phenotype. Preserves the
    ///   previous population and recorded energy and cost so applying the mutation cannot overwrite historical results.
    /// </summary>
    [TestCase]
    public void SelectedParentMutationKeepsItsResultIdentityThroughApply()
    {
        var world = CreateWorld();
        var settings = world.WorldSettings;
        var patch = world.Map.CurrentPatch!;
        var parent = CreateParent(world);
        patch.AddSpecies(parent, 100);

        var cache = new SimulationCache(settings);
        var leaf = new Miche(new MutationPressure(1000000)) { Occupant = parent };
        Species? candidate = null;
        long candidatePopulation = 0;
        float recordedEnergy = 0;
        float recordedCost = 0;
        var run = new ControlledRun(world,
            new ActionStep(results => results.AddNewMicheForPatch(patch, leaf)),
            new ModifyExistingSpecies(patch, cache, settings, new XoShiRo256starstar(7233)),
            new ActionStep(results =>
            {
                candidate = leaf.Occupant;
                AssertThat(candidate).IsNotNull().IsNotSame(parent);
                AssertThat(candidate!.ID).IsEqual(parent.ID);
                AssertThat(results.GetPossibleSpeciesList().Count).IsEqual(1);
                var accepted = results.GetPossibleSpeciesList()[0];
                AssertThat(accepted.Species).IsSame(candidate);
                AssertThat(accepted.ParentSpecies).IsSame(parent);
                candidatePopulation = accepted.InitialPopulationInPatches.Value;
                AssertThat(candidatePopulation > Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION).IsTrue();
            }),
            new CalculatePopulation(settings.AutoEvoConfiguration, settings, world.Map, null, true),
            new ActionStep(results =>
            {
                AssertThat(results.GetPopulationInPatch(parent, patch)).IsEqual(0L);
                var energy = results.GetPatchEnergyResults(parent)[patch];
                recordedEnergy = energy.TotalEnergyGathered;
                recordedCost = energy.IndividualCost;
            }),
            new RegisterNewSpecies(world, [parent]));

        for (var step = 0; step < 40 && !run.Finished; ++step)
            run.OneStep();

        AssertThat(run.WasSuccessful).IsTrue();
        var final = run.Results!;
        AssertThat(final.GetSpeciesResultForInternalUse(parent).MutatedProperties).IsSame(candidate);
        AssertThat(final.SpeciesHasResults(candidate!)).IsFalse();
        AssertThat(final.GetPopulationInPatch(parent, patch)).IsEqual(candidatePopulation);
        AssertThat(parent.MembraneRigidity).IsEqual(0.0f);
        AssertThat(leaf.Occupant).IsSame(parent);
        AssertThat(final.GetReadOnlyMicheForPatch(patch).Occupant).IsSame(parent);

        run.CalculateAndApplyFinalExternalEffectSizes();
        final.RegisterNewSpeciesForSummary(world);
        final.StorePreviousPopulations(world.Map);
        AssertThat(final.MakeSummary().ToString().Contains(parent.FormattedName)).IsTrue();
        AssertThat(leaf.Occupant).IsSame(world.Species[parent.ID]);
        run.ApplyAllResults(true);

        AssertThat(leaf.Occupant).IsSame(parent);
        AssertThat(world.Species[parent.ID]).IsSame(parent);
        AssertThat(patch.GetSpeciesSimulationPopulation(parent)).IsEqual(candidatePopulation);
        AssertThat(parent.MembraneRigidity).IsEqual(((MicrobeSpecies)candidate!).MembraneRigidity);
        AssertThat(final.GetSpeciesResultForInternalUse(parent).MutatedProperties).IsSame(candidate);
        AssertThat(final.GetSpeciesResultForInternalUse(parent).OldPopulationInPatches[patch]).IsEqual(100L);
        var tracked = final.GetPatchEnergyResults(parent)[patch];
        AssertThat(tracked.TotalEnergyGathered).IsEqual(recordedEnergy);
        AssertThat(tracked.IndividualCost).IsEqual(recordedCost);

        // The recorded competition used the parent's old phenotype, before the candidate population boost.
        var finalCost = MichePopulation.CalculateIndividualCost(parent, patch.Biome, new SimulationCache(settings));
        AssertThat(finalCost > tracked.IndividualCost).IsTrue();
    }

    /// <summary>
    ///   Checks both candidate orders so only the winning reference is rebound to its parent across nested and separate
    ///   patch miches. This protects clones with matching IDs and names, other occupants and read-only snapshots
    ///   from unintended rebinding, while preserving recorded energy and adding the winning population to the parent.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void OnlyFinalSelectedReferenceIsReboundAcrossAllPatches(bool winnerFirst)
    {
        var world = CreateWorld(2);
        var patches = new List<Patch>(world.Map.Patches.Values);
        var firstPatch = patches[0];
        var secondPatch = patches[1];
        var parent = CreateParent(world);
        var selected = (MicrobeSpecies)parent.Clone();
        selected.MembraneRigidity = 0.25f;
        var split = (MicrobeSpecies)parent.Clone();
        var unrelated = world.NewMicrobeSpecies("Test", "unrelated");
        AssertThat(selected).IsNotSame(split);
        AssertThat(selected.ID).IsEqual(split.ID);
        AssertThat(selected.FormattedName).IsEqual(split.FormattedName);

        var pressure = new MutationPressure(1000);
        var root = new Miche(pressure);
        var branch = new Miche(pressure);
        var firstLeaf = new Miche(pressure) { Occupant = selected };
        var secondLeaf = new Miche(pressure) { Occupant = selected };
        var splitLeaf = new Miche(pressure) { Occupant = split };
        var unrelatedLeaf = new Miche(pressure) { Occupant = unrelated };
        var emptyLeaf = new Miche(pressure);
        branch.AddChild(firstLeaf);
        branch.AddChild(secondLeaf);
        root.AddChild(branch);
        root.AddChild(splitLeaf);
        root.AddChild(unrelatedLeaf);
        root.AddChild(emptyLeaf);
        var otherPatchLeaf = new Miche(pressure) { Occupant = selected };
        var results = new RunResults();
        results.AddNewMicheForPatch(firstPatch, root);
        results.AddNewMicheForPatch(secondPatch, otherPatchLeaf);

        // Controlled pre-registration results distinguish reference mapping from population/energy recalculation.
        results.AddPopulationResultForSpecies(parent, firstPatch, 11);
        results.AddPopulationResultForSpecies(parent, secondPatch, 17);
        results.AddTrackedEnergyForSpecies(parent, firstPatch, pressure, 2, 5, 40, 100);
        results.AddTrackedEnergyConsumptionForSpecies(parent, firstPatch, 40, 2);
        if (winnerFirst)
        {
            results.AddPossibleMutation(selected, new(secondPatch, 50), RunResults.NewSpeciesType.SplitDueToMutation,
                parent);
        }

        results.AddPossibleMutation(split, new(firstPatch, 30), RunResults.NewSpeciesType.SplitDueToMutation, parent);
        if (!winnerFirst)
        {
            results.AddPossibleMutation(selected, new(secondPatch, 50), RunResults.NewSpeciesType.SplitDueToMutation,
                parent);
        }

        AssertThat(new RegisterNewSpecies(world, [parent, unrelated]).RunStep(results)).IsTrue();

        AssertThat(firstLeaf.Occupant).IsSame(parent);
        AssertThat(secondLeaf.Occupant).IsSame(parent);
        AssertThat(otherPatchLeaf.Occupant).IsSame(parent);
        AssertThat(splitLeaf.Occupant).IsSame(split);
        AssertThat(unrelatedLeaf.Occupant).IsSame(unrelated);
        AssertThat(emptyLeaf.Occupant).IsNull();
        AssertThat(firstLeaf.Pressure).IsSame(pressure);
        AssertThat(branch.Pressure).IsSame(pressure);
        AssertThat(branch.Occupant).IsNull();
        AssertThat(selected.MembraneRigidity).IsEqual(0.25f);
        AssertThat(parent.MembraneRigidity).IsEqual(0.0f);
        AssertThat(results.GetSpeciesResultForInternalUse(parent).MutatedProperties).IsSame(selected);
        AssertThat(results.SpeciesHasResults(selected)).IsFalse();
        AssertThat(results.SpeciesHasResults(split)).IsTrue();
        AssertThat(results.GetSpeciesResultForInternalUse(split).SplitFrom).IsSame(parent);
        AssertThat(results.GetPopulationInPatch(split, firstPatch)).IsEqual(30L);
        AssertThat(results.GetPopulationInPatch(parent, firstPatch)).IsEqual(11L);
        AssertThat(results.GetPopulationInPatch(parent, secondPatch)).IsEqual(67L);
        AssertThat(results.GetPossibleSpeciesList().Count).IsEqual(2);

        var tracked = results.GetPatchEnergyResults(parent)[firstPatch];
        AssertThat(tracked.TotalEnergyGathered).IsEqual(40.0f);
        AssertThat(tracked.IndividualCost).IsEqual(2.0f);
        AssertThat(tracked.PerNicheEnergy.Count).IsEqual(1);
        foreach (var niche in tracked.PerNicheEnergy.Values)
        {
            AssertThat(niche.CurrentSpeciesFitness).IsEqual(2.0f);
            AssertThat(niche.TotalFitness).IsEqual(5.0f);
            AssertThat(niche.CurrentSpeciesEnergy).IsEqual(40.0f);
            AssertThat(niche.TotalAvailableEnergy).IsEqual(100.0f);
        }

        // These snapshots deliberately contain the selected candidate, so scanning readonly trees would fail.
        var readonlyLeaves = new List<Miche>();
        results.GetReadOnlyMicheForPatch(firstPatch).GetLeafNodes(readonlyLeaves);
        AssertThat(readonlyLeaves[0].Occupant).IsSame(selected);
        AssertThat(readonlyLeaves[1].Occupant).IsSame(selected);
        AssertThat(readonlyLeaves[2].Occupant).IsSame(split);
        AssertThat(results.GetReadOnlyMicheForPatch(secondPatch).Occupant).IsSame(selected);

        results.RegisterNewSpeciesForSummary(world);
        AssertThat(split.ID == parent.ID).IsFalse();
        AssertThat(splitLeaf.Occupant).IsSame(world.Species[split.ID]);
        AssertThat(firstLeaf.Occupant).IsSame(world.Species[parent.ID]);
    }

    /// <summary>
    ///   Keeps a candidate as a separate split species with its own population when its parent still occupies a miche
    ///   in another patch, preventing a local replacement from being mistaken for extinction of the parent.
    /// </summary>
    [TestCase]
    public void ParentSurvivingInAnotherPatchKeepsCandidateAsNewSpecies()
    {
        var world = CreateWorld(2);
        var patches = new List<Patch>(world.Map.Patches.Values);
        var parent = CreateParent(world);
        var candidate = (MicrobeSpecies)parent.Clone();
        var candidateLeaf = new Miche(new MutationPressure(1000)) { Occupant = candidate };
        var survivingLeaf = new Miche(new MutationPressure(1000)) { Occupant = parent };
        var results = new RunResults();
        results.AddNewMicheForPatch(patches[0], candidateLeaf);
        results.AddNewMicheForPatch(patches[1], survivingLeaf);
        results.AddPopulationResultForSpecies(parent, patches[0], 0);
        results.AddPopulationResultForSpecies(parent, patches[1], 100);
        results.AddPossibleMutation(candidate, new(patches[0], 50), RunResults.NewSpeciesType.SplitDueToMutation,
            parent);

        AssertThat(new RegisterNewSpecies(world, [parent]).RunStep(results)).IsTrue();

        AssertThat(candidateLeaf.Occupant).IsSame(candidate);
        AssertThat(survivingLeaf.Occupant).IsSame(parent);
        AssertThat(results.GetSpeciesResultForInternalUse(parent).MutatedProperties).IsNull();
        AssertThat(results.SpeciesHasResults(candidate)).IsTrue();
        AssertThat(results.GetSpeciesResultForInternalUse(candidate).SplitFrom).IsSame(parent);
        AssertThat(results.GetPopulationInPatch(parent, patches[1])).IsEqual(100L);
        AssertThat(results.GetPopulationInPatch(candidate, patches[0])).IsEqual(50L);
    }

    /// <summary>
    ///   Records the best candidate under its identity with the parent's remaining population when it changes stage,
    ///   while keeping the weaker same-stage candidate separate. This prevents either candidate from being incorrectly
    ///   rebound to the parent or applied as its mutation during registration.
    /// </summary>
    [TestCase]
    public void BestCrossStageCandidateKeepsItsOwnIdentity()
    {
        var world = CreateWorld();
        var patch = world.Map.CurrentPatch!;
        var parent = CreateParent(world);
        parent.IsBacteria = false;
        var candidate = parent.GenerateMulticellularVersion(false, [], [], [], true);
        candidate.OnEdited();
        var sameStage = (MicrobeSpecies)parent.Clone();
        var root = new Miche(new MutationPressure(1000));
        var crossStageLeaf = new Miche(root.Pressure) { Occupant = candidate };
        var sameStageLeaf = new Miche(root.Pressure) { Occupant = sameStage };
        root.AddChild(crossStageLeaf);
        root.AddChild(sameStageLeaf);
        var results = new RunResults();
        results.AddNewMicheForPatch(patch, root);
        results.AddPopulationResultForSpecies(parent, patch, 13);
        results.AddPossibleMutation(sameStage, new(patch, 30), RunResults.NewSpeciesType.SplitDueToMutation, parent);
        results.AddPossibleMutation(candidate, new(patch, 50), RunResults.NewSpeciesType.SplitDueToMutation, parent);

        AssertThat(new RegisterNewSpecies(world, [parent]).RunStep(results)).IsTrue();

        AssertThat(crossStageLeaf.Occupant).IsSame(candidate);
        AssertThat(sameStageLeaf.Occupant).IsSame(sameStage);
        AssertThat(results.GetSpeciesResultForInternalUse(parent).MutatedProperties).IsNull();
        AssertThat(results.SpeciesHasResults(candidate)).IsTrue();
        AssertThat(results.SpeciesHasResults(sameStage)).IsTrue();
        AssertThat(results.GetPopulationInPatch(candidate, patch)).IsEqual(63L);
        AssertThat(results.GetPopulationInPatch(sameStage, patch)).IsEqual(30L);
    }

    /// <summary>
    ///   Removes a species from the world and patch when it loses its last miche without a replacement, while retaining
    ///   its previous population in the results and its name in the summary. This keeps historical results from being
    ///   discarded when extinction is applied to the live world.
    /// </summary>
    [TestCase]
    public void ExtinctSpeciesRetainsItsHistoricalResultAfterApply()
    {
        var world = CreateWorld();
        var patch = world.Map.CurrentPatch!;
        var parent = CreateParent(world);
        patch.AddSpecies(parent, 100);
        var emptyLeaf = new Miche(new MutationPressure(1000));
        var run = new ControlledRun(world,
            new ActionStep(results => results.AddNewMicheForPatch(patch, emptyLeaf)),
            new CalculatePopulation(world.WorldSettings.AutoEvoConfiguration, world.WorldSettings, world.Map),
            new RegisterNewSpecies(world, [parent]));
        for (var step = 0; step < 40 && !run.Finished; ++step)
            run.OneStep();
        AssertThat(run.WasSuccessful).IsTrue();
        var results = run.Results!;
        AssertThat(results.GetPossibleSpeciesList()).IsEmpty();
        AssertThat(results.GetPopulationInPatch(parent, patch)).IsEqual(0L);
        run.CalculateAndApplyFinalExternalEffectSizes();
        results.RegisterNewSpeciesForSummary(world);
        results.StorePreviousPopulations(world.Map);
        run.ApplyAllResults(true);

        AssertThat(emptyLeaf.Occupant).IsNull();
        AssertThat(world.Species.ContainsKey(parent.ID)).IsFalse();
        AssertThat(patch.SpeciesInPatch.ContainsKey(parent)).IsFalse();
        AssertThat(results.SpeciesHasResults(parent)).IsTrue();
        AssertThat(results.GetSpeciesResultForInternalUse(parent).OldPopulationInPatches[patch]).IsEqual(100L);
        AssertThat(results.MakeSummary().ToString().Contains(parent.FormattedName)).IsTrue();
    }

    private static MicrobeSpecies CreateParent(GameWorld world)
    {
        var parent = world.NewMicrobeSpecies("Test", "parent");
        parent.ApplyMutation(world.PlayerSpecies);
        parent.MembraneRigidity = 0;
        parent.OnEdited();
        return parent;
    }

    private static GameWorld CreateWorld(int patchCount = 1)
    {
        var settings = new WorldGenerationSettings
        {
            Seed = 7233,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
            AutoEvoConfiguration = new AutoEvoConfiguration { MutationsPerSpecies = 1, StrictNicheCompetition = false },
        };
        var world = new GameWorld(settings);
        var patch = world.Map.CurrentPatch!;
        var patches = new List<Patch> { patch };
        foreach (var otherPatch in world.Map.Patches.Values)
        {
            if (patches.Count >= patchCount)
                break;

            if (otherPatch != patch)
                patches.Add(otherPatch);
        }

        AssertThat(patches.Count).IsEqual(patchCount);
        world.Map.Patches.Clear();
        foreach (var selected in patches)
        {
            world.Map.Patches.Add(selected.ID, selected);
            selected.SpeciesInPatch.Clear();
        }

        return world;
    }

    private sealed class ControlledRun(GameWorld world, params IRunStep[] steps)
        : AutoEvoRun(world, world.AutoEvoGlobalCache)
    {
        protected override void GatherInfo(Queue<IRunStep> destination)
        {
            foreach (var step in steps)
                destination.Enqueue(step);
        }
    }

    private sealed class ActionStep(Action<RunResults> action) : IRunStep
    {
        public int TotalSteps => 1;
        public bool CanRunConcurrently => false;

        public bool RunStep(RunResults results)
        {
            action(results);
            return true;
        }
    }

    private sealed class MutationPressure(float energy) : SelectionPressure(1, [new CostlyMutation()])
    {
        public override ushort CurrentArchiveVersion => 1;
        public override ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.RootPressure;
        public override LocalizedString Name => new("TEST_MUTATION_ENERGY");

        public override float Score(Species species, Patch patch, SimulationCache cache)
        {
            return ((MicrobeSpecies)species).MembraneRigidity > 0 ? 2 : 1;
        }

        public override float GetEnergy(Patch patch)
        {
            return energy;
        }
    }

    private sealed class CostlyMutation : IMutationStrategy<Species>
    {
        public bool Repeatable => false;

        public List<CommonMutationFunctions.Mutant>? MutationsOf(Species baseSpecies, double mp, bool lawk,
            Random random, BiomeConditions biomeToConsider)
        {
            if (((MicrobeSpecies)baseSpecies).MembraneRigidity > 0)
                return null;

            var mutant = (MicrobeSpecies)baseSpecies.Clone();
            mutant.MembraneRigidity = 0.1f;
            for (var i = 1; i <= 20; ++i)
            {
                mutant.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"),
                    new Hex(i * 4, 0), 0));
            }

            return [new CommonMutationFunctions.Mutant(mutant, mp)];
        }
    }
}
