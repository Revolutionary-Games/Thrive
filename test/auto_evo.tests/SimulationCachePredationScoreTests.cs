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
        AssertThat(scoreAgainstPreyWithSlimeJet < scoreAgainstPreyWithoutSlimeJet).IsTrue();
    }

    [TestCase]
    public void ChannelInhibitorPredationScoreIncreasesAsMovementFundingDecreases()
    {
        var fullyFundedMovementScore = CalculateChannelInhibitorPredationScore(24.0f);
        var halfFundedMovementScore = CalculateChannelInhibitorPredationScore(14.0f);
        var unfundedMovementScore = CalculateChannelInhibitorPredationScore(4.0f);

        AssertThat(float.IsFinite(fullyFundedMovementScore) && float.IsFinite(halfFundedMovementScore) &&
            float.IsFinite(unfundedMovementScore)).IsTrue();
        AssertThat(fullyFundedMovementScore < halfFundedMovementScore &&
            halfFundedMovementScore < unfundedMovementScore).IsTrue();
    }

    [TestCase]
    public void ChannelInhibitorMovementFundingUsesFullStationaryConsumption()
    {
        var allStationaryConsumptionIsOsmoregulation =
            CalculateChannelInhibitorPredationScore(20.0f, 5.0f, 5.0f);
        var stationaryConsumptionIncludesOtherProcesses =
            CalculateChannelInhibitorPredationScore(20.0f, 0.0f, 5.0f);

        AssertThat(float.IsFinite(allStationaryConsumptionIsOsmoregulation) &&
            float.IsFinite(stationaryConsumptionIncludesOtherProcesses)).IsTrue();
        AssertThat(stationaryConsumptionIncludesOtherProcesses)
            .IsEqual(allStationaryConsumptionIsOsmoregulation);
    }

    [TestCase]
    public void ChannelInhibitorDoesNotSlowSpeciesWithoutMovementCost()
    {
        var inhibitedBelowStationaryConsumption =
            CalculateChannelInhibitorPredationScore(4.0f, 0.0f, 5.0f, 0.0f);
        var productionAboveStationaryConsumption =
            CalculateChannelInhibitorPredationScore(12.0f, 0.0f, 5.0f, 0.0f);

        AssertThat(float.IsFinite(inhibitedBelowStationaryConsumption) &&
            float.IsFinite(productionAboveStationaryConsumption)).IsTrue();
        AssertThat(inhibitedBelowStationaryConsumption)
            .IsEqual(productionAboveStationaryConsumption);
    }

    [TestCase]
    public void ChannelInhibitorSlowsSprintEscapeSpeed()
    {
        var fullyFundedMovementWithoutSprint = CalculateChannelInhibitorPredationScore(24.0f);
        var unfundedMovementWithoutSprint = CalculateChannelInhibitorPredationScore(4.0f);
        var fullyFundedMovementWithSprint = CalculateChannelInhibitorPredationScore(24.0f, finalBalance: 1.0f);
        var unfundedMovementWithSprint = CalculateChannelInhibitorPredationScore(4.0f, finalBalance: 1.0f);

        var inhibitionBenefitWithoutSprint = unfundedMovementWithoutSprint - fullyFundedMovementWithoutSprint;
        var inhibitionBenefitWithSprint = unfundedMovementWithSprint - fullyFundedMovementWithSprint;

        AssertThat(inhibitionBenefitWithSprint > inhibitionBenefitWithoutSprint).IsTrue();
    }

    private static float CalculatePredationScore(Species predator, Species prey)
    {
        var cache = CreateCache();
        var biome = SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions;

        return cache.GetPredationScore(predator, prey, biome);
    }

    private static float CalculateChannelInhibitorPredationScore(float totalProduction, float osmoregulation = 2.0f,
        float stationaryConsumption = 2.0f, float movementConsumption = 10.0f, float finalBalance = 0.0f)
    {
        var cache = CreateCache();
        var predator = CreateChannelInhibitorPredator(9);
        var prey = CreateMicrobe(10, "ChannelInhibitorPrey", "single", "cytoplasm");
        prey.ModifiableBehaviour.Fear = Constants.MAX_SPECIES_FEAR;
        prey.ModifiableBehaviour.Aggression = 0;

        var rawScores = cache.GetPredationToolsRawScores(predator);
        AssertThat(rawScores.ChannelInhibitorScore > 0.0f).IsTrue();
        AssertThat(rawScores.MacrolideScore).IsEqual(0.0f);

        var biome = SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions;
        var preyEnergyBalance = cache.GetEnergyBalanceForSpecies(prey, biome);
        preyEnergyBalance.TotalProduction = totalProduction;
        preyEnergyBalance.Osmoregulation = osmoregulation;
        preyEnergyBalance.TotalConsumptionStationary = stationaryConsumption;
        preyEnergyBalance.TotalMovement = movementConsumption;
        preyEnergyBalance.TotalConsumption = stationaryConsumption + movementConsumption;
        preyEnergyBalance.FinalBalance = finalBalance;
        preyEnergyBalance.FinalBalanceStationary = 0.0f;

        return cache.GetPredationScore(predator, prey, biome);
    }

    private static SimulationCache CreateCache()
    {
        return new SimulationCache(new WorldGenerationSettings
        {
            Seed = 1,
        });
    }

    private static MicrobeSpecies CreateChannelInhibitorPredator(uint id)
    {
        var simulationParameters = SimulationParameters.Instance;
        var species = CreateMicrobe(id, "ChannelInhibitorPredator", "single", "cytoplasm", "pilus");
        species.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("oxytoxy"),
            new Hex(8, 0), 0)
        {
            ModifiableUpgrades = new OrganelleUpgrades
            {
                ModifiableUnlockedFeatures = [ToxinUpgradeNames.CHANNEL_INHIBITOR_UPGRADE_NAME],
                CustomUpgradeData = new ToxinUpgrades(ToxinType.ChannelInhibitor, 0.0f),
            },
        });
        species.OnEdited();

        return species;
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
