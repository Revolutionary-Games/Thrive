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
    // Two cytoplasms give the small bacterium 1.0 capacity with a 1.05 specialisation bonus.
    // Other fixtures consume 0.007 glucose per cytoplasm for 40 seconds; multicellular species get 1.5 times that.
    [TestCase(false, false, false, 1.05f)]
    [TestCase(false, false, true, 0.007f * 2 * 40)]
    [TestCase(true, false, false, 0.007f * 2 * 40 * 1.5f)]
    [TestCase(true, true, false, 0.007f * 4 * 40 * 1.5f)]
    public void LowPopulationMicheOccupantHasFreshCompoundsAfterSaving(bool multicellular, bool massBudding,
        bool specializedStorage, float expectedGlucose)
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
        var step = new ModifyExistingSpecies(patch, cache, settings, 42);
        var completed = false;
        for (var i = 0; i < 10; ++i)
        {
            if (!step.RunStep(results))
                continue;

            completed = true;
            break;
        }

        AssertThat(completed).IsTrue();
        AssertThat(strategy.Candidates.Count > 0).IsTrue();
        AssertThat(miche.Occupant).IsNotNull();
        var occupant = miche.Occupant!;
        AssertThat(occupant).IsNotSame(parent);
        AssertThat(strategy.Candidates.Contains(occupant)).IsTrue();
        AssertThat(MichePopulation.CalculatePopulationInPatch(occupant, miche, patch, cache)).IsEqual(0);
        AssertCompounds(expectedGlucose, occupant.InitialCompounds);

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
            AssertCompounds(expectedGlucose, loadedTree.Occupant!.InitialCompounds);
        }
    }

    [TestCase(false, false, false, 0.5f)]
    [TestCase(false, false, true, 0.007f * 40)]
    [TestCase(true, false, false, 0.007f * 40 * 1.5f)]
    [TestCase(true, true, false, 0.007f * 2 * 40 * 1.5f)]
    public void OnEditedRefreshesCompoundsAfterDeferredAttempt(bool multicellular, bool massBudding,
        bool specializedStorage, float expectedGlucose)
    {
        var parent = CreateSpecies(multicellular, massBudding, specializedStorage);
        var candidate = (Species)parent.Clone();
        candidate.InitialCompounds.Clear();
        candidate.InitialCompounds.Add(Compound.Ammonia, -100);
        candidate.OnAttemptedInAutoEvo(true, false);
        candidate.OnEdited();

        AssertCompounds(expectedGlucose, candidate.InitialCompounds);
        AssertCompounds(expectedGlucose, parent.InitialCompounds);
    }

    [TestCase(false, false, 0.5f)]
    [TestCase(false, true, 0.5f)]
    [TestCase(true, false, 0.007f * 40 * 1.5f)]
    [TestCase(true, true, 0.007f * 40 * 1.5f)]
    public void DeferredAttemptClearsCompoundsAndDefaultAttemptRefreshesThem(bool multicellular, bool refreshCache,
        float expectedGlucose)
    {
        var parent = CreateSpecies(multicellular, false, false);
        var candidate = (Species)parent.Clone();
        candidate.InitialCompounds.Clear();
        candidate.InitialCompounds.Add(Compound.Ammonia, -100);
        candidate.OnAttemptedInAutoEvo(refreshCache, false);

        AssertThat(candidate.InitialCompounds.Count).IsEqual(0);
        AssertCompounds(expectedGlucose, parent.InitialCompounds);

        candidate.OnAttemptedInAutoEvo(refreshCache);

        AssertCompounds(expectedGlucose, candidate.InitialCompounds);
        AssertCompounds(expectedGlucose, parent.InitialCompounds);
    }

    [TestCase(false, 0.5f)]
    [TestCase(true, 0.007f * 40 * 1.5f)]
    public void ApplyingFinalizedMutationCopiesCompounds(bool multicellular, float expectedGlucose)
    {
        var parent = CreateSpecies(multicellular, false, false);
        var candidate = (Species)parent.Clone();
        candidate.OnAttemptedInAutoEvo(true, false);
        candidate.UpdateInitialCompounds();
        AssertCompounds(expectedGlucose, candidate.InitialCompounds);
        parent.InitialCompounds.Clear();
        parent.InitialCompounds.Add(Compound.Ammonia, -100);

        parent.ApplyMutation(candidate);

        AssertCompounds(expectedGlucose, parent.InitialCompounds);
        AssertCompounds(expectedGlucose, candidate.InitialCompounds);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SavingSpeciesWithEmptyCompoundsPreservesEmptyCompounds(bool multicellular)
    {
        var candidate = CreateSpecies(multicellular, false, false);
        candidate.InitialCompounds.Clear();
        using var data = new MemoryStream();
        var manager = new ThriveArchiveManager();
        var writer = new SArchiveMemoryWriter(data, manager);
        manager.OnStartNewWrite(writer);
        writer.WriteObject(candidate);
        manager.OnFinishWrite(writer);
        data.Position = 0;
        var reader = new SArchiveMemoryReader(data, manager);
        manager.OnStartNewRead(reader);
        var loaded = reader.ReadObjectOrNull<Species>();
        manager.OnFinishRead(reader);

        AssertThat(loaded).IsNotNull();
        AssertThat(loaded!.InitialCompounds.Count).IsEqual(0);
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

    private static void AssertCompounds(float expectedGlucose, Dictionary<Compound, float> actual)
    {
        AssertThat(actual.Count).IsEqual(1);
        AssertThat(actual.ContainsKey(Compound.Glucose)).IsTrue();
        AssertThat(actual[Compound.Glucose]).IsGreater(0);
        AssertThat(actual[Compound.Glucose]).IsEqual(expectedGlucose);
    }

    /// <summary>
    ///   Produces a changed candidate with known compound amounts and deliberately stale inherited data.
    /// </summary>
    private sealed class CompoundMutation : IMutationStrategy<Species>
    {
        public HashSet<Species> Candidates { get; } = new(ReferenceEqualityComparer.Instance);

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

            Candidates.Add(candidate);
            candidate.InitialCompounds.Clear();
            candidate.InitialCompounds.Add(Compound.Ammonia, -100);
            return [new Mutant(candidate, mp - 1)];
        }
    }
}
