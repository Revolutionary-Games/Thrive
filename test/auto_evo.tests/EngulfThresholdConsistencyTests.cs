using System;
using System.Collections.Generic;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   Tests that auto-evo engulf checks consistently include the exact size threshold.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class EngulfThresholdConsistencyTests
{
    private enum ThresholdSpeciesKind
    {
        Microbe,
        Multicellular,
    }

    [TestCase]
    public void GetPredationScore_MicrobeAtExactThresholdCanPredate()
    {
        var predator = CreateMicrobe(1, "ThresholdPredator", 3);
        var prey = CreateMicrobe(2, "ThresholdPrey", 2);
        var cache = CreateCache();
        var biome = SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions;

        var predatorSize = cache.GetBaseHexSizeForSpecies(predator);
        var preySize = cache.GetBaseHexSizeForSpecies(prey);

        AssertThat(predatorSize).IsEqual(preySize * Constants.ENGULF_SIZE_RATIO_REQ);

        var coldScore = cache.GetPredationScore(predator, prey, biome);
        AssertThat(coldScore).IsGreater(0.0f);

        var warmScore = cache.GetPredationScore(predator, prey, biome);
        AssertThat(warmScore).IsEqual(coldScore);
    }

    [TestCase]
    public void GetEnzymesScore_MulticellularThresholdMatrixIsInclusive()
    {
        const float preySize = 2.0f;
        var predator = CreateMulticellular(3, "ThresholdMulticellularPredator", 3);
        var cache = CreateCache();

        var predatorCellSize = cache.GetBaseHexSizeForCellType(predator.CellTypes[0]);
        AssertThat(predatorCellSize).IsEqual(preySize * Constants.ENGULF_SIZE_RATIO_REQ);

        var belowThreshold = cache.GetEnzymesScore(predator, Constants.LIPASE_ENZYME,
            float.BitIncrement(preySize), 0.0f);
        var atThreshold = cache.GetEnzymesScore(predator, Constants.LIPASE_ENZYME, preySize, 0.0f);
        var aboveThreshold = cache.GetEnzymesScore(predator, Constants.LIPASE_ENZYME,
            float.BitDecrement(preySize), 0.0f);

        AssertThat(belowThreshold).IsEqual(0.0f);
        AssertThat(atThreshold).IsGreater(0.0f);
        AssertThat(atThreshold).IsEqual(aboveThreshold);
    }

    [TestCase]
    public void ChunkCompoundPressure_MicrobeThresholdMatrixIsInclusive()
    {
        AssertChunkCompoundPressureThresholdMatrix(ThresholdSpeciesKind.Microbe);
    }

    [TestCase]
    public void ChunkCompoundPressure_MulticellularThresholdMatrixIsInclusive()
    {
        AssertChunkCompoundPressureThresholdMatrix(ThresholdSpeciesKind.Multicellular);
    }

    [TestCase]
    public void ReproductionCompoundPressure_MicrobeThresholdMatrixIsInclusive()
    {
        AssertReproductionCompoundPressureThresholdMatrix(ThresholdSpeciesKind.Microbe);
    }

    [TestCase]
    public void ReproductionCompoundPressure_MulticellularThresholdMatrixIsInclusive()
    {
        AssertReproductionCompoundPressureThresholdMatrix(ThresholdSpeciesKind.Multicellular);
    }

    private static SimulationCache CreateCache()
    {
        return new SimulationCache(new WorldGenerationSettings
        {
            Seed = 1,
        });
    }

    private static void AssertChunkCompoundPressureThresholdMatrix(ThresholdSpeciesKind speciesKind)
    {
        const float chunkSize = 2.0f;

        var belowThreshold = CalculateChunkCompoundPressureScore(speciesKind, float.BitIncrement(chunkSize));
        var atThreshold = CalculateChunkCompoundPressureScore(speciesKind, chunkSize);
        var aboveThreshold = CalculateChunkCompoundPressureScore(speciesKind, float.BitDecrement(chunkSize));

        AssertThat(atThreshold).IsEqual(aboveThreshold);
        AssertThat(atThreshold).IsGreater(belowThreshold);
    }

    private static float CalculateChunkCompoundPressureScore(ThresholdSpeciesKind speciesKind, float chunkSize)
    {
        var (species, patch, cache) = CreatePressureFixture(speciesKind, chunkSize);
        var pressure = new ChunkCompoundPressure("marineSnow", new LocalizedString("MARINE_SNOW"),
            Compound.Glucose, Compound.ATP, false, 1.0f);

        return pressure.Score(species, patch, cache);
    }

    private static void AssertReproductionCompoundPressureThresholdMatrix(ThresholdSpeciesKind speciesKind)
    {
        const float chunkSize = 2.0f;

        var belowThreshold = CalculateReproductionCompoundPressureScore(speciesKind, float.BitIncrement(chunkSize));
        var atThreshold = CalculateReproductionCompoundPressureScore(speciesKind, chunkSize);
        var aboveThreshold = CalculateReproductionCompoundPressureScore(speciesKind, float.BitDecrement(chunkSize));

        AssertThat(atThreshold).IsEqual(aboveThreshold);
        AssertThat(atThreshold).IsGreater(belowThreshold);
    }

    private static float CalculateReproductionCompoundPressureScore(ThresholdSpeciesKind speciesKind, float chunkSize)
    {
        var (species, patch, cache) = CreatePressureFixture(speciesKind, chunkSize);
        var pressure = new ReproductionCompoundPressure(Compound.Ammonia, false, 1.0f);

        return pressure.Score(species, patch, cache);
    }

    private static (Species Species, Patch Patch, SimulationCache Cache) CreatePressureFixture(
        ThresholdSpeciesKind speciesKind, float chunkSize)
    {
        var worldSettings = new WorldGenerationSettings
        {
            Seed = 1,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        };

        var world = new GameWorld(worldSettings, CreateMicrobe(100, "PressurePlayer", 1));
        var patch = world.Map.CurrentPatch!;
        var chunk = SimulationParameters.Instance.GetBiome("mesopelagic").Conditions.Chunks["marineSnow"];
        chunk.Size = chunkSize;
        patch.Biome.Chunks = new Dictionary<string, ChunkConfiguration>
        {
            ["marineSnow"] = chunk,
        };

        Species species = speciesKind switch
        {
            ThresholdSpeciesKind.Microbe => CreateMicrobe(101, "PressureMicrobe", 6),
            ThresholdSpeciesKind.Multicellular => CreateMulticellular(102, "PressureMulticellular", 3),
            _ => throw new ArgumentOutOfRangeException(nameof(speciesKind), speciesKind, null),
        };

        var cache = new SimulationCache(worldSettings);
        var engulferSize = speciesKind switch
        {
            ThresholdSpeciesKind.Microbe => cache.GetBaseHexSizeForSpecies((MicrobeSpecies)species),
            ThresholdSpeciesKind.Multicellular =>
                cache.GetBaseHexSizeForCellType(((MulticellularSpecies)species).CellTypes[0]),
            _ => throw new ArgumentOutOfRangeException(nameof(speciesKind), speciesKind, null),
        };

        AssertThat(engulferSize).IsEqual(3.0f);
        AssertThat(chunk.Size).IsEqual(chunkSize);

        if (chunkSize > 2.0f)
            AssertThat(chunk.Size * Constants.ENGULF_SIZE_RATIO_REQ).IsGreater(engulferSize);

        return (species, patch, cache);
    }

    private static MicrobeSpecies CreateMicrobe(uint id, string epithet, int cytoplasmCount)
    {
        var simulationParameters = SimulationParameters.Instance;
        var species = new MicrobeSpecies(id, "Threshold", epithet)
        {
            IsBacteria = true,
            MembraneType = simulationParameters.GetMembrane("single"),
        };

        for (var i = 0; i < cytoplasmCount; ++i)
        {
            species.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"),
                new Hex(i * 4, 0), 0));
        }

        species.OnEdited();
        return species;
    }

    private static MulticellularSpecies CreateMulticellular(uint id, string epithet, int cytoplasmCount)
    {
        var simulationParameters = SimulationParameters.Instance;
        var cellType = new CellType(simulationParameters.GetMembrane("single"))
        {
            CellTypeName = "ThresholdCell",
        };

        for (var i = 0; i < cytoplasmCount; ++i)
        {
            cellType.ModifiableOrganelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"),
                new Hex(i * 4, 0), 0));
        }

        var species = new MulticellularSpecies(id, "Threshold", epithet);
        species.ModifiableCellTypes.Add(cellType);
        species.ModifiableGameplayCells.AddFast(new CellTemplate(cellType, new Hex(0, 0), 0),
            new List<Hex>(), new List<Hex>());
        species.OnEdited();
        return species;
    }
}
