using System;
using System.Collections.Generic;
using System.IO;
using AutoEvo;
using GdUnit4;
using Saving.Serializers;
using SharedBase.Archive;
using static CommonMutationFunctions;
using static GdUnit4.Assertions;

/// <summary>
///   Checks candidate compound initialization at the result tree, archive, and full refresh boundaries.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class InitialCompoundsFinalizationTests
{
    [TestCase(false, false, false)]
    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(true, true, false)]
    public void LowPopulationMicheOccupantHasFreshCompoundsAfterSaving(bool multicellular, bool massBudding,
        bool specializedStorage)
    {
        var settings = new WorldGenerationSettings
        {
            Seed = 0x5EED,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        };
        var world = new GameWorld(settings);
        var patch = world.Map.CurrentPatch!;
        var parent = CreateSpecies(multicellular, massBudding, specializedStorage);
        patch.SpeciesInPatch.Clear();
        patch.AddSpecies(parent, 100);

        var results = new RunResults();
        var cache = new SimulationCache(settings);
        var generateMiche = new GenerateMiche(patch, cache, world.AutoEvoGlobalCache);
        AssertThat(generateMiche.RunStep(results)).IsTrue();
        var miche = results.GetModifiableMicheForPatch(patch);

        // After normal tree preparation, isolate the zero-energy root as an empty leaf to force the
        // low-population boundary. This is a controlled fixture, not evidence of its real-world frequency.
        miche.Children.Clear();
        miche.Occupant = null;
        var strategy = new CompoundMutation();
        miche.Pressure.Mutations.Clear();
        miche.Pressure.Mutations.Add(strategy);
        var step = new ModifyExistingSpecies(patch, cache, settings, new Random(42));
        var completed = false;
        for (var i = 0; i < 10; ++i)
        {
            if (!step.RunStep(results))
                continue;

            completed = true;
            break;
        }

        AssertThat(completed).IsTrue();
        AssertThat(strategy.Expected.Count > 0).IsTrue();
        AssertThat(miche.Occupant).IsNotNull();
        var occupant = miche.Occupant!;
        AssertThat(occupant).IsNotSame(parent);
        AssertThat(strategy.Expected.ContainsKey(occupant)).IsTrue();
        AssertThat(MichePopulation.CalculatePopulationInPatch(occupant, miche, patch, cache)).IsEqual(0);
        AssertCompounds(strategy.Expected[occupant], occupant.InitialCompounds);

        using var data = new MemoryStream();
        var manager = new ThriveArchiveManager();
        var writer = new SArchiveMemoryWriter(data, manager);
        manager.OnStartNewWrite(writer);
        writer.WriteObject(results);
        manager.OnFinishWrite(writer);
        data.Position = 0;
        var reader = new SArchiveMemoryReader(data, manager);
        manager.OnStartNewRead(reader);
        var loaded = reader.ReadObjectOrNull<RunResults>();
        manager.OnFinishRead(reader);

        AssertThat(loaded).IsNotNull();
        var loadedTrees = loaded!.InspectPatchMicheData();
        AssertThat(loadedTrees.Count).IsEqual(1);
        foreach (var loadedTree in loadedTrees.Values)
        {
            AssertThat(loadedTree.Occupant).IsNotNull();
            AssertCompounds(strategy.Expected[occupant], loadedTree.Occupant!.InitialCompounds);
        }
    }

    [TestCase(false, false, false)]
    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(true, true, false)]
    public void OnEditedRefreshesCompoundsAfterDeferredAttempt(bool multicellular, bool massBudding,
        bool specializedStorage)
    {
        var parent = CreateSpecies(multicellular, massBudding, specializedStorage);
        var candidate = (Species)parent.Clone();
        var expected = new Dictionary<Compound, float>(parent.InitialCompounds);
        candidate.InitialCompounds.Clear();
        candidate.InitialCompounds.Add(Compound.Ammonia, -100);
        candidate.OnAttemptedInAutoEvo(true, false);
        candidate.OnEdited();

        AssertCompounds(expected, candidate.InitialCompounds);
        AssertCompounds(expected, parent.InitialCompounds);
    }

    private static Species CreateSpecies(bool multicellular, bool massBudding,
        bool specializedStorage)
    {
        var parameters = SimulationParameters.Instance;
        var cytoplasm = parameters.GetOrganelleType("cytoplasm");
        var membrane = parameters.GetMembrane("single");
        if (!multicellular)
        {
            var microbe = new MicrobeSpecies(50, "Test", "initialcompounds")
            {
                MembraneType = membrane,
                IsBacteria = true,
            };
            microbe.Organelles.Add(new OrganelleTemplate(cytoplasm, new Hex(0, 0), 0));
            if (specializedStorage)
            {
                var random = new Random(42);
                AssertThat(AddOrganelleWithStrategy(OrganelleAddStrategy.Spiral, Nucleus, Direction.Neutral,
                    microbe, [], [], [], random)).IsTrue();
                var vacuole = parameters.GetOrganelleType("vacuole");
                AssertThat(AddOrganelleWithStrategy(OrganelleAddStrategy.Spiral, vacuole, Direction.Neutral,
                    microbe, [], [], [], random)).IsTrue();
                foreach (var organelle in microbe.Organelles)
                {
                    if (organelle.Definition.InternalName == vacuole.InternalName)
                    {
                        organelle.ModifiableUpgrades = new OrganelleUpgrades
                        {
                            CustomUpgradeData = new StorageComponentUpgrades(Compound.Glucose),
                        };
                    }
                }
            }

            microbe.OnEdited();
            if (specializedStorage)
                AssertThat(microbe.StorageCapacities.Specific.ContainsKey(Compound.Glucose)).IsTrue();

            return microbe;
        }

        var species = new MulticellularSpecies(50, "Test", "initialcompounds");
        var cellType = new CellType(membrane)
        {
            CellTypeName = "TestType",
            IsBacteria = true,
        };
        cellType.ModifiableOrganelles.Add(new OrganelleTemplate(cytoplasm, new Hex(0, 0), 0));
        species.ModifiableCellTypes.Add(cellType);
        var memory1 = new List<Hex>();
        var memory2 = new List<Hex>();
        species.ModifiableGameplayCells.AddFast(new CellTemplate(cellType, new Hex(0, 0), 0), memory1, memory2);
        if (massBudding)
        {
            species.ModifiableGameplayCells.AddFast(new CellTemplate(cellType, new Hex(1, 0), 0), memory1, memory2);
            species.ReproductionMethod = MulticellularReproductionMethod.MassBudding;
            species.MassBuddingCellCount = 2;
        }

        species.OnEdited();
        return species;
    }

    private static void AssertCompounds(Dictionary<Compound, float> expected, Dictionary<Compound, float> actual)
    {
        AssertThat(actual.Count).IsEqual(expected.Count);
        foreach (var entry in expected)
        {
            AssertThat(actual.ContainsKey(entry.Key)).IsTrue();
            AssertThat(actual[entry.Key]).IsEqual(entry.Value);
        }
    }

    /// <summary>
    ///   Produces a changed candidate with known compound amounts and deliberately stale inherited data.
    /// </summary>
    private sealed class CompoundMutation : IMutationStrategy<Species>
    {
        public Dictionary<Species, Dictionary<Compound, float>> Expected { get; } =
            new(ReferenceEqualityComparer.Instance);

        public bool Repeatable => false;

        public List<Mutant> MutationsOf(Species baseSpecies, double mp, bool lawk, Random random,
            BiomeConditions biomeToConsider)
        {
            var candidate = baseSpecies is MulticellularSpecies multicellular ?
                multicellular.Clone(true, false) :
                (Species)baseSpecies.Clone();
            var cytoplasm = SimulationParameters.Instance.GetOrganelleType("cytoplasm");
            if (candidate is MicrobeSpecies microbe)
            {
                AssertThat(AddOrganelleWithStrategy(OrganelleAddStrategy.Spiral, cytoplasm, Direction.Neutral,
                    microbe, [], [], [], random)).IsTrue();
            }
            else
            {
                ((MulticellularSpecies)candidate).ModifiableCellTypes[0].ModifiableOrganelles.Add(
                    new OrganelleTemplate(cytoplasm, new Hex(1, 0), 0));
            }

            // Establish the expected public result before replacing it with deliberately stale inherited data.
            candidate.OnAttemptedInAutoEvo(false);
            Expected.Add(candidate, new Dictionary<Compound, float>(candidate.InitialCompounds));
            candidate.InitialCompounds.Clear();
            candidate.InitialCompounds.Add(Compound.Ammonia, -100);
            return [new Mutant(candidate, mp - 1)];
        }
    }
}
