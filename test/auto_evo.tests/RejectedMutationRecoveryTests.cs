using System;
using System.Collections.Generic;
using AutoEvo;
using GdUnit4;
using SharedBase.Archive;
using static GdUnit4.Assertions;

/// <summary>
///   Verifies rejected mutation recovery before population calculation and publication.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class RejectedMutationRecoveryTests
{
    /// <summary>
    ///   A candidate that displaced an existing resident but has too little population must restore that resident
    ///   before population calculation. Following registration and result application guards against publishing
    ///   the rejected candidate or losing the restored resident later in the run.
    /// </summary>
    [TestCase]
    public void RejectedOccupantIsRestoredBeforePopulationAndRemainsAbsentAfterApply()
    {
        var settings = new WorldGenerationSettings
        {
            Seed = 7233,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
            AutoEvoConfiguration = new AutoEvoConfiguration { MutationsPerSpecies = 1 },
        };
        var world = new GameWorld(settings);
        var patch = world.Map.CurrentPatch!;
        world.Map.Patches.Clear();
        world.Map.Patches.Add(patch.ID, patch);
        patch.SpeciesInPatch.Clear();
        var parent = world.NewMicrobeSpecies("Recovery", "parent");
        parent.ApplyMutation(world.PlayerSpecies);
        parent.MembraneRigidity = 0;
        parent.OnEdited();
        var prior = world.NewMicrobeSpecies("Recovery", "prior");
        prior.ApplyMutation(parent);
        prior.MembraneRigidity = 0.05f;
        prior.OnEdited();
        patch.AddSpecies(parent, 100);
        patch.AddSpecies(prior, 100);
        var cache = new SimulationCache(settings);
        var cost = MichePopulation.CalculateIndividualCost(parent, patch.Biome, cache);
        var leaf = new Miche(new CostlyPressure(cost * (Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION + 2) * 3))
        {
            Occupant = prior,
        };
        Species? candidate = null;
        var run = new ControlledRun(world,
            new ObserveStep(results =>
            {
                results.AddNewMicheForPatch(patch, leaf);
                var modify = new ModifyExistingSpecies(patch, cache, settings, new Random(7233));
                for (int i = 0; i < 10 && ReferenceEquals(leaf.Occupant, prior); ++i)
                    AssertThat(modify.RunStep(results)).IsFalse();

                candidate = leaf.Occupant!;
                AssertThat(candidate).IsNotNull();
                AssertThat(candidate).IsNotSame(parent);
                AssertThat(candidate).IsNotSame(prior);
                var estimated = MichePopulation.CalculatePopulationInPatch(candidate, leaf, patch, cache);
                AssertThat(estimated > 0 && estimated <= Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION).IsTrue();
                AssertThat(results.GetPossibleSpeciesList()).IsEmpty();
                AssertThat(modify.RunStep(results)).IsTrue();

                // The repair must be visible before formal population calculation.
                AssertThat(leaf.Occupant).IsSame(prior);
                AssertThat(results.GetPossibleSpeciesList()).IsEmpty();
            }),
            new CalculatePopulation(settings.AutoEvoConfiguration, settings, world.Map, null, true),
            new RegisterNewSpecies(world, new HashSet<Species> { parent, prior }));
        for (int i = 0; i < 20 && !run.Finished; ++i)
            run.OneStep();

        AssertThat(run.WasSuccessful).IsTrue();
        var final = run.Results!;
        AssertThat(final.SpeciesHasResults(candidate!)).IsFalse();
        AssertThat(final.GetPopulationInPatch(prior, patch) >= Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION).IsTrue();
        AssertThat(final.GetPatchEnergyResults(prior)[patch].TotalEnergyGathered).IsGreater(0);
        run.CalculateAndApplyFinalExternalEffectSizes();
        final.RegisterNewSpeciesForSummary(world);
        final.StorePreviousPopulations(world.Map);
        final.MakeSummary();
        run.ApplyAllResults(true);
        AssertThat(leaf.Occupant).IsSame(prior);
        AssertThat(final.SpeciesHasResults(candidate!)).IsFalse();
        AssertThat(final.GetPossibleSpeciesList()).IsEmpty();
        AssertThat(patch.SpeciesInPatch.ContainsKey(candidate!)).IsFalse();
        AssertThat(patch.GetSpeciesSimulationPopulation(prior)).IsEqual(final.GetPopulationInPatch(prior, patch));
    }

    /// <summary>
    ///   Checks candidate estimates of zero and immediately below, at, and above the viability minimum.
    ///   Only the above-minimum candidate may remain and be published, protecting the strict admission boundary
    ///   and restoration of the prior occupant for each rejected case.
    /// </summary>
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(1)]
    [TestCase(2)]
    public void AdmissionRejectsZeroBelowAndEqualButKeepsAboveMinimum(int boundary)
    {
        var fixture = CreateFixture();
        var candidate = Candidate(fixture.Parent, 0.1f);
        var strategy = new FixedCandidates(fixture.Parent, candidate);
        var pressure = new TestPressure(species => ReferenceEquals(species, candidate) ? 2 : 1);
        pressure.Mutations.Add(strategy);
        int expected = boundary switch
        {
            0 => 0,
            -1 => Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION - 1,
            1 => Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION,
            _ => Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION + 1,
        };
        var leaf = new Miche(pressure) { Occupant = fixture.Parent };
        var results = new RunResults();
        results.AddNewMicheForPatch(fixture.Patch, leaf);
        var modify = PrepareInsertion(fixture, results);
        AssertThat(modify.RunStep(results)).IsFalse();
        AssertThat(leaf.Occupant).IsSame(candidate);
        var cost = MichePopulation.CalculateIndividualCost(candidate, fixture.Patch.Biome, fixture.Cache);
        pressure.Energy = cost * (expected + 0.25f);
        AssertThat(MichePopulation.CalculatePopulationInPatch(candidate, leaf, fixture.Patch, fixture.Cache))
            .IsEqual(expected);
        AssertThat(modify.RunStep(results)).IsTrue();
        bool accepted = expected > Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION;
        AssertThat(leaf.Occupant).IsSame(accepted ? candidate : fixture.Parent);
        AssertThat(results.GetPossibleSpeciesList().Count).IsEqual(accepted ? 1 : 0);
        if (accepted)
            AssertThat(results.GetPossibleSpeciesList()[0].InitialPopulationInPatches.Value).IsEqual(expected);
    }

    /// <summary>
    ///   An existing resident whose calculated population equals the viability minimum must survive the formal
    ///   population step, preventing the stricter candidate-admission rule from being applied to residents.
    /// </summary>
    [TestCase]
    public void FormalPopulationExactlyAtMinimumSurvives()
    {
        var fixture = CreateFixture();
        var cost = MichePopulation.CalculateIndividualCost(fixture.Parent, fixture.Patch.Biome, fixture.Cache);
        var pressure = new TestPressure(_ => 1)
        {
            Energy = cost * (Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION + 0.25f),
        };
        var leaf = new Miche(pressure) { Occupant = fixture.Parent };
        var results = new RunResults();
        results.AddNewMicheForPatch(fixture.Patch, leaf);
        AssertThat(MichePopulation.CalculatePopulationInPatch(fixture.Parent, leaf, fixture.Patch, fixture.Cache))
            .IsEqual(Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION);
        AssertThat(new CalculatePopulation(fixture.Settings.AutoEvoConfiguration, fixture.Settings, fixture.World.Map)
            .RunStep(results)).IsTrue();
        AssertThat(results.GetPopulationInPatch(fixture.Parent, fixture.Patch))
            .IsEqual(Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION);
    }

    /// <summary>
    ///   A mixed batch must restore rejected candidates' leaves to their exact prior occupants, including null,
    ///   while retaining accepted candidates and independent objects with the same species ID.
    ///   This protects selective recovery and the accepted candidate's publication record.
    /// </summary>
    [TestCase]
    public void MixedLeavesRestoreExactPriorReferencesWithoutOverwritingAcceptedOrSameIdObjects()
    {
        var fixture = CreateFixture();
        var prior = Candidate(fixture.Parent, 0.05f);
        fixture.Patch.AddSpecies(prior, 100);
        var rejected = Candidate(fixture.Parent, 0.1f);
        var accepted = Candidate(fixture.Parent, 0.2f);
        var sameId = Candidate(fixture.Parent, 0.3f);
        fixture.Patch.AddSpecies(sameId, 100);
        AssertThat(rejected.ID).IsEqual(sameId.ID);
        AssertThat(rejected).IsNotSame(sameId);
        AssertThat(prior).IsNotSame(fixture.Parent);
        var rootPressure = new TestPressure(_ => 1);
        rootPressure.Mutations.Add(new FixedCandidates(fixture.Parent, rejected, accepted));
        var root = new Miche(rootPressure);
        var first = new Miche(new TestPressure(s => ReferenceEquals(s, rejected) ? 2 : 1)) { Occupant = prior };
        var empty = new Miche(new TestPressure(s => ReferenceEquals(s, rejected) ? 2 : 0));
        var acceptedPressure = new TestPressure(s => ReferenceEquals(s, accepted) ? 2 : 0) { Energy = 10000 };
        var retained = new Miche(acceptedPressure);
        var other = new Miche(new TestPressure(s => ReferenceEquals(s, sameId) ? 2 : 0)) { Occupant = sameId };
        root.AddChild(first);
        root.AddChild(empty);
        root.AddChild(retained);
        root.AddChild(other);
        var results = new RunResults();
        results.AddNewMicheForPatch(fixture.Patch, root);
        var modify = PrepareInsertion(fixture, results);
        AssertThat(modify.RunStep(results)).IsFalse();
        AssertThat(first.Occupant).IsSame(rejected);
        AssertThat(empty.Occupant).IsSame(rejected);
        AssertThat(retained.Occupant).IsSame(accepted);
        AssertThat(other.Occupant).IsSame(sameId);
        AssertThat(MichePopulation.CalculatePopulationInPatch(rejected, root, fixture.Patch, fixture.Cache)).IsEqual(0);
        var acceptedEstimate = MichePopulation.CalculatePopulationInPatch(accepted, root, fixture.Patch, fixture.Cache);
        AssertThat(acceptedEstimate > Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION).IsTrue();
        AssertThat(modify.RunStep(results)).IsTrue();
        AssertThat(first.Occupant).IsSame(prior);
        AssertThat(empty.Occupant).IsNull();
        AssertThat(retained.Occupant).IsSame(accepted);
        AssertThat(other.Occupant).IsSame(sameId);
        AssertThat(results.GetPossibleSpeciesList().Count).IsEqual(1);
        var possible = results.GetPossibleSpeciesList()[0];
        AssertThat(possible.Species).IsSame(accepted);
        AssertThat(possible.ParentSpecies).IsSame(fixture.Parent);
        AssertThat(possible.AddType).IsEqual(RunResults.NewSpeciesType.FillNiche);
        AssertThat(possible.InitialPopulationInPatches.Value).IsEqual(acceptedEstimate);
    }

    /// <summary>
    ///   When a rejected candidate displaced a viable hidden candidate, recovery must restore the original
    ///   resident without replacing it with the hidden candidate or expanding an accepted candidate into that leaf.
    ///   Separate insertion probes show both alternatives would change the tree, making the retained state
    ///   and publication record evidence that recovery preserves the completed batch.
    /// </summary>
    [TestCase]
    public void HiddenCandidateIsNotRetriedAndAcceptedCandidateDoesNotExpand()
    {
        var fixture = CreateFixture();
        var hidden = Candidate(fixture.Parent, 0.1f);
        var rejected = Candidate(fixture.Parent, 0.2f);
        for (int i = 1; i <= 20; ++i)
        {
            rejected.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"),
                new Hex(i * 4, 0), 0));
        }

        var accepted = Candidate(fixture.Parent, 0.3f);
        var rootPressure = new TestPressure(_ => 1);
        rootPressure.Mutations.Add(new FixedCandidates(fixture.Parent, hidden, rejected, accepted));
        var root = new Miche(rootPressure);
        var sharedPressure = new TestPressure(s =>
        {
            if (ReferenceEquals(s, rejected))
                return 4;

            if (ReferenceEquals(s, hidden))
                return 3;

            return ReferenceEquals(s, accepted) ? 2 : 1;
        });
        var shared = new Miche(sharedPressure) { Occupant = fixture.Parent };
        var retainedPressure = new TestPressure(s => ReferenceEquals(s, accepted) ? 2 : 0) { Energy = 10000 };
        var retained = new Miche(retainedPressure);
        root.AddChild(shared);
        root.AddChild(retained);
        var results = new RunResults();
        results.AddNewMicheForPatch(fixture.Patch, root);
        var modify = PrepareInsertion(fixture, results, 7233);
        sharedPressure.Energy = MichePopulation.CalculateIndividualCost(hidden, fixture.Patch.Biome, fixture.Cache) *
            (Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION + 2.25f);
        var sharedHistory = new List<Species?> { shared.Occupant };
        rootPressure.ObserveScore = _ =>
        {
            if (!ReferenceEquals(sharedHistory[^1], shared.Occupant))
                sharedHistory.Add(shared.Occupant);
        };
        AssertThat(modify.RunStep(results)).IsFalse();
        rootPressure.ObserveScore = null;
        if (!ReferenceEquals(sharedHistory[^1], shared.Occupant))
            sharedHistory.Add(shared.Occupant);
        AssertThat(sharedHistory.Exists(s => ReferenceEquals(s, hidden))).IsTrue();
        AssertThat(shared.Occupant).IsSame(rejected);
        AssertThat(retained.Occupant).IsSame(accepted);
        var rejectedEstimate = MichePopulation.CalculatePopulationInPatch(rejected, root, fixture.Patch, fixture.Cache);
        AssertThat(rejectedEstimate <= Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION).IsTrue();
        var acceptedEstimate = MichePopulation.CalculatePopulationInPatch(accepted, root, fixture.Patch, fixture.Cache);
        AssertThat(acceptedEstimate > Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION).IsTrue();
        AssertThat(modify.RunStep(results)).IsTrue();
        AssertThat(shared.Occupant).IsSame(fixture.Parent);
        AssertThat(retained.Occupant).IsSame(accepted);
        AssertThat(results.GetPossibleSpeciesList().Count).IsEqual(1);
        var possible = results.GetPossibleSpeciesList()[0];
        AssertThat(possible.Species).IsSame(accepted);
        AssertThat(possible.ParentSpecies).IsSame(fixture.Parent);
        AssertThat(possible.AddType).IsEqual(RunResults.NewSpeciesType.SplitDueToMutation);
        AssertThat(possible.InitialPopulationInPatches.Value).IsEqual(acceptedEstimate);

        // Counterfactual probes use separate trees, preserving the completed batch and its published record.
        var retryTree = new Miche(sharedPressure) { Occupant = fixture.Parent };
        AssertThat(retryTree.InsertSpecies(hidden, fixture.Patch, null, fixture.Cache, false,
            new Miche.InsertWorkingMemory())).IsTrue();
        AssertThat(MichePopulation.CalculatePopulationInPatch(hidden, retryTree, fixture.Patch, fixture.Cache) >
            Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION).IsTrue();
        var expansionTree = new Miche(sharedPressure) { Occupant = fixture.Parent };
        AssertThat(expansionTree.InsertSpecies(accepted, fixture.Patch, null, fixture.Cache, false,
            new Miche.InsertWorkingMemory())).IsTrue();
        AssertThat(expansionTree.Occupant).IsSame(accepted);
        AssertThat(shared.Occupant).IsSame(fixture.Parent);
    }

    /// <summary>
    ///   Rejecting a candidate in one patch must preserve a candidate already accepted in another patch and its
    ///   publication record. Formal population calculation and registration must retain that accepted split
    ///   without publishing the rejected candidate or replacing their shared parent.
    /// </summary>
    [TestCase]
    public void RejectionInOnePatchPreservesAnotherPatchCandidateAndGlobalSplitRegistration()
    {
        var fixture = CreateFixture(true);
        Patch? secondPatch = null;
        foreach (var patch in fixture.World.Map.Patches.Values)
        {
            if (!ReferenceEquals(patch, fixture.Patch))
                secondPatch = patch;
        }

        AssertThat(secondPatch).IsNotNull();
        secondPatch!.SpeciesInPatch.Clear();
        secondPatch.AddSpecies(fixture.Parent, 100);
        var rejected = Candidate(fixture.Parent, 0.1f);
        var accepted = Candidate(fixture.Parent, 0.2f);
        var firstPressure = new TestPressure(s => ReferenceEquals(s, rejected) ? 2 : 1);
        firstPressure.Mutations.Add(new FixedCandidates(fixture.Parent, rejected));
        var secondPressure = new TestPressure(s => ReferenceEquals(s, accepted) ? 2 : 1) { Energy = 10000 };
        secondPressure.Mutations.Add(new FixedCandidates(fixture.Parent, accepted));
        var firstTree = new Miche(firstPressure) { Occupant = fixture.Parent };
        var secondTree = new Miche(secondPressure) { Occupant = fixture.Parent };
        var results = new RunResults();
        results.AddNewMicheForPatch(fixture.Patch, firstTree);
        results.AddNewMicheForPatch(secondPatch, secondTree);
        var firstModify = PrepareInsertion(fixture, results);
        var secondModify = PrepareInsertion(fixture with { Patch = secondPatch }, results);
        AssertThat(firstModify.RunStep(results)).IsFalse();
        AssertThat(secondModify.RunStep(results)).IsFalse();
        AssertThat(firstTree.Occupant).IsSame(rejected);
        AssertThat(secondTree.Occupant).IsSame(accepted);
        AssertThat(MichePopulation.CalculatePopulationInPatch(rejected, firstTree, fixture.Patch, fixture.Cache))
            .IsEqual(0);
        AssertThat(MichePopulation.CalculatePopulationInPatch(accepted, secondTree, secondPatch, fixture.Cache) >
            Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION).IsTrue();
        AssertThat(secondModify.RunStep(results)).IsTrue();
        AssertThat(results.GetPossibleSpeciesList().Count).IsEqual(1);
        var acceptedRecord = results.GetPossibleSpeciesList()[0];
        AssertThat(firstModify.RunStep(results)).IsTrue();
        AssertThat(firstTree.Occupant).IsSame(fixture.Parent);
        AssertThat(secondTree.Occupant).IsSame(accepted);
        AssertThat(results.GetPossibleSpeciesList().Count).IsEqual(1);
        AssertThat(results.GetPossibleSpeciesList()[0]).IsEqual(acceptedRecord);
        AssertThat(new CalculatePopulation(fixture.Settings.AutoEvoConfiguration, fixture.Settings, fixture.World.Map)
            .RunStep(results)).IsTrue();
        AssertThat(new RegisterNewSpecies(fixture.World, new HashSet<Species> { fixture.Parent }).RunStep(results))
            .IsTrue();
        AssertThat(results.SpeciesHasResults(rejected)).IsFalse();
        AssertThat(results.GetSpeciesResultForInternalUse(fixture.Parent).MutatedProperties).IsNull();
        AssertThat(results.GetSpeciesResultForInternalUse(accepted).SplitFrom).IsSame(fixture.Parent);
        AssertThat(results.GetPossibleSpeciesList().Count).IsEqual(1);
        AssertThat(results.GetPossibleSpeciesList()[0]).IsEqual(acceptedRecord);
        AssertThat(secondTree.Occupant).IsSame(accepted);
    }

    private static Fixture CreateFixture(bool twoPatches = false)
    {
        var settings = new WorldGenerationSettings
        {
            Seed = 7233,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
            AutoEvoConfiguration = new AutoEvoConfiguration { MutationsPerSpecies = 4 },
        };
        var world = new GameWorld(settings);
        var patch = world.Map.CurrentPatch!;
        Patch? extra = null;
        if (twoPatches)
        {
            foreach (var entry in world.Map.Patches.Values)
            {
                if (ReferenceEquals(entry, patch))
                    continue;

                extra = entry;
                break;
            }
        }

        world.Map.Patches.Clear();
        if (extra != null)
            world.Map.Patches.Add(extra.ID, extra);

        world.Map.Patches.Add(patch.ID, patch);
        patch.SpeciesInPatch.Clear();
        var parent = world.NewMicrobeSpecies("Recovery", "parent");
        parent.ApplyMutation(world.PlayerSpecies);
        parent.MembraneRigidity = 0;
        parent.OnEdited();
        patch.AddSpecies(parent, 100);
        return new Fixture(settings, world, patch, parent, new SimulationCache(settings));
    }

    private static MicrobeSpecies Candidate(MicrobeSpecies parent, float rigidity)
    {
        var candidate = (MicrobeSpecies)parent.Clone();
        candidate.MembraneRigidity = rigidity;
        return candidate;
    }

    private static ModifyExistingSpecies PrepareInsertion(Fixture fixture, RunResults results, int seed = 7233)
    {
        var modify = new ModifyExistingSpecies(fixture.Patch, fixture.Cache, fixture.Settings, new Random(seed));

        // One generation step per patch species, then the end-of-enumeration and filter steps.
        for (int i = 0; i < fixture.Patch.SpeciesInPatch.Count + 2; ++i)
            AssertThat(modify.RunStep(results)).IsFalse();

        return modify;
    }

    private sealed record Fixture(WorldGenerationSettings Settings, GameWorld World, Patch Patch,
        MicrobeSpecies Parent, SimulationCache Cache);

    private sealed class FixedCandidates(Species parent, params Species[] candidates) : IMutationStrategy<Species>
    {
        private bool supplied;

        public bool Repeatable => false;

        public List<CommonMutationFunctions.Mutant>? MutationsOf(Species baseSpecies, double mp, bool lawk,
            Random random, BiomeConditions biomeToConsider)
        {
            if (supplied || !ReferenceEquals(baseSpecies, parent))
                return null;

            supplied = true;
            var result = new List<CommonMutationFunctions.Mutant>();
            foreach (var candidate in candidates)
                result.Add(new CommonMutationFunctions.Mutant(candidate, mp));

            return result;
        }
    }

    private sealed class TestPressure(Func<Species, float> score) : SelectionPressure(1, [])
    {
        public float Energy { get; set; }
        public Action<Species>? ObserveScore { get; set; }
        public override ushort CurrentArchiveVersion => 1;
        public override ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.RootPressure;
        public override LocalizedString Name => new("TEST_RECOVERY_ENERGY");

        public override float Score(Species species, Patch patch, SimulationCache cache)
        {
            ObserveScore?.Invoke(species);
            return score(species);
        }

        public override float GetEnergy(Patch patch)
        {
            return Energy;
        }
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

    private sealed class ObserveStep(Action<RunResults> action) : IRunStep
    {
        public int TotalSteps => 1;
        public bool CanRunConcurrently => false;

        public bool RunStep(RunResults results)
        {
            action(results);
            return true;
        }
    }

    private sealed class CostlyPressure(float energy) : SelectionPressure(1, [new CostlyMutation()])
    {
        public override ushort CurrentArchiveVersion => 1;
        public override ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.RootPressure;
        public override LocalizedString Name => new("TEST_RECOVERY_ENERGY");

        public override float Score(Species species, Patch patch, SimulationCache cache)
        {
            var rigidity = ((MicrobeSpecies)species).MembraneRigidity;
            if (rigidity >= 0.1f)
                return 3;

            return rigidity > 0 ? 2 : 1;
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
            for (int i = 1; i <= 20; ++i)
            {
                mutant.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"),
                    new Hex(i * 4, 0), 0));
            }

            return [new CommonMutationFunctions.Mutant(mutant, mp)];
        }
    }
}
