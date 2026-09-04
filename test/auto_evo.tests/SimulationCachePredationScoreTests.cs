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

        AssertThat(score).IsEqual(61.029884f);
    }

    [TestCase]
    public void PreySlimeJetPropulsionReducesCatchability()
    {
        var predator = CreateSlimeJetPredator(9);
        var preyWithoutSlimeJet = CreatePrey(10, false);
        var preyWithSlimeJet = CreatePrey(11, true);
        var cache = new SimulationCache(new WorldGenerationSettings
        {
            Seed = 1,
        });
        var biome = SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions;

        var preyWithoutSlimeJetRawScores = cache.GetPredationToolsRawScores(preyWithoutSlimeJet);
        var preyWithSlimeJetRawScores = cache.GetPredationToolsRawScores(preyWithSlimeJet);
        var scoreAgainstPreyWithoutSlimeJet = cache.GetPredationScore(predator, preyWithoutSlimeJet, biome);
        var scoreAgainstPreyWithSlimeJet = cache.GetPredationScore(predator, preyWithSlimeJet, biome);

        AssertThat(preyWithoutSlimeJetRawScores.SlimeJetScore).IsEqual(0.0f);
        AssertThat(preyWithSlimeJetRawScores.SlimeJetScore > 0.0f).IsTrue();
        AssertThat(float.IsFinite(scoreAgainstPreyWithoutSlimeJet)).IsTrue();
        AssertThat(float.IsFinite(scoreAgainstPreyWithSlimeJet)).IsTrue();
        AssertThat(scoreAgainstPreyWithoutSlimeJet > 0.0f).IsTrue();
        AssertThat(scoreAgainstPreyWithSlimeJet > 0.0f).IsTrue();
        AssertThat(scoreAgainstPreyWithoutSlimeJet).IsEqual(80417.01f);
        AssertThat(scoreAgainstPreyWithSlimeJet).IsEqual(80317.52f);
        AssertThat(scoreAgainstPreyWithSlimeJet < scoreAgainstPreyWithoutSlimeJet).IsTrue();
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

    private static MicrobeSpecies CreateSlimeJetPredator(uint id)
    {
        var simulationParameters = SimulationParameters.Instance;
        var species = CreateMicrobe(id, "SlimeJetPredator", "cellulose", "cytoplasm");
        species.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("pilus"),
            new Hex(0, -4), 0));
        species.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("slimeJet"),
            new Hex(0, 4), 0));
        species.OnEdited();

        return species;
    }

    private static MicrobeSpecies CreatePrey(uint id, bool hasSlimeJet)
    {
        var simulationParameters = SimulationParameters.Instance;
        var species = CreateMicrobe(id, hasSlimeJet ? "SlimeJetPrey" : "NoSlimeJetPrey", "cellulose",
            "cytoplasm");
        species.Organelles.Add(new OrganelleTemplate(
            simulationParameters.GetOrganelleType(hasSlimeJet ? "slimeJet" : "chemoreceptor"), new Hex(0, 4), 0));
        species.ModifiableBehaviour.Fear = 0;
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
