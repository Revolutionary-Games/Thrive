using System;
using System.Collections.Generic;
using System.Globalization;
using AutoEvo;
using GdUnit4;
using Godot;
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

    [TestCase]
    public void CollectionEndpointsUseCurrentPhysicalInputs()
    {
        var patch = CreatePatch();
        var pressure = CreatePressure();
        var matching = CreateReceptorMicrobe(Compound.Glucose);
        var diagnostic = CreateCache();
        var baseCollection = 1 + MathF.Pow(diagnostic.GetSpeedForSpecies(matching), 0.4f)
            + StorageContribution(matching.NominalStorageCapacity);

        // The fixture has unit chunk density and amount, so only the configured receptor coefficients remain.
        var receptor = Constants.AUTO_EVO_CHEMORECEPTOR_BASE_SCORE
            + Constants.AUTO_EVO_CHEMORECEPTOR_VARIABLE_CHUNK_SCORE;
        var passive = Constants.AUTO_EVO_PASSIVE_COMPOUND_COLLECTION_FRACTION;
        var focus = 1 + MathF.Pow(Constants.MAX_SPECIES_FOCUS / Constants.MAX_SPECIES_ACTIVITY, 0.4f)
            * Constants.AUTO_EVO_MAX_FOCUS_CHUNK_BONUS;
        var endpoints = new (float Activity, float Focus, float Collection)[]
        {
            (0, 0, baseCollection * passive),
            (0, Constants.MAX_SPECIES_FOCUS, baseCollection * passive),
            (Constants.MAX_SPECIES_ACTIVITY, 0, baseCollection + receptor),
            (Constants.MAX_SPECIES_ACTIVITY, Constants.MAX_SPECIES_FOCUS,
                (baseCollection + receptor) * focus + baseCollection * (1 - focus) * passive),
        };

        AssertThat(diagnostic.GetBaseHexSizeForSpecies(matching))
            .IsLess(patch.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        foreach (var endpoint in endpoints)
        {
            var species = CreateReceptorMicrobe(Compound.Glucose);
            SetBehaviour(species, endpoint.Activity, endpoint.Focus);
            var score = ScoreSaturated("collection", pressure, species, patch);
            AssertNear(score, endpoint.Collection * Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER);
        }

        var mismatching = CreateReceptorMicrobe(Compound.Ammonia);
        var matchingScore = ScoreSaturated("receptor-matching", pressure, matching, patch);
        var mismatchingScore = ScoreSaturated("receptor-mismatching", pressure, mismatching, patch);
        AssertNear(mismatchingScore, baseCollection * Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER);
        AssertNear(matchingScore - mismatchingScore, receptor * Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER);

        var zeroDensityPatch = CreatePatch(density: 0);
        var zeroDensityScore = ScoreSaturated("receptor-zero-density", pressure, matching, zeroDensityPatch);
        AssertNear(zeroDensityScore, mismatchingScore);
        AssertThat(zeroDensityScore).IsGreater(0.0f);
    }

    [TestCase]
    public void LeakingFoodPenalizesAggressionButDoesNotRewardOpportunism()
    {
        var patch = CreatePatch();
        var pressure = CreatePressure();
        var baseline = CreateReceptorMicrobe(Compound.Glucose);
        var aggressive = CreateReceptorMicrobe(Compound.Glucose);
        var opportunistic = CreateReceptorMicrobe(Compound.Glucose);
        var fearful = CreateReceptorMicrobe(Compound.Glucose);
        aggressive.ModifiableBehaviour.Aggression = Constants.MAX_SPECIES_AGGRESSION;
        opportunistic.ModifiableBehaviour.Opportunism = Constants.MAX_SPECIES_OPPORTUNISM;
        fearful.ModifiableBehaviour.Fear = Constants.MAX_SPECIES_FEAR;
        AssertThat(CreateCache().GetBaseHexSizeForSpecies(baseline))
            .IsLess(patch.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        var score = ScoreFresh("leak-baseline", pressure, baseline, patch);
        AssertThat(score).IsGreater(0.0f);
        AssertNear(ScoreFresh("leak-aggression", pressure, aggressive, patch),
            score * (1 - Constants.AUTO_EVO_MAX_AGGRESSION_GATHERING_PENALTY));
        SimulationCacheTestFixtures.AssertBits(ScoreFresh("leak-opportunism", pressure, opportunistic, patch), score);
        AssertNear(ScoreFresh("leak-fear", pressure, fearful, patch),
            score * (1 - Constants.AUTO_EVO_MAX_FEAR_GATHERING_PENALTY));
    }

    [TestCase]
    public void EngulfedFoodRewardsOpportunismButDoesNotPenalizeAggression()
    {
        var pressure = CreatePressure();
        var baseline = CreateReceptorMicrobe(Compound.Glucose);
        var patch = CreatePatch(chunkSize:
            CreateCache().GetBaseHexSizeForSpecies(baseline) / (2 * Constants.ENGULF_SIZE_RATIO_REQ));
        var aggressive = CreateReceptorMicrobe(Compound.Glucose);
        var opportunistic = CreateReceptorMicrobe(Compound.Glucose);
        aggressive.ModifiableBehaviour.Aggression = Constants.MAX_SPECIES_AGGRESSION;
        opportunistic.ModifiableBehaviour.Opportunism = Constants.MAX_SPECIES_OPPORTUNISM;
        AssertThat(baseline.CanEngulf).IsTrue();
        AssertThat(CreateCache().GetBaseHexSizeForSpecies(baseline))
            .IsGreater(patch.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        var score = ScoreFresh("engulf-baseline", pressure, baseline, patch);
        AssertThat(score).IsGreater(0.0f);
        AssertNear(ScoreFresh("engulf-opportunism", pressure, opportunistic, patch),
            score * (1 + MathF.Sqrt(Constants.MAX_SPECIES_OPPORTUNISM / Constants.MAX_SPECIES_ACTIVITY)
                * Constants.AUTO_EVO_MAX_OPPORTUNISM_BONUS));
        SimulationCacheTestFixtures.AssertBits(ScoreFresh("engulf-aggression", pressure, aggressive, patch), score);
    }

    [TestCase]
    public void NonEngulfingMembraneStillCollectsLeakedFood()
    {
        var pressure = CreatePressure();
        var species = CreateMicrobe("cytoplasm");
        species.MembraneType = SimulationParameters.Instance.GetMembrane("cellulose");
        species.OnEdited();
        var patch = CreatePatch(chunkSize:
            CreateCache().GetBaseHexSizeForSpecies(species) / (2 * Constants.ENGULF_SIZE_RATIO_REQ));
        AssertThat(species.CanEngulf).IsFalse();
        AssertThat(CreateCache().GetBaseHexSizeForSpecies(species))
            .IsGreater(patch.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        var aggressive = (MicrobeSpecies)species.Clone();
        aggressive.ModifiableBehaviour.Aggression = Constants.MAX_SPECIES_AGGRESSION;
        var opportunistic = (MicrobeSpecies)species.Clone();
        opportunistic.ModifiableBehaviour.Opportunism = Constants.MAX_SPECIES_OPPORTUNISM;
        var score = ScoreFresh("membrane-leak-baseline", pressure, species, patch);
        AssertThat(score).IsGreater(0.0f);
        AssertNear(ScoreFresh("membrane-leak-aggression", pressure, aggressive, patch),
            score * (1 - Constants.AUTO_EVO_MAX_AGGRESSION_GATHERING_PENALTY));
        SimulationCacheTestFixtures.AssertBits(ScoreFresh("membrane-leak-opportunism", pressure, opportunistic, patch),
            score);
    }

    [TestCase]
    public void UnusedCellTypeCannotGrantEngulfingCapability()
    {
        var pressure = CreatePressure();
        var small = CreateCellType("Small", "cytoplasm");
        var large = CreateCellType("UnusedLarge", "cytoplasm", "cytoplasm", "cytoplasm");
        var diagnostic = CreateCache();
        var smallSize = diagnostic.GetBaseHexSizeForCellType(small);
        var largeSize = diagnostic.GetBaseHexSizeForCellType(large);
        var patch = CreatePatch(chunkSize: (smallSize + largeSize) / (2 * Constants.ENGULF_SIZE_RATIO_REQ));
        AssertThat(smallSize).IsLess(patch.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        var species = CreateMulticellular(small);
        var score = ScoreFresh("multicellular-unused-before", pressure, species, patch);
        species.ModifiableCellTypes.Add(large);
        species.OnEdited();
        AssertThat(CreateCache().GetBaseHexSizeForCellType(large))
            .IsGreater(patch.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        AssertThat(species.EditorCells.Count).IsEqual(1);
        SimulationCacheTestFixtures.AssertBits(ScoreFresh("multicellular-unused-after", pressure, species, patch),
            score);
    }

    [TestCase]
    public void PresentCellTypeGrantsEngulfingCapability()
    {
        var pressure = CreatePressure();
        var large = CreateCellType("PresentLarge", "cytoplasm", "cytoplasm", "cytoplasm");
        var patch = CreatePatch(chunkSize:
            CreateCache().GetBaseHexSizeForCellType(large) / (2 * Constants.ENGULF_SIZE_RATIO_REQ));
        var species = CreateMulticellular(large);
        var aggressive = (MulticellularSpecies)species.Clone();
        aggressive.ModifiableBehaviour.Aggression = Constants.MAX_SPECIES_AGGRESSION;
        var opportunistic = (MulticellularSpecies)species.Clone();
        opportunistic.ModifiableBehaviour.Opportunism = Constants.MAX_SPECIES_OPPORTUNISM;
        AssertThat(CreateCache().GetBaseHexSizeForCellType(large))
            .IsGreater(patch.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        var score = ScoreFresh("multicellular-present-baseline", pressure, species, patch);
        AssertThat(score).IsGreater(0.0f);
        SimulationCacheTestFixtures.AssertBits(
            ScoreFresh("multicellular-present-aggression", pressure, aggressive, patch), score);
        AssertNear(ScoreFresh("multicellular-present-opportunism", pressure, opportunistic, patch),
            score * (1 + MathF.Sqrt(Constants.MAX_SPECIES_OPPORTUNISM / Constants.MAX_SPECIES_ACTIVITY)
                * Constants.AUTO_EVO_MAX_OPPORTUNISM_BONUS));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void DayNightActivityFollowsFloorAndNightThreshold(int scenario)
    {
        var threshold = Constants.AI_ACTIVITY_TO_BE_FULLY_ACTIVE_DURING_NIGHT;
        var floor = Constants.AUTO_EVO_MAX_NIGHT_SESSILITY_COLLECTING_PENALTY;
        AssertThat(floor).IsGreater(0.0f);
        AssertThat(floor).IsLess(1.0f);
        AssertThat(threshold).IsLessEqual(Constants.MAX_SPECIES_ACTIVITY);
        var belowFloor = threshold * floor / 2;
        var betweenFloorAndThreshold = threshold * (1 + floor) / 2;
        var cases = new (float Activity, float EffectiveActivity)[]
        {
            (belowFloor, belowFloor * floor),
            (betweenFloorAndThreshold, betweenFloorAndThreshold * ((1 + floor) / 2)),
            (threshold, threshold),
            (Constants.MAX_SPECIES_ACTIVITY, Constants.MAX_SPECIES_ACTIVITY),
        };
        var (activity, effectiveActivity) = cases[scenario];
        var patch = CreatePatch(varyingSunlight: true);
        var species = CreateMicrobe("cytoplasm", "chromatophore");
        var counterpart = (MicrobeSpecies)species.Clone();
        SetBehaviour(species, activity, Constants.MAX_SPECIES_FOCUS * 0.3f);
        SetBehaviour(counterpart, effectiveActivity, Constants.MAX_SPECIES_FOCUS * 0.3f);
        AssertThat(CreateCache().GetUsesVaryingCompoundsForSpecies(species, patch.Biome)).IsTrue();
        var expected = ScoreFresh("night-counterpart", CreatePressure(), counterpart, patch);
        var score = ScoreFresh("night-varying", CreatePressure(dayNight: true), species, patch);
        AssertThat(expected).IsGreater(0.0f);
        AssertNear(score, expected);
    }

    [TestCase]
    public void DayNightPenaltyRequiresEnabledCycleAndVaryingInput()
    {
        var species = CreateMicrobe("cytoplasm", "chromatophore");
        SetBehaviour(species, Constants.AI_ACTIVITY_TO_BE_FULLY_ACTIVE_DURING_NIGHT / 2,
            Constants.MAX_SPECIES_FOCUS * 0.3f);
        var varyingPatch = CreatePatch(varyingSunlight: true);
        var steadyPatch = CreatePatch();
        AssertThat(CreateCache().GetUsesVaryingCompoundsForSpecies(species, varyingPatch.Biome)).IsTrue();
        AssertThat(CreateCache().GetUsesVaryingCompoundsForSpecies(species, steadyPatch.Biome)).IsFalse();
        var disabledVarying = ScoreFresh("night-disabled-varying", CreatePressure(), species, varyingPatch);
        var disabledSteady = ScoreFresh("night-disabled-steady", CreatePressure(), species, steadyPatch);
        var enabledSteady = ScoreFresh("night-enabled-steady", CreatePressure(dayNight: true), species, steadyPatch);
        SimulationCacheTestFixtures.AssertBits(disabledVarying, disabledSteady);
        SimulationCacheTestFixtures.AssertBits(enabledSteady, disabledSteady);
    }

    [TestCase]
    public void DirectAtpSupportUsesProductionAndCapsAboveConsumption()
    {
        var species = CreateMicrobe("cytoplasm");
        var patch = CreatePatch();
        var production = ExpectedDirectAtp(species, patch);
        var pressure = CreatePressure();
        var saturated = ScoreWithConsumption("atp-direct-saturated", pressure, species, patch, production / 2);
        var under = ScoreWithConsumption("atp-direct-under", pressure, species, patch, production * 2);
        AssertThat(saturated).IsGreater(0.0f);
        AssertNear(under, saturated / 2);
        AssertThat(under).IsLess(saturated);

        var zeroPatch = CreatePatch(compoundAmount: 0, glucoseAmount: 0);
        AssertThat(CreateCache().GetCompoundGeneratedFrom(SimulationParameters.GetCompound(Compound.Glucose),
            SimulationParameters.GetCompound(Compound.ATP), species, zeroPatch.Biome)).IsEqual(0.0f);
        AssertThat(ScoreWithConsumption("atp-direct-zero", pressure, species, zeroPatch, production)).IsEqual(0.0f);
    }

    [TestCase]
    public void IntermediateAtpSupportUsesBothGlycolysisEfficiencies()
    {
        var species = CreateMicrobe("cytoplasm", "chemoSynthesizingProteins");
        AssertProcessLayout(species, "glycolysis_cytoplasm", "glycolysis", "bacterial_ChemoSynthesis");
        var parameters = SimulationParameters.Instance;
        var glucose = SimulationParameters.GetCompound(Compound.Glucose);
        var atp = SimulationParameters.GetCompound(Compound.ATP);
        var carbonDioxide = SimulationParameters.GetCompound(Compound.Carbondioxide);
        var cytoplasm = parameters.GetBioProcess("glycolysis_cytoplasm");
        var glycolysis = parameters.GetBioProcess("glycolysis");
        var synthesis = parameters.GetBioProcess("bacterial_ChemoSynthesis");
        var patch = CreatePatch(Compound.Hydrogensulfide, carbonDioxide: synthesis.Inputs[carbonDioxide]);
        var diagnostic = CreateCache();
        var processes = diagnostic.GetActiveProcessList(species);
        var synthesisRate = processes.Find(p => ReferenceEquals(p.Process, synthesis)).Rate;
        AssertThat(synthesisRate).IsGreater(0.0f);

        // At the configured CO2 requirement the environmental rate is one. The two glucose-to-ATP
        // recipes contribute their quantities to conversion efficiency, independently of process capacity.
        var generatedGlucose = synthesis.Outputs[glucose] *
            (synthesisRate * diagnostic.GetEnvironmentalTolerances(species, patch.Biome).ProcessSpeedModifier);
        AssertNear(diagnostic.GetCompoundGeneratedFrom(SimulationParameters.GetCompound(Compound.Hydrogensulfide),
            glucose, species, patch.Biome), generatedGlucose);
        var efficiency = (cytoplasm.Outputs[atp] + glycolysis.Outputs[atp]) /
            (cytoplasm.Inputs[glucose] + glycolysis.Inputs[glucose]);
        var production = efficiency * generatedGlucose;
        AssertThat(production).IsGreater(0.0f);
        var pressure = CreatePressure(Compound.Hydrogensulfide, Compound.Glucose);
        var saturated = ScoreWithConsumption("atp-intermediate-saturated", pressure, species, patch, production / 2);
        var under = ScoreWithConsumption("atp-intermediate-under", pressure, species, patch, production * 2);
        AssertThat(saturated).IsGreater(0.0f);
        AssertNear(under, saturated / 2);
        AssertThat(under).IsLess(saturated);

        var zeroPatch = CreatePatch(Compound.Hydrogensulfide, compoundAmount: 0, hydrogenSulfideAmount: 0);
        AssertThat(ScoreWithConsumption("atp-intermediate-zero", pressure, species, zeroPatch, production))
            .IsEqual(0.0f);
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
        SimulationCacheTestFixtures.AssertBits(
            diagnostic.GetEnergyBalanceForSpecies(smaller, CreatePatch().Biome).TotalConsumption,
            diagnostic.GetEnergyBalanceForSpecies(larger, CreatePatch().Biome).TotalConsumption);

        var patch = CreatePatch(chunkSize:
            diagnostic.GetBaseHexSizeForSpecies(smaller) / (2 * Constants.ENGULF_SIZE_RATIO_REQ));
        var pressure = CreatePressure();
        var smallerScore = ScoreSaturated("storage-smaller", pressure, smaller, patch);
        var largerScore = ScoreSaturated("storage-larger", pressure, larger, patch);
        AssertThat(largerScore).IsGreater(smallerScore);
        AssertNear(largerScore - smallerScore,
            StorageContribution(largerStorage) - StorageContribution(smallerStorage));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CompleteScoreSurvivesDirectAndPressureCacheLifetimes(bool multicellular)
    {
        Species species;
        Patch patch;
        ChunkCompoundPressure pressure;
        string label;
        if (multicellular)
        {
            species = CreateMulticellular(CreateCellType("Intermediate", "cytoplasm", "chemoSynthesizingProteins"));
            patch = CreatePatch(Compound.Hydrogensulfide,
                CreateCache().GetBaseHexSizeForSpecies(species) / (2 * Constants.ENGULF_SIZE_RATIO_REQ),
                carbonDioxide: 0.001f);
            pressure = CreatePressure(Compound.Hydrogensulfide, Compound.Glucose);
            label = "complete-multicellular-intermediate-engulf";
        }
        else
        {
            species = CreateReceptorMicrobe(Compound.Glucose);
            patch = CreatePatch();
            pressure = CreatePressure();
            label = "complete-microbe-direct-leak";
        }

        SetBehaviour(species, 237, 141);
        species.ModifiableBehaviour.Aggression = 85;
        species.ModifiableBehaviour.Opportunism = 219;
        species.ModifiableBehaviour.Fear = 75;
        AssertLifecycle(label, pressure, species, patch);
    }

    [TestCase]
    public void MissingChunkIsReportedBeforeSpeciesAndCacheAccess()
    {
        var patch = CreatePatch();
        patch.Biome.Chunks.Clear();
        var unsupported = new MacroscopicSpecies(1900, "Chunk", "Unsupported");
        AssertArgumentException(() => CreatePressure().Score(unsupported, patch, null!),
            "Chunk does not exist in patch");
    }

    [TestCase]
    public void UnsupportedSpeciesIsReportedBeforeCacheAccess()
    {
        var unsupported = new MacroscopicSpecies(1900, "Chunk", "Unsupported");
        var patch = CreatePatch();
        AssertArgumentException(() => CreatePressure().Score(unsupported, patch, null!),
            "Wrong type of Species passed to Microbe/Multicellular Species miche tree");
    }

    [TestCase]
    public void ZeroActivityStillChecksMatchingChunkReceptor()
    {
        var species = CreateReceptorMicrobe(Compound.Glucose);
        SetBehaviour(species, 0, 0);
        var patch = CreatePatch();
        var pressure = CreatePressure();
        AssertThat(ScoreFresh("zero-activity-valid-compound", pressure, species, patch)).IsGreater(0.0f);
        var chunk = patch.Biome.Chunks[ChunkType];
        chunk.Compounds = new Dictionary<Compound, ChunkConfiguration.ChunkCompound>();
        patch.Biome.Chunks[ChunkType] = chunk;
        AssertArgumentException(() => pressure.Score(species, patch, CreateCache()),
            "Chunk does not contain compound");
    }

    /// <summary>
    ///   Exercises exact ATP equality through a test-owned cache input rather than a tuned biome temperature.
    /// </summary>
    [TestCase]
    public void DirectAtpSupportAtEqualConsumptionMatchesSaturation()
    {
        var species = CreateMicrobe("cytoplasm");
        var patch = CreatePatch();
        var production = ExpectedDirectAtp(species, patch);
        var diagnostic = CreateCache();
        diagnostic.GetEnergyBalanceForSpecies(species, patch.Biome).TotalConsumption = production;
        var actualProduction = diagnostic.GetCompoundGeneratedFrom(SimulationParameters.GetCompound(Compound.Glucose),
            SimulationParameters.GetCompound(Compound.ATP), species, patch.Biome);
        var consumption = diagnostic.GetEnergyBalanceForSpecies(species, patch.Biome).TotalConsumption;
        SimulationCacheTestFixtures.AssertBits(actualProduction, consumption);
        AssertThat(actualProduction / consumption).IsEqual(1.0f);

        var pressure = CreatePressure();
        var saturated = ScoreWithConsumption("atp-equality-saturated", pressure, species, patch, production / 2);
        var atBoundary = pressure.Score(species, patch, diagnostic);
        SimulationCacheTestFixtures.AssertBits(atBoundary, saturated);
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
        float compoundAmount = 1, float temperature = 15, float carbonDioxide = 0.09f,
        float glucoseAmount = 1, float hydrogenSulfideAmount = 1, bool varyingSunlight = false)
    {
        var template = SimulationParameters.Instance.GetBiome("aavolcanic_vent");
        var biome = (BiomeConditions)template.Conditions.Clone();
        biome.Pressure = 101325;
        SetCompound(biome, Compound.Temperature, new BiomeCompoundProperties { Ambient = temperature });
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
            [ChunkType] = new ChunkConfiguration
            {
                Name = "ChunkScoreTest",
                Meshes = [],
                Size = chunkSize,
                Density = density,
                Compounds = new Dictionary<Compound, ChunkConfiguration.ChunkCompound>
                {
                    [input] = new ChunkConfiguration.ChunkCompound { Amount = compoundAmount },
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

    private static float StorageContribution(float capacity)
    {
        return (MathF.Pow(capacity + 1, 0.8f) - 1) / 0.8f;
    }

    private static float ScoreSaturated(string label, ChunkCompoundPressure pressure, Species species, Patch patch)
    {
        var production = CreateCache().GetCompoundGeneratedFrom(SimulationParameters.GetCompound(Compound.Glucose),
            SimulationParameters.GetCompound(Compound.ATP), species, patch.Biome);
        AssertThat(production).IsGreater(0.0f);
        return ScoreWithConsumption(label, pressure, species, patch, production / 2);
    }

    private static float ScoreWithConsumption(string label, ChunkCompoundPressure pressure, Species species,
        Patch patch, float consumption)
    {
        AssertThat(consumption).IsGreater(0.0f);
        var cache = CreateCache();

        // This cache belongs only to this call. Control Score's consumption input without changing shared
        // definitions or relying on a particular reaction yield, movement cost or osmoregulation balance.
        cache.GetEnergyBalanceForSpecies(species, patch.Biome).TotalConsumption = consumption;
        var score = pressure.Score(species, patch, cache);
        PrintScore(label, score);
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

        AssertThat(remaining).IsEmpty();
    }

    private static float ScoreFresh(string label, ChunkCompoundPressure pressure, Species species, Patch patch)
    {
        var score = pressure.Score(species, patch, CreateCache());
        PrintScore(label, score);
        return score;
    }

    private static void AssertLifecycle(string label, ChunkCompoundPressure pressure, Species species, Patch patch)
    {
        var direct = CreateCache();
        var cold = pressure.Score(species, patch, direct);
        AssertThat(float.IsFinite(cold)).IsTrue();
        AssertThat(cold).IsGreater(0.0f);
        PrintScore(label + "/direct/cold", cold);
        var warm = pressure.Score(species, patch, direct);
        PrintScore(label + "/direct/warm", warm);
        SimulationCacheTestFixtures.AssertBits(warm, cold);
        direct.Clear();
        var cleared = pressure.Score(species, patch, direct);
        PrintScore(label + "/direct/clear", cleared);
        SimulationCacheTestFixtures.AssertBits(cleared, cold);

        var wrapped = CreateCache();
        var wrappedCold = wrapped.GetPressureScore(pressure, patch, species);
        PrintScore(label + "/wrapped/cold", wrappedCold);
        SimulationCacheTestFixtures.AssertBits(wrappedCold, cold);
        var wrappedWarm = wrapped.GetPressureScore(pressure, patch, species);
        PrintScore(label + "/wrapped/warm", wrappedWarm);
        SimulationCacheTestFixtures.AssertBits(wrappedWarm, cold);
        wrapped.Clear();
        var wrappedCleared = wrapped.GetPressureScore(pressure, patch, species);
        PrintScore(label + "/wrapped/clear", wrappedCleared);
        SimulationCacheTestFixtures.AssertBits(wrappedCleared, cold);
    }

    private static void AssertNear(float actual, float expected)
    {
        AssertThat(float.IsFinite(actual)).IsTrue();
        AssertThat(MathF.Abs(actual - expected) <= MathF.Abs(expected) * RelativeTolerance)
            .OverrideFailureMessage($"Expected {expected:R}, got {actual:R} (relative tolerance {RelativeTolerance:R})")
            .IsTrue();
    }

    private static void AssertArgumentException(Action action, string message)
    {
        try
        {
            action();
        }
        catch (ArgumentException exception)
        {
            AssertThat(exception.Message).IsEqual(message);
            return;
        }

        throw new InvalidOperationException("Expected ArgumentException: " + message);
    }

    private static void PrintScore(string label, float score)
    {
        GD.Print($"COD153_SCORE|{label}|{score.ToString("R", CultureInfo.InvariantCulture)}|" +
            $"0x{BitConverter.SingleToInt32Bits(score):X8}");
    }
}
