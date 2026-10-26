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
    ///   A candidate exactly at the viability minimum must restore the displaced resident before population
    ///   calculation. The resident differs from the mutation parent, so its population record also detects
    ///   restoring the wrong species or publishing the rejected candidate.
    /// </summary>
    [TestCase]
    public void RejectedOccupantIsRestoredBeforePopulationAndPublication()
    {
        var fixture = CreateFixture();
        var prior = fixture.World.NewMicrobeSpecies("Recovery", "prior");
        prior.ApplyMutation(fixture.Parent);
        prior.MembraneRigidity = 0.05f;
        prior.OnEdited();
        fixture.Patch.AddSpecies(prior, 100);
        var rejected = Candidate(fixture.Parent, 0.1f);
        for (int i = 1; i <= 20; ++i)
        {
            rejected.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"),
                new Hex(i * 4, 0), 0));
        }

        var pressure = new TestPressure(s => ReferenceEquals(s, rejected) ? 3 : 1);
        pressure.Mutations.Add(new FixedCandidates(fixture.Parent, rejected));
        var leaf = new Miche(pressure) { Occupant = prior };
        var results = new RunResults();
        results.AddNewMicheForPatch(fixture.Patch, leaf);
        var modify = PrepareInsertion(fixture, results);
        AssertThat(modify.RunStep(results)).IsFalse();
        AssertThat(leaf.Occupant).IsSame(rejected);

        var cost = MichePopulation.CalculateIndividualCost(rejected, fixture.Patch.Biome, fixture.Cache);
        pressure.Energy = cost * (Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION + 0.25f);
        AssertThat(MichePopulation.CalculatePopulationInPatch(rejected, leaf, fixture.Patch, fixture.Cache))
            .IsEqual(Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION);
        AssertThat(modify.RunStep(results)).IsTrue();
        AssertThat(leaf.Occupant).IsSame(prior);
        AssertThat(results.GetPossibleSpeciesList()).IsEmpty();

        // The parent and prior resident score equally, so each receives half of the leaf's energy.
        var priorCost = MichePopulation.CalculateIndividualCost(prior, fixture.Patch.Biome, fixture.Cache);
        var expectedPopulation = (int)(pressure.Energy / 2 / priorCost);
        AssertThat(expectedPopulation > Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION).IsTrue();
        AssertThat(new CalculatePopulation(fixture.Settings.AutoEvoConfiguration, fixture.Settings, fixture.World.Map,
            null, true).RunStep(results)).IsTrue();
        AssertThat(new RegisterNewSpecies(fixture.World, new HashSet<Species> { fixture.Parent, prior })
            .RunStep(results)).IsTrue();
        AssertThat(results.GetPopulationInPatch(prior, fixture.Patch)).IsEqual(expectedPopulation);
        AssertThat(results.SpeciesHasResults(rejected)).IsFalse();
        AssertThat(results.GetPossibleSpeciesList()).IsEmpty();
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

    private static Fixture CreateFixture()
    {
        var settings = new WorldGenerationSettings
        {
            Seed = 7233,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
            AutoEvoConfiguration = new AutoEvoConfiguration { MutationsPerSpecies = 4 },
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
}
