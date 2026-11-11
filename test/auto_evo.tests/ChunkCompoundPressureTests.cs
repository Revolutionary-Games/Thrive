using System;
using System.Collections.Generic;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   Protects chunk-food mechanisms with current configuration data and isolated scoring inputs.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class ChunkCompoundPressureTests
{
    private const string ChunkType = "chunkScoreTest";
    private const float RelativeTolerance = 1e-5f;

    /// <summary>
    ///   Sensing the compound in the food helps collection; sensing an unrelated compound does not.
    ///   The microbes differ only in the receptor target, and ATP supply does not limit either score.
    /// </summary>
    [TestCase]
    public void MatchingChunkReceptorImprovesCollection()
    {
        var patch = CreatePatch();
        var pressure = CreatePressure();
        var matching = CreateReceptorMicrobe(Compound.Glucose);
        var mismatching = CreateReceptorMicrobe(Compound.Ammonia);
        var diagnostic = CreateCache();
        SimulationCacheTestFixtures.AssertBits(diagnostic.GetSpeedForSpecies(matching),
            diagnostic.GetSpeedForSpecies(mismatching));
        SimulationCacheTestFixtures.AssertBits(matching.NominalStorageCapacity, mismatching.NominalStorageCapacity);
        var matchingScore = ScoreSaturated(pressure, matching, patch);
        var mismatchingScore = ScoreSaturated(pressure, mismatching, patch);
        AssertThat(mismatchingScore).IsGreater(0.0f);
        AssertThat(matchingScore).IsGreater(mismatchingScore);
    }

    /// <summary>
    ///   A receptor cannot help find chunks that do not spawn. Other collection contributions can remain.
    /// </summary>
    [TestCase]
    public void MatchingReceptorProvidesNoAdvantageForAbsentChunks()
    {
        var patch = CreatePatch(density: 0);
        var pressure = CreatePressure();
        var matching = CreateReceptorMicrobe(Compound.Glucose);
        var mismatching = CreateReceptorMicrobe(Compound.Ammonia);
        var matchingScore = ScoreSaturated(pressure, matching, patch);
        var mismatchingScore = ScoreSaturated(pressure, mismatching, patch);
        AssertThat(matchingScore).IsGreater(0.0f);
        SimulationCacheTestFixtures.AssertBits(matchingScore, mismatchingScore);
    }

    /// <summary>
    ///   A membrane that cannot engulf must rely on leakage even when a chunk is small enough to swallow.
    /// </summary>
    [TestCase]
    public void NonEngulfingMembraneStillCollectsLeakedFood()
    {
        AssertFeedingModesAreDistinguishable();
        var pressure = CreatePressure();
        var species = CreateMicrobe("cytoplasm");
        species.MembraneType = SimulationParameters.Instance.GetMembrane("cellulose");
        species.OnEdited();
        var size = CreateCache().GetBaseHexSizeForSpecies(species);
        var smallChunk = CreatePatch(chunkSize: size / (2 * Constants.ENGULF_SIZE_RATIO_REQ));
        var largeChunk = CreatePatch(chunkSize: size * 2 / Constants.ENGULF_SIZE_RATIO_REQ);
        AssertThat(species.CanEngulf).IsFalse();
        AssertThat(size).IsGreater(smallChunk.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        AssertThat(size).IsLess(largeChunk.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        var smallChunkScore = ScoreSaturated(pressure, species, smallChunk);
        var largeChunkScore = ScoreSaturated(pressure, species, largeChunk);
        AssertThat(smallChunkScore).IsGreater(0.0f);
        SimulationCacheTestFixtures.AssertBits(smallChunkScore, largeChunkScore);
    }

    /// <summary>
    ///   Defining a larger cell type does not help a species eat larger chunks until it uses that type in its body.
    /// </summary>
    [TestCase]
    public void UnusedCellTypeCannotGrantEngulfingCapability()
    {
        AssertFeedingModesAreDistinguishable();
        var pressure = CreatePressure();
        var small = CreateCellType("Small", "cytoplasm");
        var large = CreateCellType("UnusedLarge", "cytoplasm", "cytoplasm", "cytoplasm");
        var diagnostic = CreateCache();
        var smallSize = diagnostic.GetBaseHexSizeForCellType(small);
        var largeSize = diagnostic.GetBaseHexSizeForCellType(large);
        var patch = CreatePatch(chunkSize: (smallSize + largeSize) / (2 * Constants.ENGULF_SIZE_RATIO_REQ));
        AssertThat(smallSize).IsLess(patch.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        var species = CreateMulticellular(small);
        var score = ScoreFresh(pressure, species, patch);
        AssertThat(score).IsGreater(0.0f);
        species.ModifiableCellTypes.Add(large);
        species.OnEdited();
        AssertThat(large.MembraneType.CanEngulf).IsTrue();
        AssertThat(CreateCache().GetBaseHexSizeForCellType(large))
            .IsGreater(patch.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        AssertThat(species.EditorCells.Count).IsEqual(1);
        SimulationCacheTestFixtures.AssertBits(ScoreFresh(pressure, species, patch), score);
    }

    /// <summary>
    ///   A cell type used in the body lets the species engulf chunks within that cell's size limit.
    /// </summary>
    [TestCase]
    public void PresentCellTypeGrantsEngulfingCapability()
    {
        AssertFeedingModesAreDistinguishable();
        var pressure = CreatePressure();
        var large = CreateCellType("PresentLarge", "cytoplasm", "cytoplasm", "cytoplasm");
        var size = CreateCache().GetBaseHexSizeForCellType(large);
        var smallChunk = CreatePatch(chunkSize: size / (2 * Constants.ENGULF_SIZE_RATIO_REQ));
        var largeChunk = CreatePatch(chunkSize: size * 2 / Constants.ENGULF_SIZE_RATIO_REQ);
        var species = CreateMulticellular(large);
        AssertThat(large.MembraneType.CanEngulf).IsTrue();
        AssertThat(species.EditorCells.Count).IsEqual(1);
        foreach (var cell in species.EditorCells)
            AssertThat(ReferenceEquals(cell.Data?.CellType, large)).IsTrue();

        AssertThat(size).IsGreater(smallChunk.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        AssertThat(size).IsLess(largeChunk.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        var smallChunkScore = ScoreSaturated(pressure, species, smallChunk);
        var largeChunkScore = ScoreSaturated(pressure, species, largeChunk);
        AssertThat(largeChunkScore).IsGreater(0.0f);
        AssertThat(smallChunkScore).IsNotEqual(largeChunkScore);
    }

    /// <summary>
    ///   Depending on a resource that disappears at night reduces collection for a less active species.
    ///   Only the day/night setting changes; available ATP is kept sufficient in both runs.
    /// </summary>
    [TestCase]
    public void DayNightCycleReducesCollectionForLessActiveSpeciesUsingVaryingInputs()
    {
        var patch = CreatePatch(varyingSunlight: true);
        var species = CreateMicrobe("cytoplasm", "chromatophore");
        SetBehaviour(species, Constants.AI_ACTIVITY_TO_BE_FULLY_ACTIVE_DURING_NIGHT / 2, 0);
        AssertThat(species.Behaviour.Activity).IsGreater(0.0f);
        AssertThat(Constants.AUTO_EVO_MAX_NIGHT_SESSILITY_COLLECTING_PENALTY).IsLess(1.0f);
        AssertThat(CreateCache().GetUsesVaryingCompoundsForSpecies(species, patch.Biome)).IsTrue();
        var disabled = ScoreSaturated(CreatePressure(), species, patch);
        var enabled = ScoreSaturated(CreatePressure(dayNight: true), species, patch);
        AssertThat(enabled).IsGreater(0.0f);
        AssertThat(enabled).IsLess(disabled);
    }

    [TestCase]
    public void DayNightPenaltyRequiresEnabledCycleAndVaryingInput()
    {
        var species = CreateMicrobe("cytoplasm", "chromatophore");
        SetBehaviour(species, Constants.AI_ACTIVITY_TO_BE_FULLY_ACTIVE_DURING_NIGHT / 2, 0);
        var varyingPatch = CreatePatch(varyingSunlight: true);
        var steadyPatch = CreatePatch();
        AssertThat(CreateCache().GetUsesVaryingCompoundsForSpecies(species, varyingPatch.Biome)).IsTrue();
        AssertThat(CreateCache().GetUsesVaryingCompoundsForSpecies(species, steadyPatch.Biome)).IsFalse();
        var disabledVarying = ScoreSaturated(CreatePressure(), species, varyingPatch);
        var disabledSteady = ScoreSaturated(CreatePressure(), species, steadyPatch);
        var enabledSteady = ScoreSaturated(CreatePressure(dayNight: true), species, steadyPatch);
        SimulationCacheTestFixtures.AssertBits(disabledVarying, disabledSteady);
        SimulationCacheTestFixtures.AssertBits(enabledSteady, disabledSteady);
    }

    /// <summary>
    ///   Food that cannot supply ATP is unusable. Insufficient ATP limits collection, while meeting or exceeding
    ///   consumption removes that limit. The amount of the penalty is deliberately not fixed here.
    /// </summary>
    [TestCase]
    public void DirectAtpSupportHandlesZeroInsufficientEqualAndSurplusProduction()
    {
        var species = CreateMicrobe("cytoplasm");
        var patch = CreatePatch();
        var production = ExpectedDirectAtp(species, patch);
        var pressure = CreatePressure();
        var saturated = ScoreWithConsumption(pressure, species, patch, production / 2);

        // Control consumption in this test-owned cache to exercise exact equality.
        var diagnostic = CreateCache();
        diagnostic.GetEnergyBalanceForSpecies(species, patch.Biome).TotalConsumption = production;
        var actualProduction = diagnostic.GetCompoundGeneratedFrom(SimulationParameters.GetCompound(Compound.Glucose),
            SimulationParameters.GetCompound(Compound.ATP), species, patch.Biome);
        var consumption = diagnostic.GetEnergyBalanceForSpecies(species, patch.Biome).TotalConsumption;
        SimulationCacheTestFixtures.AssertBits(actualProduction, consumption);
        AssertThat(actualProduction / consumption).IsEqual(1.0f);
        SimulationCacheTestFixtures.AssertBits(pressure.Score(species, patch, diagnostic), saturated);

        var under = ScoreWithConsumption(pressure, species, patch, production * 2);
        AssertThat(saturated).IsGreater(0.0f);
        AssertThat(under).IsGreater(0.0f);
        AssertThat(under).IsLess(saturated);

        var zeroPatch = CreatePatch(compoundAmount: 0, glucoseAmount: 0);
        AssertThat(CreateCache().GetCompoundGeneratedFrom(SimulationParameters.GetCompound(Compound.Glucose),
            SimulationParameters.GetCompound(Compound.ATP), species, zeroPatch.Biome)).IsEqual(0.0f);
        AssertThat(ScoreWithConsumption(pressure, species, zeroPatch, production)).IsEqual(0.0f);
    }

    /// <summary>
    ///   Food can support collection by first making an intermediate compound that can be converted to ATP.
    ///   This protects that route and its energy limit without fixing how conversion efficiency is calculated.
    /// </summary>
    [TestCase]
    public void IntermediateAtpSupportRequiresFoodAndSaturatesWhenConsumptionIsMet()
    {
        var species = CreateMicrobe("cytoplasm", "chemoSynthesizingProteins");
        AssertProcessLayout(species, "glycolysis_cytoplasm", "glycolysis", "bacterial_ChemoSynthesis");
        var glucose = SimulationParameters.GetCompound(Compound.Glucose);
        var atp = SimulationParameters.GetCompound(Compound.ATP);
        var hydrogenSulfide = SimulationParameters.GetCompound(Compound.Hydrogensulfide);
        var synthesis = SimulationParameters.Instance.GetBioProcess("bacterial_ChemoSynthesis");
        var patch = CreatePatch(Compound.Hydrogensulfide,
            carbonDioxide: synthesis.Inputs[SimulationParameters.GetCompound(Compound.Carbondioxide)]);
        var diagnostic = CreateCache();
        AssertThat(diagnostic.GetCompoundGeneratedFrom(hydrogenSulfide, atp, species, patch.Biome)).IsEqual(0.0f);
        var generatedGlucose = diagnostic.GetCompoundGeneratedFrom(hydrogenSulfide, glucose, species, patch.Biome);
        var efficiency = diagnostic.GetCompoundConversionScoreForSpecies(glucose, atp, species);
        AssertThat(generatedGlucose).IsGreater(0.0f);
        AssertThat(efficiency).IsGreater(0.0f);

        // Use the observed ATP supply only to place consumption on either side of the limit.
        // Expected scores are relationships: no food gives zero, shortage reduces the score, and surplus plateaus.
        var production = generatedGlucose * efficiency;
        var pressure = CreatePressure(Compound.Hydrogensulfide, Compound.Glucose);
        var saturated = ScoreWithConsumption(pressure, species, patch, production / 2);
        var moreSurplus = ScoreWithConsumption(pressure, species, patch, production / 4);
        var under = ScoreWithConsumption(pressure, species, patch, production * 2);
        AssertThat(under).IsGreater(0.0f);
        AssertThat(under).IsLess(saturated);
        SimulationCacheTestFixtures.AssertBits(moreSurplus, saturated);

        var zeroPatch = CreatePatch(Compound.Hydrogensulfide, compoundAmount: 0, hydrogenSulfideAmount: 0);
        AssertThat(diagnostic.GetCompoundGeneratedFrom(hydrogenSulfide, glucose, species, zeroPatch.Biome))
            .IsEqual(0.0f);
        AssertThat(ScoreWithConsumption(pressure, species, zeroPatch, production)).IsEqual(0.0f);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void StorageContributesToCollectionForBothSpeciesTypes(bool multicellular)
    {
        var smaller = CreateStorageSpecies(multicellular, 1);
        var larger = CreateStorageSpecies(multicellular, 5);
        var smallerStorage = NominalStorage(smaller);
        var largerStorage = NominalStorage(larger);
        AssertThat(largerStorage).IsGreater(smallerStorage);
        var diagnostic = CreateCache();
        SimulationCacheTestFixtures.AssertBits(diagnostic.GetSpeedForSpecies(smaller),
            diagnostic.GetSpeedForSpecies(larger));

        var patch = CreatePatch(chunkSize:
            diagnostic.GetBaseHexSizeForSpecies(smaller) / (2 * Constants.ENGULF_SIZE_RATIO_REQ));
        var pressure = CreatePressure();
        var smallerScore = ScoreSaturated(pressure, smaller, patch);
        var largerScore = ScoreSaturated(pressure, larger, patch);
        AssertThat(smallerScore).IsGreater(0.0f);
        AssertThat(largerScore).IsGreater(smallerScore);
    }

    /// <summary>
    ///   With unchanged inputs, cache history must not change the score. Direct calls exercise cached ingredients;
    ///   the wrapper also caches the completed score. This does not test formulas or invalidation after edits.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void ScoreIsUnchangedAcrossColdWarmAndClearedCaches(bool multicellular)
    {
        Species species;
        Patch patch;
        ChunkCompoundPressure pressure;
        if (multicellular)
        {
            species = CreateMulticellular(CreateCellType("Intermediate", "cytoplasm", "chemoSynthesizingProteins"));
            patch = CreatePatch(Compound.Hydrogensulfide,
                CreateCache().GetBaseHexSizeForSpecies(species) / (2 * Constants.ENGULF_SIZE_RATIO_REQ),
                carbonDioxide: 0.001f);
            pressure = CreatePressure(Compound.Hydrogensulfide, Compound.Glucose);
        }
        else
        {
            species = CreateReceptorMicrobe(Compound.Glucose);
            patch = CreatePatch();
            pressure = CreatePressure();
        }

        SetBehaviour(species, 237, 141);
        species.ModifiableBehaviour.Aggression = 85;
        species.ModifiableBehaviour.Opportunism = 219;
        species.ModifiableBehaviour.Fear = 75;
        AssertLifecycle(pressure, species, patch);
    }

    [TestCase]
    public void MissingChunkIsRejected()
    {
        var species = CreateMicrobe("cytoplasm");
        var patch = CreatePatch();
        var pressure = CreatePressure();
        var cache = CreateCache();
        patch.Biome.Chunks.Clear();
        AssertThrown(() => pressure.Score(species, patch, cache)).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    public void UnsupportedSpeciesIsRejected()
    {
        var unsupported = new MacroscopicSpecies(1900, "Chunk", "Unsupported");
        var patch = CreatePatch();
        var pressure = CreatePressure();
        var cache = CreateCache();
        AssertThrown(() => pressure.Score(unsupported, patch, cache)).IsInstanceOf<ArgumentException>();
    }

    private static SimulationCache CreateCache()
    {
        return SimulationCacheTestFixtures.CreateCache();
    }

    private static ChunkCompoundPressure CreatePressure(Compound input = Compound.Glucose,
        Compound output = Compound.ATP, bool dayNight = false)
    {
        return new ChunkCompoundPressure(ChunkType, new LocalizedString("MARINE_SNOW"), input, output, dayNight, 1);
    }

    private static MicrobeSpecies CreateMicrobe(params string[] organelles)
    {
        var species = SimulationCacheTestFixtures.CreateMicrobe(1800, "Chunk", "single", organelles);
        ConfigureSpecies(species);
        return species;
    }

    private static MicrobeSpecies CreateReceptorMicrobe(Compound target)
    {
        var species = CreateMicrobe("cytoplasm", "chemoreceptor");
        species.Organelles.Organelles[1].ModifiableUpgrades = new OrganelleUpgrades
        {
            CustomUpgradeData = new ChemoreceptorUpgrades(target, null,
                Constants.CHEMORECEPTOR_RANGE_DEFAULT, Constants.CHEMORECEPTOR_AMOUNT_DEFAULT,
                SimulationParameters.GetCompound(target).Colour),
        };
        species.OnEdited();
        return species;
    }

    private static CellType CreateCellType(string name, params string[] organelles)
    {
        var templates = new OrganelleTemplate[organelles.Length];
        for (var i = 0; i < organelles.Length; ++i)
            templates[i] = SimulationCacheTestFixtures.CreateOrganelle(organelles[i], new Hex(i * 4, 0));
        return SimulationCacheTestFixtures.CreateCellType(name, "single", templates);
    }

    private static MulticellularSpecies CreateMulticellular(CellType cellType)
    {
        var species = SimulationCacheTestFixtures.CreateMulticellular(1801, "Chunk", (cellType, new Hex(0, 0)));
        ConfigureSpecies(species);
        return species;
    }

    private static void ConfigureSpecies(Species species)
    {
        species.ModifiableTolerances.PreferredTemperature = 15;
        species.ModifiableTolerances.TemperatureTolerance = 3;
        species.ModifiableTolerances.PressureMinimum = 71325;
        species.ModifiableTolerances.PressureTolerance = 2000000;
        species.ModifiableTolerances.OxygenResistance = 1;
        species.ModifiableTolerances.UVResistance = 1;
        SetBehaviour(species, Constants.MAX_SPECIES_ACTIVITY, 0);
        species.OnEdited();
        AssertThat(species.PlayerSpecies).IsFalse();
    }

    private static void SetBehaviour(Species species, float activity, float focus)
    {
        species.ModifiableBehaviour.Activity = activity;
        species.ModifiableBehaviour.Focus = focus;
        species.ModifiableBehaviour.Aggression = 0;
        species.ModifiableBehaviour.Opportunism = 0;
        species.ModifiableBehaviour.Fear = 0;
    }

    private static Patch CreatePatch(Compound input = Compound.Glucose, float chunkSize = 10, float density = 1,
        float compoundAmount = 1, float carbonDioxide = 0.09f,
        float glucoseAmount = 1, float hydrogenSulfideAmount = 1, bool varyingSunlight = false)
    {
        var template = SimulationParameters.Instance.GetBiome("aavolcanic_vent");
        var biome = (BiomeConditions)template.Conditions.Clone();
        biome.Pressure = 101325;
        SetCompound(biome, Compound.Temperature, new BiomeCompoundProperties { Ambient = 15 });
        SetCompound(biome, Compound.Carbondioxide, new BiomeCompoundProperties { Ambient = carbonDioxide });
        SetCompound(biome, Compound.Oxygen, new BiomeCompoundProperties { Ambient = 0 });
        SetCompound(biome, Compound.Sunlight, new BiomeCompoundProperties { Ambient = 1 });
        biome.AverageCompounds[Compound.Sunlight] = new BiomeCompoundProperties { Ambient = 0.5f };
        if (varyingSunlight)
            biome.MinimumCompounds[Compound.Sunlight] = new BiomeCompoundProperties { Ambient = 0 };
        SetCompound(biome, Compound.Glucose, new BiomeCompoundProperties { Amount = glucoseAmount, Density = 1 });
        SetCompound(biome, Compound.Hydrogensulfide,
            new BiomeCompoundProperties { Amount = hydrogenSulfideAmount, Density = 1 });
        biome.Chunks = new Dictionary<string, ChunkConfiguration>
        {
            [ChunkType] = new()
            {
                Name = "ChunkScoreTest",
                Meshes = [],
                Size = chunkSize,
                Density = density,
                Compounds = new Dictionary<Compound, ChunkConfiguration.ChunkCompound>
                {
                    [input] = new() { Amount = compoundAmount },
                },
            },
        };
        return new Patch(new LocalizedString("ChunkScoreTest"), 1, template, BiomeType.Vents,
            new PatchSnapshot(biome, template.Background), 1);
    }

    private static void SetCompound(BiomeConditions biome, Compound compound, BiomeCompoundProperties value)
    {
        biome.ChangeableCompounds[compound] = value;
        biome.AverageCompounds[compound] = value;
        biome.MinimumCompounds[compound] = value;
    }

    private static float ExpectedDirectAtp(MicrobeSpecies species, Patch patch)
    {
        AssertProcessLayout(species, "glycolysis_cytoplasm");
        var cache = CreateCache();
        var process = cache.GetActiveProcessList(species)[0];
        var glucose = SimulationParameters.GetCompound(Compound.Glucose);
        var atp = SimulationParameters.GetCompound(Compound.ATP);
        AssertThat(process.Process.Inputs).HasSize(1);
        AssertThat(process.Process.Inputs.ContainsKey(glucose)).IsTrue();
        var production = process.Process.Outputs[atp] *
            (process.Rate * cache.GetEnvironmentalTolerances(species, patch.Biome).ProcessSpeedModifier);
        AssertThat(production).IsGreater(0.0f);
        AssertNear(cache.GetCompoundGeneratedFrom(glucose, atp, species, patch.Biome), production);
        return production;
    }

    private static Species CreateStorageSpecies(bool multicellular, float capacity)
    {
        // Only this test owns the definition. No SimulationParameters object is mutated.
        var storage = new OrganelleDefinition
        {
            InternalName = "chunkTestStorage",
            Name = "Chunk test storage",
            Hexes = [new Hex(0, 0)],
            InitialComposition = new Dictionary<Compound, float>(),
            Components = new OrganelleDefinition.OrganelleComponentFactoryInfo
            {
                Storage = new StorageComponentFactory { Capacity = capacity },
            },
        };
        storage.Components.Check(storage.InternalName);
        storage.Resolve(SimulationParameters.Instance);
        var organelle = new OrganelleTemplate(storage, new Hex(4, 0), 0);
        if (multicellular)
        {
            var cellType = CreateCellType("Storage", "cytoplasm");
            cellType.ModifiableOrganelles.Add(organelle);
            AssertThat(cellType.MembraneType.CanEngulf).IsTrue();
            return CreateMulticellular(cellType);
        }

        var microbe = CreateMicrobe("cytoplasm");
        microbe.Organelles.Add(organelle);
        microbe.OnEdited();
        AssertThat(microbe.CanEngulf).IsTrue();
        return microbe;
    }

    private static float NominalStorage(Species species)
    {
        if (species is MicrobeSpecies microbe)
            return microbe.NominalStorageCapacity;

        return ((MulticellularSpecies)species).StorageCapacities.Nominal;
    }

    private static void AssertFeedingModesAreDistinguishable()
    {
        // With neutral behaviour, size can reveal engulfing only when leakage has a different score.
        AssertThat(Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER).IsGreater(0.0f);
        AssertThat(Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER)
            .OverrideFailureMessage("This fixture needs engulfing and leakage to have distinguishable scores")
            .IsNotEqual(1.0f);
    }

    private static float ScoreSaturated(ChunkCompoundPressure pressure, Species species, Patch patch)
    {
        var production = CreateCache().GetCompoundGeneratedFrom(SimulationParameters.GetCompound(Compound.Glucose),
            SimulationParameters.GetCompound(Compound.ATP), species, patch.Biome);
        AssertThat(production).IsGreater(0.0f);
        return ScoreWithConsumption(pressure, species, patch, production / 2);
    }

    private static float ScoreWithConsumption(ChunkCompoundPressure pressure, Species species,
        Patch patch, float consumption)
    {
        AssertThat(consumption).IsGreater(0.0f);
        var cache = CreateCache();

        // This cache belongs only to this call. Control Score's consumption input without changing shared
        // definitions or relying on a particular reaction yield, movement cost or osmoregulation balance.
        cache.GetEnergyBalanceForSpecies(species, patch.Biome).TotalConsumption = consumption;
        var score = pressure.Score(species, patch, cache);
        AssertThat(float.IsFinite(score)).IsTrue();
        return score;
    }

    private static void AssertProcessLayout(Species species, params string[] names)
    {
        var active = CreateCache().GetActiveProcessList(species);
        AssertThat(active).HasSize(names.Length);
        var remaining = new HashSet<string>(names);
        foreach (var process in active)
        {
            AssertThat(remaining.Remove(process.Process.InternalName)).IsTrue();
            AssertThat(process.Rate).IsGreater(0.0f);
            AssertThat(process.Process.Inputs.ContainsKey(SimulationParameters.GetCompound(Compound.ATP))).IsFalse();
        }
    }

    private static float ScoreFresh(ChunkCompoundPressure pressure, Species species, Patch patch)
    {
        var score = pressure.Score(species, patch, CreateCache());
        AssertThat(float.IsFinite(score)).IsTrue();
        return score;
    }

    private static void AssertLifecycle(ChunkCompoundPressure pressure, Species species, Patch patch)
    {
        var direct = CreateCache();
        var cold = pressure.Score(species, patch, direct);
        AssertThat(float.IsFinite(cold)).IsTrue();
        AssertThat(cold).IsGreater(0.0f);
        var warm = pressure.Score(species, patch, direct);
        SimulationCacheTestFixtures.AssertBits(warm, cold);
        direct.Clear();
        var cleared = pressure.Score(species, patch, direct);
        SimulationCacheTestFixtures.AssertBits(cleared, cold);

        var wrapped = CreateCache();
        var wrappedCold = wrapped.GetPressureScore(pressure, patch, species);
        SimulationCacheTestFixtures.AssertBits(wrappedCold, cold);
        var wrappedWarm = wrapped.GetPressureScore(pressure, patch, species);
        SimulationCacheTestFixtures.AssertBits(wrappedWarm, cold);
        wrapped.Clear();
        var wrappedCleared = wrapped.GetPressureScore(pressure, patch, species);
        SimulationCacheTestFixtures.AssertBits(wrappedCleared, cold);
    }

    private static void AssertNear(float actual, float expected)
    {
        AssertThat(float.IsFinite(actual)).IsTrue();
        AssertThat(MathF.Abs(actual - expected) <= MathF.Abs(expected) * RelativeTolerance)
            .OverrideFailureMessage($"Expected {expected:R}, got {actual:R} (relative tolerance {RelativeTolerance:R})")
            .IsTrue();
    }
}
