using System.Collections.Generic;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class SimulationCachePredationScoreTests
{
    [TestCase]
    public void PredatorWithoutPredationAbilityScoresZero()
    {
        var predator = CreateMicrobe(1, "NoTools", "cellulose", "cytoplasm");
        var prey = CreateMicrobe(2, "NoToolsPrey", "single", "cytoplasm");

        var score = CalculatePredationScore(predator, prey);

        AssertThat(score).IsEqual(0.0f);
    }

    [TestCase]
    public void EngulfingPredatorScoreIsCharacterized()
    {
        var predator = CreateMicrobe(3, "Engulfer", "single",
            "cytoplasm", "cytoplasm", "cytoplasm", "cytoplasm");
        var prey = CreateMicrobe(4, "Engulfed", "single", "cytoplasm");

        var score = CalculatePredationScore(predator, prey);

        AssertThat(score).IsEqual(273.68018f);
    }

    [TestCase]
    public void PilusAndToxinPredatorScoreIsCharacterized()
    {
        var predator = CreateMicrobe(5, "Armed", "cellulose", "cytoplasm", "pilus", "oxytoxy");
        var prey = CreateMicrobe(6, "ArmouredPrey", "cellulose", "cytoplasm");

        var score = CalculatePredationScore(predator, prey);

        AssertThat(score).IsEqual(0.17211556f);
    }

    [TestCase]
    public void MulticellularPredatorScoreIsCharacterized()
    {
        var predator = CreateMulticellularPredator(7);
        var prey = CreateMicrobe(8, "MulticellularPrey", "single", "cytoplasm");

        var score = CalculatePredationScore(predator, prey);

        AssertThat(score).IsEqual(258.4216f);
    }

    private static float CalculatePredationScore(Species predator, Species prey)
    {
        var worldSettings = new WorldGenerationSettings
        {
            Seed = 1,
        };
        var cache = new SimulationCache(worldSettings);
        var biome = SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions;

        return cache.GetPredationScore(predator, prey, biome);
    }

    private static MicrobeSpecies CreateMicrobe(uint id, string epithet, string membrane,
        params string[] organelles)
    {
        var simulationParameters = SimulationParameters.Instance;
        var species = new MicrobeSpecies(id, "Characterization", epithet)
        {
            IsBacteria = true,
            MembraneType = simulationParameters.GetMembrane(membrane),
        };

        for (var i = 0; i < organelles.Length; ++i)
        {
            species.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType(organelles[i]),
                new Hex(i * 4, 0), 0));
        }

        species.OnEdited();
        return species;
    }

    private static MulticellularSpecies CreateMulticellularPredator(uint id)
    {
        var simulationParameters = SimulationParameters.Instance;
        var cellType = new CellType(simulationParameters.GetMembrane("single"))
        {
            CellTypeName = "Predator",
        };

        for (var i = 0; i < 4; ++i)
        {
            cellType.ModifiableOrganelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"),
                new Hex(i * 4, 0), 0));
        }

        var species = new MulticellularSpecies(id, "Characterization", "MulticellularPredator");
        species.ModifiableCellTypes.Add(cellType);
        species.ModifiableGameplayCells.AddFast(new CellTemplate(cellType, new Hex(0, 0), 0),
            new List<Hex>(), new List<Hex>());
        species.OnEdited();

        return species;
    }
}
