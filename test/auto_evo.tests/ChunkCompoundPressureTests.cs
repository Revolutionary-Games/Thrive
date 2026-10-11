using System;
using System.Collections.Generic;
using System.Globalization;
using AutoEvo;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

/// <summary>
///   Protects public chunk-food scoring with fixed physical inputs and independent reaction expectations.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class ChunkCompoundPressureTests
{
    private const string ChunkType = "chunkScoreTest";
    private const float RelativeTolerance = 1e-5f;

    [TestCase]
    public void CollectionEndpointsFollowKnownPhysicalAndReactionInputs()
    {
        // These two one-hex organelles have density 800 and 1100, and only cytoplasm stores 0.5.
        // Auto-evo estimates their mass as 2660 and their movement force as 990; neither can propel the cell.
        var baseCollection = 1 + MathF.Pow(990.0f / 2660.0f, 0.4f)
            + (MathF.Pow(1.5f, 0.8f) - 1) / 0.8f;
        var receptor = 0.4f + 0.00007f;
        var patch = CreatePatch();
        var pressure = CreatePressure();
        var endpoints = new (float Activity, float Focus, float Collection)[]
        {
            (0, 0, baseCollection * 0.1f),
            (0, 400, baseCollection * 0.1f),
            (400, 0, baseCollection + receptor),
            (400, 400, (baseCollection + receptor) * 1.9f + baseCollection * -0.9f * 0.1f),
        };

        foreach (var endpoint in endpoints)
        {
            var species = CreateReceptorMicrobe(Compound.Glucose);
            SetBehaviour(species, endpoint.Activity, endpoint.Focus);
            AssertSimpleInputs(species, patch, 1, 1, 4, 2.4f);
            AssertNear(CreateCache().GetSpeedForSpecies(species), 990.0f / 2660.0f);
            AssertThat(species.NominalStorageCapacity).IsEqual(0.5f);
            var score = ScoreFresh($"collection-{endpoint.Activity}-{endpoint.Focus}", pressure, species, patch);
            AssertNear(score, endpoint.Collection * 0.2f * (2.4f / 4));
        }

        var matching = CreateReceptorMicrobe(Compound.Glucose);
        var mismatching = CreateReceptorMicrobe(Compound.Ammonia);
        var matchingScore = ScoreFresh("receptor-matching", pressure, matching, patch);
        var mismatchingScore = ScoreFresh("receptor-mismatching", pressure, mismatching, patch);
        AssertNear(mismatchingScore, baseCollection * 0.2f * (2.4f / 4));
        AssertNear(matchingScore - mismatchingScore, receptor * 0.2f * (2.4f / 4));

        var zeroDensityPatch = CreatePatch(density: 0);
        var zeroDensityScore = ScoreFresh("receptor-zero-density", pressure, matching, zeroDensityPatch);
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
        aggressive.ModifiableBehaviour.Aggression = 400;
        opportunistic.ModifiableBehaviour.Opportunism = 400;
        fearful.ModifiableBehaviour.Fear = 400;
        AssertThat(CreateCache().GetBaseHexSizeForSpecies(baseline)).IsLess(patch.Biome.Chunks[ChunkType].Size);
        var score = ScoreFresh("leak-baseline", pressure, baseline, patch);
        AssertThat(score).IsGreater(0.0f);
        AssertNear(ScoreFresh("leak-aggression", pressure, aggressive, patch), score * 0.9f);
        SimulationCacheTestFixtures.AssertBits(ScoreFresh("leak-opportunism", pressure, opportunistic, patch), score);
        SimulationCacheTestFixtures.AssertBits(ScoreFresh("leak-fear", pressure, fearful, patch), score);
    }

    [TestCase]
    public void EngulfedFoodRewardsOpportunismButDoesNotPenalizeAggression()
    {
        var patch = CreatePatch(chunkSize: 0.5f);
        var pressure = CreatePressure();
        var baseline = CreateReceptorMicrobe(Compound.Glucose);
        var aggressive = CreateReceptorMicrobe(Compound.Glucose);
        var opportunistic = CreateReceptorMicrobe(Compound.Glucose);
        aggressive.ModifiableBehaviour.Aggression = 400;
        opportunistic.ModifiableBehaviour.Opportunism = 400;
        AssertThat(baseline.CanEngulf).IsTrue();
        AssertThat(CreateCache().GetBaseHexSizeForSpecies(baseline))
            .IsGreater(patch.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        var score = ScoreFresh("engulf-baseline", pressure, baseline, patch);
        AssertThat(score).IsGreater(0.0f);
        AssertNear(ScoreFresh("engulf-opportunism", pressure, opportunistic, patch), score * 1.4f);
        SimulationCacheTestFixtures.AssertBits(ScoreFresh("engulf-aggression", pressure, aggressive, patch), score);
    }

    [TestCase]
    public void NonEngulfingMembraneStillCollectsLeakedFood()
    {
        var patch = CreatePatch(chunkSize: 0.1f);
        var pressure = CreatePressure();
        var species = CreateMicrobe("cytoplasm");
        species.MembraneType = SimulationParameters.Instance.GetMembrane("cellulose");
        species.OnEdited();
        AssertThat(species.CanEngulf).IsFalse();
        AssertThat(CreateCache().GetBaseHexSizeForSpecies(species))
            .IsGreater(patch.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        var aggressive = (MicrobeSpecies)species.Clone();
        aggressive.ModifiableBehaviour.Aggression = 400;
        var opportunistic = (MicrobeSpecies)species.Clone();
        opportunistic.ModifiableBehaviour.Opportunism = 400;
        var score = ScoreFresh("membrane-leak-baseline", pressure, species, patch);
        AssertThat(score).IsGreater(0.0f);
        AssertNear(ScoreFresh("membrane-leak-aggression", pressure, aggressive, patch), score * 0.9f);
        SimulationCacheTestFixtures.AssertBits(ScoreFresh("membrane-leak-opportunism", pressure, opportunistic, patch),
            score);
    }

    [TestCase]
    public void UnusedCellTypeCannotGrantEngulfingCapability()
    {
        var patch = CreatePatch(chunkSize: 1);
        var pressure = CreatePressure();
        var small = CreateCellType("Small", "cytoplasm");
        var species = CreateMulticellular(small);
        var score = ScoreFresh("multicellular-unused-before", pressure, species, patch);
        var large = CreateCellType("UnusedLarge", "cytoplasm", "cytoplasm", "cytoplasm");
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
        var patch = CreatePatch(chunkSize: 1);
        var pressure = CreatePressure();
        var large = CreateCellType("PresentLarge", "cytoplasm", "cytoplasm", "cytoplasm");
        var species = CreateMulticellular(large);
        var aggressive = (MulticellularSpecies)species.Clone();
        aggressive.ModifiableBehaviour.Aggression = 400;
        var opportunistic = (MulticellularSpecies)species.Clone();
        opportunistic.ModifiableBehaviour.Opportunism = 400;
        AssertThat(CreateCache().GetBaseHexSizeForCellType(large))
            .IsGreater(patch.Biome.Chunks[ChunkType].Size * Constants.ENGULF_SIZE_RATIO_REQ);
        var score = ScoreFresh("multicellular-present-baseline", pressure, species, patch);
        AssertThat(score).IsGreater(0.0f);
        SimulationCacheTestFixtures.AssertBits(
            ScoreFresh("multicellular-present-aggression", pressure, aggressive, patch), score);
        AssertNear(ScoreFresh("multicellular-present-opportunism", pressure, opportunistic, patch), score * 1.4f);
    }

    [TestCase(170.0f, 119.0f)]
    [TestCase(300.0f, 264.7059f)]
    [TestCase(340.0f, 340.0f)]
    [TestCase(400.0f, 400.0f)]
    public void DayNightActivityFollowsFloorAndNightThreshold(float activity, float effectiveActivity)
    {
        var patch = CreatePatch(varyingSunlight: true);
        var species = CreateMicrobe("cytoplasm", "chromatophore");
        var counterpart = (MicrobeSpecies)species.Clone();
        SetBehaviour(species, activity, 120);
        SetBehaviour(counterpart, effectiveActivity, 120);
        AssertThat(CreateCache().GetUsesVaryingCompoundsForSpecies(species, patch.Biome)).IsTrue();
        var expected = ScoreFresh($"night-counterpart-{activity}", CreatePressure(), counterpart, patch);
        var score = ScoreFresh($"night-varying-{activity}", CreatePressure(dayNight: true), species, patch);
        AssertThat(expected).IsGreater(0.0f);
        AssertNear(score, expected);
    }

    [TestCase]
    public void DayNightPenaltyRequiresEnabledCycleAndVaryingInput()
    {
        var species = CreateMicrobe("cytoplasm", "chromatophore");
        SetBehaviour(species, 170, 120);
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
        AssertProcessLayout(species, "glycolysis_cytoplasm");
        var pressure = CreatePressure();
        var saturatedPatch = CreatePatch();
        var underPatch = CreatePatch(temperature: 25);
        AssertSimpleInputs(species, saturatedPatch, 1, 1, 2, 2.4f);
        AssertSimpleInputs(species, underPatch, 0.8f, 1.2f, 2.2f, 2.4f * 0.8f);
        var saturated = ScoreFresh("atp-direct-saturated", pressure, species, saturatedPatch);
        var under = ScoreFresh("atp-direct-under", pressure, species, underPatch);
        AssertThat(saturated).IsGreater(0.0f);
        AssertNear(under, saturated * ((2.4f * 0.8f) / 2.2f));
        AssertThat(under).IsLess(saturated);
        var zeroPatch = CreatePatch(compoundAmount: 0, glucoseAmount: 0);
        AssertThat(CreateCache().GetEnergyBalanceForSpecies(species, zeroPatch.Biome).TotalConsumption)
            .IsGreater(0.0f);
        AssertThat(ScoreFresh("atp-direct-zero", pressure, species, zeroPatch)).IsEqual(0.0f);
    }

    [TestCase]
    public void IntermediateAtpSupportUsesBothGlycolysisEfficiencies()
    {
        var species = CreateMicrobe("cytoplasm", "chemoSynthesizingProteins");
        AssertProcessLayout(species, "glycolysis_cytoplasm", "glycolysis", "bacterial_ChemoSynthesis");
        var saturatedPatch = CreatePatch(Compound.Hydrogensulfide);
        var underPatch = CreatePatch(Compound.Hydrogensulfide, carbonDioxide: 0.001f);
        var pressure = CreatePressure(Compound.Hydrogensulfide, Compound.Glucose);
        var efficiency = (2.0f + 2.4f) / (0.006f + 0.007f);
        var glucoseGenerated = 0.04f * (0.001f / 0.09f);
        AssertSimpleInputs(species, saturatedPatch, 1, 1, 4);
        AssertSimpleInputs(species, underPatch, 1, 1, 4);
        AssertThat(0.04f * efficiency / 4).IsGreater(1.0f);
        AssertThat(glucoseGenerated * efficiency / 4).IsGreater(0.0f);
        AssertThat(glucoseGenerated * efficiency / 4).IsLess(1.0f);
        var saturated = ScoreFresh("atp-intermediate-saturated", pressure, species, saturatedPatch);
        var under = ScoreFresh("atp-intermediate-under", pressure, species, underPatch);
        AssertThat(saturated).IsGreater(0.0f);
        AssertNear(under, saturated * (efficiency * glucoseGenerated / 4));
        AssertThat(under).IsLess(saturated);
        var zeroPatch = CreatePatch(Compound.Hydrogensulfide, compoundAmount: 0, hydrogenSulfideAmount: 0);
        AssertThat(ScoreFresh("atp-intermediate-zero", pressure, species, zeroPatch)).IsEqual(0.0f);
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
            patch = CreatePatch(Compound.Hydrogensulfide, 1, carbonDioxide: 0.001f);
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
        AssertSimpleInputs(species, patch, 1, 1, 4);
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
    ///   Verifies the natural boundary where food ATP exactly covers the cell's consumption.
    /// </summary>
    [TestCase]
    public void DirectAtpSupportAtEqualConsumptionMatchesSaturation()
    {
        var species = CreateMicrobe("cytoplasm");
        AssertProcessLayout(species, "glycolysis_cytoplasm");
        var patch = CreatePatch(temperature: 19.705881f);

        // For this one-hex cell, production is 2.4*T and consumption is 3-T: equality gives T=3/3.4.
        var independentModifier = 3.0f / 3.4f;
        AssertSimpleInputs(species, patch, independentModifier, 2 - independentModifier,
            3 - independentModifier, 2.4f * independentModifier);
        var diagnostic = CreateCache();
        var production = diagnostic.GetCompoundGeneratedFrom(SimulationParameters.GetCompound(Compound.Glucose),
            SimulationParameters.GetCompound(Compound.ATP), species, patch.Biome);
        var consumption = diagnostic.GetEnergyBalanceForSpecies(species, patch.Biome).TotalConsumption;
        GD.Print($"COD153_ATP_BOUNDARY|production={production.ToString("R", CultureInfo.InvariantCulture)}|" +
            $"productionBits=0x{BitConverter.SingleToInt32Bits(production):X8}|" +
            $"consumption={consumption.ToString("R", CultureInfo.InvariantCulture)}|" +
            $"consumptionBits=0x{BitConverter.SingleToInt32Bits(consumption):X8}");
        SimulationCacheTestFixtures.AssertBits(production, consumption);
        AssertThat(production / consumption).IsEqual(1.0f);

        var pressure = CreatePressure();
        var saturated = ScoreFresh("atp-equality-saturated", pressure, species, CreatePatch());
        var atBoundary = ScoreFresh("atp-direct-equal-consumption", pressure, species, patch);
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
        SetBehaviour(species, 400, 0);
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

    private static void AssertSimpleInputs(Species species, Patch patch, float processModifier,
        float osmoregulationModifier, float consumption, float? directAtp = null)
    {
        var diagnostic = CreateCache();
        var tolerances = diagnostic.GetEnvironmentalTolerances(species, patch.Biome);
        AssertNear(tolerances.ProcessSpeedModifier, processModifier);
        AssertNear(tolerances.OsmoregulationModifier, osmoregulationModifier);
        AssertNear(diagnostic.GetEnergyBalanceForSpecies(species, patch.Biome).TotalConsumption, consumption);
        if (species is MicrobeSpecies microbe)
        {
            AssertThat(microbe.CellTypeSpecializationBonus).IsEqual(1.0f);
        }
        else if (species is MulticellularSpecies multicellular)
        {
            AssertThat(multicellular.CellTypes[0].CellTypeSpecializationBonus).IsEqual(1.0f);
        }

        if (directAtp != null)
        {
            var actual = diagnostic.GetCompoundGeneratedFrom(SimulationParameters.GetCompound(Compound.Glucose),
                SimulationParameters.GetCompound(Compound.ATP), species, patch.Biome);
            AssertNear(actual, directAtp.Value);
        }
    }

    private static void AssertProcessLayout(Species species, params string[] names)
    {
        var active = CreateCache().GetActiveProcessList(species);
        AssertThat(active).HasSize(names.Length);
        var remaining = new HashSet<string>(names);
        foreach (var process in active)
        {
            AssertThat(remaining.Remove(process.Process.InternalName)).IsTrue();
            AssertThat(process.Rate).IsEqual(1.0f);
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
        AssertThat(MathF.Abs(actual - expected) <= MathF.Abs(expected) * RelativeTolerance).IsTrue();
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
