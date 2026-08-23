using System.Collections.Generic;
using AutoEvo;
using GdUnit4;
using SharedBase.Archive;
using Xoshiro.PRNG64;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class MacroscopicSpeciesCompatibilityTests
{
    private const long WorldSeed = 0x5EED;
    private const long TestPopulation = 100;

    [TestCase]
    public void MicheInsert_MacroscopicSpeciesReturnsFalseBeforeScoring()
    {
        var fixture = CreateSpeciesFixture();
        var cache = new SimulationCache(fixture.WorldSettings);
        var pressure = new RecordingSelectionPressure();
        var originalOccupant = fixture.Microbe;
        var miche = new Miche(pressure)
        {
            Occupant = originalOccupant,
        };

        var inserted = miche.InsertSpecies(fixture.Macroscopic, fixture.Patch, null, cache, false,
            new Miche.InsertWorkingMemory());

        AssertThat(pressure.ScoreCalls).IsEqual(0);
        AssertThat(inserted).IsFalse();
        AssertThat(miche.Occupant).IsSame(originalOccupant);

        AssertSupportedSpeciesReachesScoring(fixture.Microbe, fixture.Patch, cache);
        AssertSupportedSpeciesReachesScoring(fixture.Multicellular, fixture.Patch, cache);
    }

    [TestCase]
    public void GenerateMiche_MacroscopicOnlyPatchSkipsUnsupportedSpecies()
    {
        var fixture = CreateSpeciesFixture();
        var cache = new SimulationCache(fixture.WorldSettings);
        var results = new RunResults();
        var generateMiche = new GenerateMiche(fixture.Patch, cache, fixture.World.AutoEvoGlobalCache);

        var completed = generateMiche.RunStep(results);

        var occupants = new HashSet<Species>();
        results.GetModifiableMicheForPatch(fixture.Patch).GetOccupants(occupants);

        AssertThat(completed).IsTrue();
        AssertThat(occupants.Contains(fixture.Macroscopic)).IsFalse();
    }

    [TestCase]
    public void GenerateMiche_MixedPatchExcludesMacroscopicPredationInputs()
    {
        var fixture = CreateSpeciesFixture();
        fixture.Patch.SpeciesInPatch.Clear();
        fixture.Patch.AddSpecies(fixture.Microbe, TestPopulation);
        fixture.Patch.AddSpecies(fixture.Multicellular, TestPopulation);
        fixture.Patch.AddSpecies(fixture.Macroscopic, TestPopulation);

        var cache = new SimulationCache(fixture.WorldSettings);
        var generateMiche = new GenerateMiche(fixture.Patch, cache, fixture.World.AutoEvoGlobalCache);
        var generatedMiche = generateMiche.GenerateMicheTree(fixture.World.AutoEvoGlobalCache);
        var predationPressures = new List<PredationEffectivenessPressure>();
        CollectPredationPressures(generatedMiche, predationPressures);
        var preySpecies = new HashSet<Species>();

        foreach (var pressure in predationPressures)
            preySpecies.Add(pressure.Prey);

        AssertThat(predationPressures.Count).IsEqual(2);
        AssertThat(preySpecies.Contains(fixture.Microbe)).IsTrue();
        AssertThat(preySpecies.Contains(fixture.Multicellular)).IsTrue();
        AssertThat(preySpecies.Contains(fixture.Macroscopic)).IsFalse();

        var populatedMiche = generateMiche.PopulateMiche(generatedMiche);
        var occupants = new HashSet<Species>();
        populatedMiche.GetOccupants(occupants);

        AssertThat(populatedMiche).IsSame(generatedMiche);
        AssertThat(occupants.Contains(fixture.Macroscopic)).IsFalse();

        var generalAvoidPressure = fixture.World.AutoEvoGlobalCache.GeneralAvoidPredationSelectionPressure;
        AssertThat(generalAvoidPressure.Score(fixture.Microbe, fixture.Patch, cache) > 0).IsTrue();
        AssertThat(generalAvoidPressure.Score(fixture.Multicellular, fixture.Patch, cache) > 0).IsTrue();
    }

    [TestCase]
    public void GenerateMiche_OneSupportedSpeciesPlusMacroscopicDoesNotCreatePredationBranch()
    {
        var fixture = CreateSpeciesFixture();
        fixture.Patch.SpeciesInPatch.Clear();
        fixture.Patch.AddSpecies(fixture.Microbe, TestPopulation);
        fixture.Patch.AddSpecies(fixture.Macroscopic, TestPopulation);

        var cache = new SimulationCache(fixture.WorldSettings);
        var generateMiche = new GenerateMiche(fixture.Patch, cache, fixture.World.AutoEvoGlobalCache);
        var generatedMiche = generateMiche.GenerateMicheTree(fixture.World.AutoEvoGlobalCache);
        var predationPressures = new List<PredationEffectivenessPressure>();
        CollectPredationPressures(generatedMiche, predationPressures);

        AssertThat(predationPressures.Count).IsEqual(0);
    }

    [TestCase]
    public void MichePopulation_MacroscopicPopulationIsPreserved()
    {
        var fixture = CreateSpeciesFixture();
        fixture.Patch.SpeciesInPatch.Clear();
        fixture.Patch.AddSpecies(fixture.Microbe, TestPopulation);
        fixture.Patch.AddSpecies(fixture.Macroscopic, TestPopulation);

        var cache = new SimulationCache(fixture.WorldSettings);
        var results = new RunResults();
        var generateMiche = new GenerateMiche(fixture.Patch, cache, fixture.World.AutoEvoGlobalCache);
        AssertThat(generateMiche.RunStep(results)).IsTrue();

        var simulationConfiguration = new SimulationConfiguration(fixture.WorldSettings.AutoEvoConfiguration,
            fixture.World.Map, fixture.WorldSettings)
        {
            Results = results,
            CollectEnergyInformation = true,
            PatchesToRun = new HashSet<Patch> { fixture.Patch },
        };

        MichePopulation.Simulate(simulationConfiguration, cache, new XoShiRo256starstar(WorldSeed));

        AssertThat(results.GetPopulationInPatch(fixture.Macroscopic, fixture.Patch)).IsEqual(TestPopulation);

        var microbeEnergyResults = results.GetPatchEnergyResults(fixture.Microbe);
        AssertThat(microbeEnergyResults.ContainsKey(fixture.Patch)).IsTrue();
        AssertThat(microbeEnergyResults[fixture.Patch].IndividualCost > 0).IsTrue();
    }

    [TestCase]
    public void AutoEvoRun_AfterMacroscopicConversionCompletesMicheGenerationWithoutAbort()
    {
        var microbePlayer = CreateMicrobeSpecies(1, "player");
        var world = new GameWorld(CreateWorldSettings(), microbePlayer);
        var multicellularPlayer = world.ChangeSpeciesToMulticellular(microbePlayer, true);
        var populationsBeforeConversion = new Dictionary<Patch, long>();

        foreach (var patch in world.Map.Patches.Values)
        {
            if (patch.SpeciesInPatch.TryGetValue(multicellularPlayer, out var population))
                populationsBeforeConversion.Add(patch, population);
        }

        AssertThat(populationsBeforeConversion.Count > 0).IsTrue();

        var macroscopicPlayer = world.ChangeSpeciesToMacroscopic(multicellularPlayer);
        AssertThat(world.PlayerSpecies).IsSame(macroscopicPlayer);

        foreach (var (patch, population) in populationsBeforeConversion)
        {
            AssertThat(patch.SpeciesInPatch.ContainsKey(multicellularPlayer)).IsFalse();
            AssertThat(patch.SpeciesInPatch.TryGetValue(macroscopicPlayer, out var convertedPopulation)).IsTrue();
            AssertThat(convertedPopulation).IsEqual(population);
        }

        var run = new AutoEvoRun(world, world.AutoEvoGlobalCache);

        run.OneStep();
        AssertThat(run.Aborted).IsFalse();

        for (var i = 0; i < world.Map.Patches.Count; ++i)
        {
            run.OneStep();
            AssertThat(run.Aborted).IsFalse();
        }
    }

    private static void AssertSupportedSpeciesReachesScoring(Species species, Patch patch, SimulationCache cache)
    {
        var pressure = new RecordingSelectionPressure();
        var miche = new Miche(pressure);

        var inserted = miche.InsertSpecies(species, patch, null, cache, false, new Miche.InsertWorkingMemory());

        AssertThat(inserted).IsTrue();
        AssertThat(pressure.ScoreCalls).IsEqual(1);
        AssertThat(miche.Occupant).IsSame(species);
    }

    private static SpeciesFixture CreateSpeciesFixture()
    {
        var worldSettings = CreateWorldSettings();

        var microbe = CreateMicrobeSpecies(1, "microbe");
        var world = new GameWorld(worldSettings, microbe);
        var multicellularSource = world.NewMicrobeSpecies("Test", "multicellular");
        ConfigureMicrobeSpecies(multicellularSource);
        var multicellular = world.ChangeSpeciesToMulticellular(multicellularSource, false);
        var macroscopicSource = world.NewMicrobeSpecies("Test", "macroscopic");
        ConfigureMicrobeSpecies(macroscopicSource);
        var macroscopicIntermediate = world.ChangeSpeciesToMulticellular(macroscopicSource, false);
        var macroscopic = world.ChangeSpeciesToMacroscopic(macroscopicIntermediate);
        var patch = world.Map.CurrentPatch!;

        patch.SpeciesInPatch.Clear();
        patch.AddSpecies(macroscopic, TestPopulation);

        return new SpeciesFixture(worldSettings, world, patch, microbe, multicellular, macroscopic);
    }

    private static WorldGenerationSettings CreateWorldSettings()
    {
        return new WorldGenerationSettings
        {
            Seed = WorldSeed,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        };
    }

    private static MicrobeSpecies CreateMicrobeSpecies(uint id, string epithet)
    {
        var microbe = new MicrobeSpecies(id, "Test", epithet);
        ConfigureMicrobeSpecies(microbe);

        return microbe;
    }

    private static void ConfigureMicrobeSpecies(MicrobeSpecies microbe)
    {
        var simulationParameters = SimulationParameters.Instance;
        microbe.IsBacteria = false;
        microbe.MembraneType = simulationParameters.GetMembrane("single");

        microbe.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("nucleus"),
            new Hex(0, 0), 0));
        microbe.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"),
            new Hex(3, 0), 0));
        microbe.OnEdited();
    }

    private static void CollectPredationPressures(Miche miche,
        ICollection<PredationEffectivenessPressure> pressures)
    {
        if (miche.Pressure is PredationEffectivenessPressure predationPressure)
            pressures.Add(predationPressure);

        foreach (var child in miche.Children)
        {
            CollectPredationPressures(child, pressures);
        }
    }

    private sealed record SpeciesFixture(WorldGenerationSettings WorldSettings, GameWorld World, Patch Patch,
        MicrobeSpecies Microbe, MulticellularSpecies Multicellular, MacroscopicSpecies Macroscopic);

    private sealed class RecordingSelectionPressure : SelectionPressure
    {
        private static readonly LocalizedString PressureName = new("TEST_RECORDING_SELECTION_PRESSURE");

        public RecordingSelectionPressure() : base(1, [])
        {
        }

        public int ScoreCalls { get; private set; }

        public override LocalizedString Name => PressureName;

        public override ushort CurrentArchiveVersion => 1;

        public override ArchiveObjectType ArchiveObjectType =>
            (ArchiveObjectType)ThriveArchiveObjectType.RootPressure;

        public override float Score(Species species, Patch patch, SimulationCache cache)
        {
            ++ScoreCalls;
            return 1;
        }

        public override float GetEnergy(Patch patch)
        {
            return 0;
        }
    }
}
