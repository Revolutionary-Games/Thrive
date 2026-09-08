using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;
using static SimulationCacheTestFixtures;

[TestSuite]
[RequireGodotRuntime]
public class SimulationCachePredationScoreTests
{
    [TestCase]
    public void PredatorWithoutPredationAbilityScoresZero()
    {
        var predator = CreateMicrobe(1, "NoTools", "cellulose", "cytoplasm");
        var prey = CreateMicrobe(2, "NoToolsPrey", "single", "cytoplasm");

        AssertPredationLifecycle(predator, prey, 0.0f);
    }

    [TestCase]
    public void EngulfingPredatorScoreIsCharacterized()
    {
        var predator = CreateMicrobe(3, "Engulfer", "single",
            "cytoplasm", "cytoplasm", "cytoplasm", "cytoplasm");
        var prey = CreateMicrobe(4, "Engulfed", "single", "cytoplasm");

        AssertPredationLifecycle(predator, prey, 233.40552f);
    }

    [TestCase]
    public void PilusAndToxinPredatorScoreIsCharacterized()
    {
        var predator = CreateMicrobe(5, "Armed", "cellulose", "cytoplasm", "pilus", "oxytoxy");
        var prey = CreateMicrobe(6, "ArmouredPrey", "cellulose", "cytoplasm");

        AssertPredationLifecycle(predator, prey, 0.17211556f);
    }

    [TestCase]
    public void MulticellularPredatorScoreIsCharacterized()
    {
        var predator = CreateMulticellularPredator(7);
        var prey = CreateMicrobe(8, "MulticellularPrey", "single", "cytoplasm");

        AssertPredationLifecycle(predator, prey, 61.029884f);
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
        var cellType = CreateCellType("Predator", "single",
            CreateOrganelle("cytoplasm", new Hex(0, 0)),
            CreateOrganelle("cytoplasm", new Hex(4, 0)),
            CreateOrganelle("cytoplasm", new Hex(8, 0)),
            CreateOrganelle("cytoplasm", new Hex(12, 0)));

        return CreateMulticellular(id, "MulticellularPredator", (cellType, new Hex(0, 0)));
    }
}
