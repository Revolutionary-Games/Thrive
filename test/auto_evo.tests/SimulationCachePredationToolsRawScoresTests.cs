using System.Collections.Generic;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class SimulationCachePredationToolsRawScoresTests
{
    [TestCase]
    public void MicrobeRawScoresAndCacheBehaviorAreCharacterized()
    {
        var cache = CreateCache();
        var species = CreateMicrobe(101);
        var originalId = species.ID;
        var originalEpithet = species.Epithet;
        var originalAutoEvoAttemptCache = species.AutoEvoAttemptCache;

        var initial = cache.GetPredationToolsRawScores(species);
        AssertMicrobeInitialScores(initial);

        species.CellTypeSpecializationBonus = 2.0f;
        AssertSpeciesCacheIdentityIsStable(species, originalId, originalEpithet, originalAutoEvoAttemptCache);

        var cached = cache.GetPredationToolsRawScores(species);
        AssertMicrobeInitialScores(cached);

        cache.Clear();

        var recomputed = cache.GetPredationToolsRawScores(species);
        AssertThat(recomputed.OxytoxyScore).IsNotEqual(initial.OxytoxyScore);
        AssertThat(recomputed.SlimeJetScore).IsNotEqual(initial.SlimeJetScore);
        AssertThat(recomputed.PullingCiliaModifier).IsNotEqual(initial.PullingCiliaModifier);
        AssertMicrobeRecomputedScores(recomputed);
    }

    [TestCase]
    public void MulticellularRawScoresAndCacheBehaviorAreCharacterized()
    {
        var cache = CreateCache();
        var (species, contributingCellType) = CreateMulticellularSpecies(102);
        var originalId = species.ID;
        var originalEpithet = species.Epithet;
        var originalAutoEvoAttemptCache = species.AutoEvoAttemptCache;

        var initial = cache.GetPredationToolsRawScores(species);
        AssertMulticellularInitialScores(initial);

        contributingCellType.CellTypeSpecializationBonus = 2.25f;
        AssertSpeciesCacheIdentityIsStable(species, originalId, originalEpithet, originalAutoEvoAttemptCache);

        var cached = cache.GetPredationToolsRawScores(species);
        AssertMulticellularInitialScores(cached);

        cache.Clear();

        var recomputed = cache.GetPredationToolsRawScores(species);
        AssertThat(recomputed.OxytoxyScore).IsNotEqual(initial.OxytoxyScore);
        AssertThat(recomputed.SlimeJetScore).IsNotEqual(initial.SlimeJetScore);
        AssertThat(recomputed.PullingCiliaModifier).IsNotEqual(initial.PullingCiliaModifier);
        AssertMulticellularRecomputedScores(recomputed);
    }

    private static SimulationCache CreateCache()
    {
        return new SimulationCache(new WorldGenerationSettings
        {
            Seed = 1,
        });
    }

    private static MicrobeSpecies CreateMicrobe(uint id)
    {
        var simulationParameters = SimulationParameters.Instance;
        var species = new MicrobeSpecies(id, "Characterization", "RawScoreMicrobe")
        {
            IsBacteria = true,
            MembraneType = simulationParameters.GetMembrane("single"),
        };

        AddPredationToolOrganelles(species.Organelles, simulationParameters);
        species.OnEdited();
        species.CellTypeSpecializationBonus = 1.25f;

        return species;
    }

    private static (MulticellularSpecies Species, CellType ContributingCellType) CreateMulticellularSpecies(uint id)
    {
        var simulationParameters = SimulationParameters.Instance;
        var contributingCellType = new CellType(simulationParameters.GetMembrane("single"))
        {
            CellTypeName = "PredationTools",
        };
        AddPredationToolOrganelles(contributingCellType.ModifiableOrganelles, simulationParameters);

        var supportingCellType = new CellType(simulationParameters.GetMembrane("single"))
        {
            CellTypeName = "Support",
        };
        supportingCellType.ModifiableOrganelles.Add(CreateOrganelle(simulationParameters, "cytoplasm", new Hex(0, 0)));

        var species = new MulticellularSpecies(id, "Characterization", "RawScoreMulticellular");
        species.ModifiableCellTypes.Add(contributingCellType);
        species.ModifiableCellTypes.Add(supportingCellType);
        species.ModifiableGameplayCells.AddFast(new CellTemplate(contributingCellType, new Hex(0, 0), 0),
            new List<Hex>(), new List<Hex>());
        species.ModifiableGameplayCells.AddFast(new CellTemplate(contributingCellType, new Hex(1, 0), 0),
            new List<Hex>(), new List<Hex>());
        species.ModifiableGameplayCells.AddFast(new CellTemplate(supportingCellType, new Hex(0, 1), 0),
            new List<Hex>(), new List<Hex>());
        species.OnEdited();

        contributingCellType.CellTypeSpecializationBonus = 1.5f;
        supportingCellType.CellTypeSpecializationBonus = 0.75f;

        return (species, contributingCellType);
    }

    private static void AddPredationToolOrganelles(OrganelleLayout<OrganelleTemplate> organelles,
        SimulationParameters simulationParameters)
    {
        organelles.Add(CreateOrganelle(simulationParameters, "cytoplasm", new Hex(0, 0)));
        organelles.Add(CreateOrganelle(simulationParameters, "pilus", new Hex(0, -4)));
        organelles.Add(CreateOrganelle(simulationParameters, "slimeJet", new Hex(0, 4)));
        organelles.Add(CreateUpgradedOrganelle(simulationParameters, "cilia", new Hex(4, 0),
            CiliaComponent.CILIA_PULL_UPGRADE_NAME));
        organelles.Add(CreateToxinOrganelle(simulationParameters, new Hex(-4, 0)));
    }

    private static OrganelleTemplate CreateOrganelle(SimulationParameters simulationParameters, string internalName,
        Hex position)
    {
        return new OrganelleTemplate(simulationParameters.GetOrganelleType(internalName), position, 0);
    }

    private static OrganelleTemplate CreateUpgradedOrganelle(SimulationParameters simulationParameters,
        string internalName, Hex position, string upgrade)
    {
        return new OrganelleTemplate(simulationParameters.GetOrganelleType(internalName), position, 0)
        {
            ModifiableUpgrades = new OrganelleUpgrades
            {
                ModifiableUnlockedFeatures = [upgrade],
            },
        };
    }

    private static OrganelleTemplate CreateToxinOrganelle(SimulationParameters simulationParameters, Hex position)
    {
        return new OrganelleTemplate(simulationParameters.GetOrganelleType("oxytoxy"), position, 0)
        {
            ModifiableUpgrades = new OrganelleUpgrades
            {
                ModifiableUnlockedFeatures = [ToxinUpgradeNames.OXYTOXY_UPGRADE_NAME],
                CustomUpgradeData = new ToxinUpgrades(ToxinType.Oxytoxy, 0.25f),
            },
        };
    }

    private static void AssertSpeciesCacheIdentityIsStable(Species species, uint expectedId, string expectedEpithet,
        int expectedAutoEvoAttemptCache)
    {
        AssertThat(species.ID).IsEqual(expectedId);
        AssertThat(species.Epithet).IsEqual(expectedEpithet);
        AssertThat(species.AutoEvoAttemptCache).IsEqual(expectedAutoEvoAttemptCache);
    }

    private static void AssertMicrobeInitialScores(SimulationCache.PredationToolsRawScores scores)
    {
        AssertThat(scores.PilusScore).IsEqual(5000.0f);
        AssertThat(scores.InjectisomeScore).IsEqual(0.0f);
        AssertThat(scores.DefensivePilusScore).IsEqual(0.0f);
        AssertThat(scores.DefensiveInjectisomeScore).IsEqual(0.0f);
        AssertThat(scores.AverageToxicity).IsEqual(0.25f);
        AssertThat(scores.OxytoxyScore).IsEqual(4334465.0f);
        AssertThat(scores.CytotoxinScore).IsEqual(0.0f);
        AssertThat(scores.MacrolideScore).IsEqual(0.0f);
        AssertThat(scores.ChannelInhibitorScore).IsEqual(0.0f);
        AssertThat(scores.OxygenMetabolismInhibitorScore).IsEqual(0.0f);
        AssertThat(scores.SlimeJetScore).IsEqual(37.5f);
        AssertThat(scores.MucocystsScore).IsEqual(0.0f);
        AssertThat(scores.PullingCiliaModifier).IsEqual(2.25f);
    }

    private static void AssertMicrobeRecomputedScores(SimulationCache.PredationToolsRawScores scores)
    {
        AssertThat(scores.PilusScore).IsEqual(5000.0f);
        AssertThat(scores.InjectisomeScore).IsEqual(0.0f);
        AssertThat(scores.DefensivePilusScore).IsEqual(0.0f);
        AssertThat(scores.DefensiveInjectisomeScore).IsEqual(0.0f);
        AssertThat(scores.AverageToxicity).IsEqual(0.25f);
        AssertThat(scores.OxytoxyScore).IsEqual(6935144.0f);
        AssertThat(scores.CytotoxinScore).IsEqual(0.0f);
        AssertThat(scores.MacrolideScore).IsEqual(0.0f);
        AssertThat(scores.ChannelInhibitorScore).IsEqual(0.0f);
        AssertThat(scores.OxygenMetabolismInhibitorScore).IsEqual(0.0f);
        AssertThat(scores.SlimeJetScore).IsEqual(60.0f);
        AssertThat(scores.MucocystsScore).IsEqual(0.0f);
        AssertThat(scores.PullingCiliaModifier).IsEqual(3.6f);
    }

    private static void AssertMulticellularInitialScores(SimulationCache.PredationToolsRawScores scores)
    {
        AssertThat(scores.PilusScore).IsEqual(7071.068f);
        AssertThat(scores.InjectisomeScore).IsEqual(0.0f);
        AssertThat(scores.DefensivePilusScore).IsEqual(0.0f);
        AssertThat(scores.DefensiveInjectisomeScore).IsEqual(0.0f);
        AssertThat(scores.AverageToxicity).IsEqual(0.25f);
        AssertThat(scores.OxytoxyScore).IsEqual(10922850.0f);
        AssertThat(scores.CytotoxinScore).IsEqual(0.0f);
        AssertThat(scores.MacrolideScore).IsEqual(0.0f);
        AssertThat(scores.ChannelInhibitorScore).IsEqual(0.0f);
        AssertThat(scores.OxygenMetabolismInhibitorScore).IsEqual(0.0f);
        AssertThat(scores.SlimeJetScore).IsEqual(94.49999f);
        AssertThat(scores.MucocystsScore).IsEqual(0.0f);
        AssertThat(scores.PullingCiliaModifier).IsEqual(2.4198592f);
    }

    private static void AssertMulticellularRecomputedScores(SimulationCache.PredationToolsRawScores scores)
    {
        AssertThat(scores.PilusScore).IsEqual(7071.068f);
        AssertThat(scores.InjectisomeScore).IsEqual(0.0f);
        AssertThat(scores.DefensivePilusScore).IsEqual(0.0f);
        AssertThat(scores.DefensiveInjectisomeScore).IsEqual(0.0f);
        AssertThat(scores.AverageToxicity).IsEqual(0.25f);
        AssertThat(scores.OxytoxyScore).IsEqual(16384275.0f);
        AssertThat(scores.CytotoxinScore).IsEqual(0.0f);
        AssertThat(scores.MacrolideScore).IsEqual(0.0f);
        AssertThat(scores.ChannelInhibitorScore).IsEqual(0.0f);
        AssertThat(scores.OxygenMetabolismInhibitorScore).IsEqual(0.0f);
        AssertThat(scores.SlimeJetScore).IsEqual(141.75f);
        AssertThat(scores.MucocystsScore).IsEqual(0.0f);
        AssertThat(scores.PullingCiliaModifier).IsEqual(2.7389653f);
    }
}
