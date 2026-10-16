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
    public void RejectedAndHiddenCandidatesHaveFreshCompoundsAtInsertionAndAfterSaving(bool multicellular,
        bool massBudding, bool specializedStorage)
    {
        var expectedGlucose = ExpectedGlucose(multicellular, massBudding, specializedStorage, 2);
        var settings = new WorldGenerationSettings
        {
            Seed = 0x5EED,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
            AutoEvoConfiguration = new AutoEvoConfiguration { MutationsPerSpecies = 3 },
        };
        var world = new GameWorld(settings);
        var patch = world.Map.CurrentPatch!;
        var parent = CreateSpecies(multicellular, massBudding, specializedStorage);
        patch.SpeciesInPatch.Clear();
        patch.AddSpecies(parent, 100);

        var results = new RunResults();
        var cache = new SimulationCache(settings);
        var strategy = new CompoundMutation();
        var pressure = new CompoundObservationPressure(strategy, expectedGlucose);

        // A zero-energy empty leaf forces rejection after real insertion. This is a controlled fixture,
        // not evidence of its real-world frequency.
        var miche = new Miche(pressure);
        results.AddNewMicheForPatch(patch, miche);
        var step = new ModifyExistingSpecies(patch, cache, settings, new Random(42));

        // Generate for the one parent, finish generation, then filter. Scoring during these phases is not insertion.
        AssertThat(step.RunStep(results)).IsFalse();
        AssertThat(step.RunStep(results)).IsFalse();
        AssertThat(step.RunStep(results)).IsFalse();
        pressure.ObserveAttempts = true;
        AssertThat(step.RunStep(results)).IsFalse();
        pressure.ObserveAttempts = false;

        AssertThat(pressure.AttemptedCandidates.Count).IsGreater(1);
        AssertThat(miche.Occupant).IsNotNull();
        var occupant = miche.Occupant!;
        AssertThat(occupant).IsNotSame(parent);
        AssertThat(pressure.AttemptedCandidates.Contains(occupant)).IsTrue();
        AssertThat(MichePopulation.CalculatePopulationInPatch(occupant, miche, patch, cache)).IsEqual(0);

        // Only one attempted candidate occupies this leaf; the others were hidden during competition.
        // Every attempt was checked before insertion, including those skipped by FinalApply.
        AssertThat(step.RunStep(results)).IsTrue();
        AssertThat(miche.Occupant).IsNull();
        AssertThat(results.GetPossibleSpeciesList().Count).IsEqual(0);

        using var data = new MemoryStream();
        var manager = new ThriveArchiveManager();
        var writer = new SArchiveMemoryWriter(data, manager);
        manager.OnStartNewWrite(writer);
        writer.WriteObject(results);
        foreach (var candidate in pressure.AttemptedCandidates)
        {
            // Independent candidate archives preserve initialization coverage without keeping rejected occupants
            // in the result tree. Complete only the multicellular layout; do not refresh compounds with OnEdited.
            if (candidate is MulticellularSpecies multicellularCandidate)
            {
                multicellularCandidate.RestoreGameplayCellsForAutoEvo();
                MulticellularLayoutHelpers.UpdateGameplayLayoutForAutoEvo(
                    multicellularCandidate.ModifiableGameplayCells, multicellularCandidate.ModifiableEditorCells,
                    [], [], []);
            }

            AssertCompounds(expectedGlucose, candidate.InitialCompounds);
            writer.WriteObject(candidate);
        }

        manager.OnFinishWrite(writer);
        data.Position = 0;
        var reader = new SArchiveMemoryReader(data, manager);
        manager.OnStartNewRead(reader);
        var loaded = reader.ReadObjectOrNull<RunResults>();
        AssertThat(loaded).IsNotNull();
        var loadedTrees = loaded!.InspectPatchMicheData();
        AssertThat(loadedTrees.Count).IsEqual(1);
        foreach (var loadedTree in loadedTrees.Values)
        {
            AssertThat(loadedTree.Occupant).IsNull();
        }

        foreach (var candidate in pressure.AttemptedCandidates)
        {
            var loadedCandidate = reader.ReadObjectOrNull<Species>();
            AssertThat(loadedCandidate).IsNotNull();
            AssertThat(loadedCandidate).IsNotSame(candidate);
            AssertCompounds(expectedGlucose, loadedCandidate!.InitialCompounds);
        }

        manager.OnFinishRead(reader);
    }

    [TestCase(false, false, false)]
    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(true, true, false)]
    public void OnEditedRefreshesCompoundsAfterDeferredAttempt(bool multicellular, bool massBudding,
        bool specializedStorage)
    {
        var expectedGlucose = ExpectedGlucose(multicellular, massBudding, specializedStorage, 1);
        var parent = CreateSpecies(multicellular, massBudding, specializedStorage);
        var candidate = (Species)parent.Clone();
        candidate.InitialCompounds.Clear();
        candidate.InitialCompounds.Add(Compound.Ammonia, -100);
        candidate.OnAttemptedInAutoEvo(true, false);
        candidate.OnEdited();

        AssertCompounds(expectedGlucose, candidate.InitialCompounds);
        AssertCompounds(expectedGlucose, parent.InitialCompounds);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void DeferredAttemptClearsCompoundsAndDefaultAttemptRefreshesThem(bool multicellular, bool refreshCache)
    {
        var expectedGlucose = ExpectedGlucose(multicellular, false, false, 1);
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

    [TestCase(false)]
    [TestCase(true)]
    public void ApplyingFinalizedMutationCopiesCompounds(bool multicellular)
    {
        var expectedGlucose = ExpectedGlucose(multicellular, false, false, 1);
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

    private static float ExpectedGlucose(bool multicellular, bool massBudding, bool specializedStorage,
        int cytoplasmsPerCell)
    {
        var parameters = SimulationParameters.Instance;
        var cytoplasm = parameters.GetOrganelleType("cytoplasm");
        var glucose = parameters.GetCompoundDefinition(Compound.Glucose);
        AssertThat(glucose.CanBeInitialCompound).IsTrue();
        AssertThat(glucose.IsEnvironmental).IsFalse();
        AssertThat(cytoplasm.RunnableProcesses.Count).IsEqual(1);
        var glycolysis = cytoplasm.RunnableProcesses[0];
        AssertThat(glycolysis.Process.Inputs.Count).IsEqual(1);
        AssertThat(glycolysis.Process.Inputs.ContainsKey(glucose)).IsTrue();
        AssertThat(glycolysis.Process.Outputs.ContainsKey(glucose)).IsFalse();
        foreach (var output in glycolysis.Process.Outputs.Keys)
        {
            AssertThat(output.CanBeInitialCompound).IsFalse();
        }

        AssertThat(cytoplasm.Components.Storage).IsNotNull();
        float capacityPerCell = cytoplasm.Components.Storage!.Capacity * cytoplasmsPerCell;
        if (!multicellular && !specializedStorage)
        {
            // One hex gets no specialization. Two identical one-hex organelles have full concentration and
            // one unit of size-dependent strength, using the cell-type specialization constants.
            AssertThat(cytoplasm.HexCount).IsEqual(1);
            AssertThat(cytoplasm.HasNucleusFeature).IsFalse();
            AssertThat(cytoplasmsPerCell is 1 or 2).IsTrue();
            AssertThat(cytoplasmsPerCell <= Constants.FULL_INITIAL_GLUCOSE_SMALL_SIZE_LIMIT).IsTrue();
            float specialization = cytoplasmsPerCell == 1 ?
                1 :
                1 + Constants.CELL_SPECIALIZATION_STRENGTH_MULTIPLIER / Constants.CELL_SPECIALIZATION_STRENGTH_FULL_AT;
            return capacityPerCell * specialization;
        }

        if (specializedStorage)
        {
            AssertThat(parameters.GetOrganelleType("nucleus").RunnableProcesses.Count).IsEqual(0);
            AssertThat(parameters.GetOrganelleType("vacuole").RunnableProcesses.Count).IsEqual(0);
        }

        // This fixture has one glucose-only process and no environmental rate inputs. Its recipe and organelle
        // rate give an independent demand expectation; no compound-balance or initialization method is called.
        int initialCellCount = massBudding ? 2 : 1;
        float glucoseDemand = glycolysis.Process.Inputs[glucose] * glycolysis.Rate *
            (cytoplasmsPerCell * initialCellCount) * Constants.INITIAL_COMPOUND_TIME;
        if (multicellular)
            glucoseDemand *= Constants.MULTICELLULAR_INITIAL_COMPOUND_MULTIPLIER;

        // Cytoplasm storage alone suffices, even before specialised or other organelle storage is considered.
        AssertThat(glucoseDemand < capacityPerCell * initialCellCount).IsTrue();
        return glucoseDemand;
    }

    private static void AssertCompounds(float expectedGlucose, Dictionary<Compound, float> actual)
    {
        AssertThat(actual.Count).IsEqual(1);
        AssertThat(actual.ContainsKey(Compound.Glucose)).IsTrue();
        AssertThat(actual[Compound.Glucose]).IsGreater(0);
        AssertThat(actual[Compound.Glucose]).IsEqual(expectedGlucose);
    }

    /// <summary>
    ///   Observes initialization only while real candidate insertion is running, including attempts that stay hidden.
    /// </summary>
    private sealed class CompoundObservationPressure : RootPressure
    {
        private readonly CompoundMutation strategy;
        private readonly float expectedGlucose;

        public CompoundObservationPressure(CompoundMutation strategy, float expectedGlucose)
        {
            this.strategy = strategy;
            this.expectedGlucose = expectedGlucose;
            Mutations.Clear();
            Mutations.Add(strategy);
        }

        public bool ObserveAttempts { get; set; }

        public HashSet<Species> AttemptedCandidates { get; } = new(ReferenceEqualityComparer.Instance);

        public override float Score(Species species, Patch patch, SimulationCache cache)
        {
            if (!strategy.Candidates.Contains(species))
                return 1;

            if (ObserveAttempts)
            {
                AssertCompounds(expectedGlucose, species.InitialCompounds);
                AttemptedCandidates.Add(species);
            }

            return 2;
        }
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
