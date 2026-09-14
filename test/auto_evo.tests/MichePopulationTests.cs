using System;
using System.Collections.Generic;
using AutoEvo;
using GdUnit4;
using SharedBase.Archive;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class MichePopulationTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void EnergyAndResidentScoresAreReadOncePerLeaf(bool trackEnergy)
    {
        var fixture = CreateFixture();
        var rootPressure = new CountingPressure(101, _ => 1);
        var leafPressure = new CountingPressure(102, _ => 1);
        var root = new Miche(rootPressure);
        root.AddChild(new Miche(leafPressure) { Occupant = fixture.Species[0] });
        var results = Simulate(fixture, root, trackEnergy);

        AssertThat(rootPressure.HashCalls).IsEqual(4);
        AssertThat(leafPressure.HashCalls).IsEqual(4);
        AssertThat(rootPressure.EnergyCalls).IsEqual(0);
        AssertThat(leafPressure.EnergyCalls).IsEqual(1);
        AssertThat(leafPressure.DescriptionCalls).IsEqual(trackEnergy ? 3 : 0);
        if (!trackEnergy)
            return;

        foreach (var species in fixture.Species)
        {
            var energy = results.GetPatchEnergyResults(species)[fixture.Patch];
            AssertThat(energy.TotalEnergyGathered).IsEqual(10000.0f);
            AssertThat(energy.PerNicheEnergy.Count).IsEqual(1);
            foreach (var niche in energy.PerNicheEnergy.Values)
            {
                AssertThat(niche.TotalAvailableEnergy).IsEqual(30000.0f);
                AssertThat(niche.CurrentSpeciesEnergy).IsEqual(10000.0f);
                AssertThat(niche.CurrentSpeciesFitness).IsEqual(2.0f);
                AssertThat(niche.TotalFitness).IsEqual(6.0f);
            }
        }
    }

    [TestCase]
    public void ZeroRawScoresDoNotEvaluateResidentsDeeperPressuresOrEnergy()
    {
        var fixture = CreateFixture();
        var absentResident = new MicrobeSpecies(99, "Test", "absent");
        var rootPressure = new CountingPressure(201, species => species == absentResident ?
            throw new InvalidOperationException("Resident must remain unevaluated") :
            0);
        var leafPressure = new CountingPressure(202, _ => throw new InvalidOperationException("Unreached pressure"));
        var root = new Miche(rootPressure);
        root.AddChild(new Miche(leafPressure) { Occupant = absentResident });
        Simulate(fixture, root, true);

        AssertThat(rootPressure.HashCalls).IsEqual(3);
        AssertThat(leafPressure.HashCalls).IsEqual(0);
        AssertThat(leafPressure.EnergyCalls).IsEqual(0);
        AssertThat(leafPressure.DescriptionCalls).IsEqual(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EarlyExitAndZeroOrMissingResidentKeepRelativeScores(bool missingResident)
    {
        var fixture = CreateFixture();
        var rootPressure = new CountingPressure(301, species => species == fixture.Species[0] ? 0 : 1);
        var leafPressure = new CountingPressure(302, species => species == fixture.Species[0] ? 0 : 1);
        var root = new Miche(rootPressure);
        root.AddChild(new Miche(leafPressure) { Occupant = missingResident ? null : fixture.Species[0] });
        var results = Simulate(fixture, root, true);

        AssertThat(rootPressure.HashCalls).IsEqual(missingResident ? 3 : 4);
        AssertThat(leafPressure.HashCalls).IsEqual(missingResident ? 2 : 3);
        AssertThat(leafPressure.EnergyCalls).IsEqual(1);
        AssertThat(leafPressure.DescriptionCalls).IsEqual(2);
        AssertThat(results.GetPatchEnergyResults(fixture.Species[0])[fixture.Patch].TotalEnergyGathered).IsEqual(0.0f);
        for (int i = 1; i < fixture.Species.Count; ++i)
        {
            var energy = results.GetPatchEnergyResults(fixture.Species[i])[fixture.Patch];
            AssertThat(energy.TotalEnergyGathered).IsEqual(15000.0f);
            foreach (var niche in energy.PerNicheEnergy.Values)
            {
                AssertThat(niche.CurrentSpeciesFitness).IsEqual(4.0f);
                AssertThat(niche.TotalFitness).IsEqual(8.0f);
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EachLeafUsesItsOwnResidentAndOriginalFloatingPointOrder(bool strict)
    {
        var fixture = CreateFixture();
        fixture.Settings.AutoEvoConfiguration = new AutoEvoConfiguration { StrictNicheCompetition = strict };
        var rootPressure = new CountingPressure(401, species => species.ID);
        var firstPressure = new CountingPressure(402, species => species.ID * 1.3f);
        var secondPressure = new CountingPressure(403, species => species.ID * 0.7f);
        var root = new Miche(rootPressure);
        root.AddChild(new Miche(firstPressure) { Occupant = fixture.Species[0] });
        root.AddChild(new Miche(secondPressure) { Occupant = fixture.Species[2] });
        var results = Simulate(fixture, root, true);
        foreach (var species in fixture.Species)
        {
            var expectedEnergy = 0.0f;
            foreach (int residentIndex in new[] { 0, 2 })
            {
                var factor = residentIndex == 0 ? 1.3f : 0.7f;
                var scores = new List<float>();
                float total = 0;
                foreach (var entry in fixture.Species)
                {
                    var first = (float)entry.ID / fixture.Species[residentIndex].ID;
                    var second = entry.ID * factor / (fixture.Species[residentIndex].ID * factor);
                    if (strict)
                    {
                        first *= first;
                        second *= second;
                    }

                    var score = first + second;
                    scores.Add(score);
                    total += score;
                }

                expectedEnergy += 30000.0f * (scores[fixture.Species.IndexOf(species)] / total);
            }

            var actual = results.GetPatchEnergyResults(species)[fixture.Patch].TotalEnergyGathered;
            AssertThat(BitConverter.SingleToInt32Bits(actual)).IsEqual(BitConverter.SingleToInt32Bits(expectedEnergy));
        }

        AssertThat(firstPressure.EnergyCalls).IsEqual(1);
        AssertThat(secondPressure.EnergyCalls).IsEqual(1);
        AssertThat(rootPressure.HashCalls).IsEqual(8);
    }

    [TestCase(false, false, 1)]
    [TestCase(false, true, 1)]
    [TestCase(true, false, 1)]
    [TestCase(true, true, 1)]
    [TestCase(false, true, 2)]
    [TestCase(true, true, 2)]
    public void PatchesKeepIndependentResultsAcrossCapacityChanges(bool strict, bool trackEnergy, int steps)
    {
        var fixture = CreateFixture(9);
        fixture.Settings.AutoEvoConfiguration = new AutoEvoConfiguration { StrictNicheCompetition = strict };
        var patches = new List<Patch>(fixture.World.Map.Patches.Values);
        foreach (var patch in patches)
            patch.SpeciesInPatch.Clear();

        var trees = new Dictionary<Patch, Miche>();
        int[] speciesCounts = [9, 0, 1, 3, 9, 2];
        for (int index = 0; index < speciesCounts.Length; ++index)
        {
            var patch = patches[index];
            int speciesCount = speciesCounts[index];
            for (int speciesIndex = 0; speciesIndex < speciesCount; ++speciesIndex)
                patch.AddSpecies(fixture.Species[speciesIndex], 1000);

            var root = new Miche(new CountingPressure(500 + index * 100, _ => 1));
            for (int leafIndex = 0; leafIndex < speciesCount; ++leafIndex)
            {
                var parent = root;
                for (int depth = 0; depth <= leafIndex; ++depth)
                {
                    bool zeroScore = index == 3;
                    var node = new Miche(new CountingPressure(501 + index * 100 + leafIndex * 10 + depth,
                        species => zeroScore ? 0 : species.ID * 0.7f));
                    parent.AddChild(node);
                    parent = node;
                }

                parent.Occupant = fixture.Species[leafIndex];
            }

            trees.Add(patch, root);
        }

        var expected = new Dictionary<Patch, RunResults>();
        foreach (var patch in trees.Keys)
            expected.Add(patch, SimulatePatches(fixture, trees, new HashSet<Patch> { patch }, trackEnergy, steps));

        var actual = SimulatePatches(fixture, trees, new HashSet<Patch>(trees.Keys), trackEnergy, steps);
        var repeated = SimulatePatches(fixture, trees, new HashSet<Patch>(trees.Keys), trackEnergy, steps);
        foreach (var patch in trees.Keys)
        {
            AssertPatchResults(fixture, patch, expected[patch], actual, trackEnergy);
            AssertPatchResults(fixture, patch, expected[patch], repeated, trackEnergy);
        }
    }

    [TestCase]
    public void FailedSimulationDoesNotAffectLaterCallsOrEarlierResults()
    {
        var fixture = CreateFixture();
        var root = new Miche(new CountingPressure(1201, _ => 1));
        root.AddChild(new Miche(new CountingPressure(1202, _ => 1)) { Occupant = fixture.Species[0] });
        var before = Simulate(fixture, CopyMiche(root), true);
        var failingRoot = new Miche(new CountingPressure(1203, _ => 1));
        failingRoot.AddChild(new Miche(new CountingPressure(1204,
            _ => throw new InvalidOperationException("Test scoring failure"))) { Occupant = fixture.Species[1] });

        AssertThrown(() => Simulate(fixture, failingRoot, true)).IsInstanceOf<InvalidOperationException>();

        var after = Simulate(fixture, CopyMiche(root), true);
        foreach (var species in fixture.Species)
            AssertThat(before.GetPatchEnergyResults(species)[fixture.Patch].TotalEnergyGathered).IsEqual(10000.0f);
        AssertPatchResults(fixture, fixture.Patch, before, after, true);
    }

    private static RunResults SimulatePatches(Fixture fixture, Dictionary<Patch, Miche> trees,
        HashSet<Patch> patches, bool trackEnergy, int steps)
    {
        var results = new RunResults();
        foreach (var entry in trees)
            results.AddNewMicheForPatch(entry.Key, CopyMiche(entry.Value));

        var configuration = new SimulationConfiguration(fixture.Settings.AutoEvoConfiguration,
            fixture.World.Map, fixture.Settings, steps)
        {
            Results = results,
            CollectEnergyInformation = trackEnergy,
            PatchesToRun = patches,
        };
        MichePopulation.Simulate(configuration, new SimulationCache(fixture.Settings), new Random(1234));
        return results;
    }

    private static void AssertPatchResults(Fixture fixture, Patch patch, RunResults expected, RunResults actual,
        bool trackEnergy)
    {
        foreach (var species in fixture.Species)
        {
            AssertThat(actual.GetPopulationInPatch(species, patch))
                .IsEqual(expected.GetPopulationInPatch(species, patch));
            if (!trackEnergy)
                continue;

            bool expectedHasEnergy = expected.GetPatchEnergyResults(species).TryGetValue(patch, out var expectedEnergy);
            bool actualHasEnergy = actual.GetPatchEnergyResults(species).TryGetValue(patch, out var actualEnergy);
            AssertThat(actualHasEnergy).IsEqual(expectedHasEnergy);
            if (!expectedHasEnergy)
                continue;

            AssertFloatBits(actualEnergy!.TotalEnergyGathered, expectedEnergy!.TotalEnergyGathered);
            AssertFloatBits(actualEnergy.IndividualCost, expectedEnergy.IndividualCost);
            AssertThat(actualEnergy.PerNicheEnergy.Count).IsEqual(expectedEnergy.PerNicheEnergy.Count);
            using var actualNiches = actualEnergy.PerNicheEnergy.Values.GetEnumerator();
            foreach (var entry in expectedEnergy.PerNicheEnergy)
            {
                AssertThat(actualNiches.MoveNext()).IsTrue();
                var actualNiche = actualNiches.Current;
                AssertFloatBits(actualNiche.CurrentSpeciesFitness, entry.Value.CurrentSpeciesFitness);
                AssertFloatBits(actualNiche.CurrentSpeciesEnergy, entry.Value.CurrentSpeciesEnergy);
                AssertFloatBits(actualNiche.TotalFitness, entry.Value.TotalFitness);
                AssertFloatBits(actualNiche.TotalAvailableEnergy, entry.Value.TotalAvailableEnergy);
            }
        }
    }

    private static Miche CopyMiche(Miche source)
    {
        var copy = new Miche(source.Pressure) { Occupant = source.Occupant };
        foreach (var child in source.Children)
            copy.AddChild(CopyMiche(child));
        return copy;
    }

    private static void AssertFloatBits(float actual, float expected)
    {
        AssertThat(BitConverter.SingleToInt32Bits(actual)).IsEqual(BitConverter.SingleToInt32Bits(expected));
    }

    private static Fixture CreateFixture(int speciesCount = 3)
    {
        var settings = new WorldGenerationSettings
        {
            Seed = 0x5EED,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        };
        var species = new List<MicrobeSpecies>();
        for (uint id = 1; id <= speciesCount; ++id)
        {
            var microbe = new MicrobeSpecies(id, "Test", "species" + id)
            {
                MembraneType = SimulationParameters.Instance.GetMembrane("single"),
            };
            microbe.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"),
                new Hex(0, 0), 0));
            microbe.OnEdited();
            species.Add(microbe);
        }

        var world = new GameWorld(settings, species[0]);
        foreach (var entry in world.Map.Patches.Values)
            entry.SpeciesInPatch.Clear();
        var patch = world.Map.CurrentPatch!;
        foreach (var microbe in species)
            patch.AddSpecies(microbe, 1000);
        return new Fixture(settings, world, patch, species);
    }

    private static RunResults Simulate(Fixture fixture, Miche root, bool trackEnergy)
    {
        var results = new RunResults();
        results.AddNewMicheForPatch(fixture.Patch, root);
        var configuration = new SimulationConfiguration(fixture.Settings.AutoEvoConfiguration,
            fixture.World.Map, fixture.Settings)
        {
            Results = results,
            CollectEnergyInformation = trackEnergy,
            PatchesToRun = new HashSet<Patch> { fixture.Patch },
        };
        MichePopulation.Simulate(configuration, new SimulationCache(fixture.Settings), new Random(1234));
        return results;
    }

    private sealed record Fixture(WorldGenerationSettings Settings, GameWorld World, Patch Patch,
        List<MicrobeSpecies> Species);

    private sealed class CountingPressure(int identity, Func<Species, float> score) : SelectionPressure(1, [])
    {
        public int HashCalls { get; private set; }
        public int EnergyCalls { get; private set; }
        public int DescriptionCalls { get; private set; }
        public override LocalizedString Name => new("TEST_MICHE_ENERGY");
        public override ushort CurrentArchiveVersion => 1;
        public override ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.RootPressure;

        public override float Score(Species species, Patch patch, SimulationCache cache)
        {
            return score(species);
        }

        public override float GetEnergy(Patch patch)
        {
            ++EnergyCalls;
            return 30000;
        }

        public override LocalizedString GetDescription()
        {
            ++DescriptionCalls;
            return new LocalizedString("TEST_MICHE_ENERGY", identity);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            ++HashCalls;
            return identity;
        }
    }
}
