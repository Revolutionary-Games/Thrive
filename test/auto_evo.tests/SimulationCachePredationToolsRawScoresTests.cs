using System;
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
        AssertFloatBits(scores.PilusScore, 0x459C4000);
        AssertFloatBits(scores.InjectisomeScore, 0x00000000);
        AssertFloatBits(scores.DefensivePilusScore, 0x00000000);
        AssertFloatBits(scores.DefensiveInjectisomeScore, 0x00000000);
        AssertFloatBits(scores.AverageToxicity, 0x3E800000);
        AssertFloatBits(scores.OxytoxyScore, 0x4A844702);
        AssertFloatBits(scores.CytotoxinScore, 0x00000000);
        AssertFloatBits(scores.MacrolideScore, 0x00000000);
        AssertFloatBits(scores.ChannelInhibitorScore, 0x00000000);
        AssertFloatBits(scores.OxygenMetabolismInhibitorScore, 0x00000000);
        AssertFloatBits(scores.SlimeJetScore, 0x42160000);
        AssertFloatBits(scores.MucocystsScore, 0x00000000);
        AssertFloatBits(scores.PullingCiliaModifier, 0x40100000);
    }

    private static void AssertMicrobeRecomputedScores(SimulationCache.PredationToolsRawScores scores)
    {
        AssertFloatBits(scores.PilusScore, 0x459C4000);
        AssertFloatBits(scores.InjectisomeScore, 0x00000000);
        AssertFloatBits(scores.DefensivePilusScore, 0x00000000);
        AssertFloatBits(scores.DefensiveInjectisomeScore, 0x00000000);
        AssertFloatBits(scores.AverageToxicity, 0x3E800000);
        AssertFloatBits(scores.OxytoxyScore, 0x4AD3A4D0);
        AssertFloatBits(scores.CytotoxinScore, 0x00000000);
        AssertFloatBits(scores.MacrolideScore, 0x00000000);
        AssertFloatBits(scores.ChannelInhibitorScore, 0x00000000);
        AssertFloatBits(scores.OxygenMetabolismInhibitorScore, 0x00000000);
        AssertFloatBits(scores.SlimeJetScore, 0x42700000);
        AssertFloatBits(scores.MucocystsScore, 0x00000000);
        AssertFloatBits(scores.PullingCiliaModifier, 0x40666666);
    }

    private static void AssertMulticellularInitialScores(SimulationCache.PredationToolsRawScores scores)
    {
        AssertFloatBits(scores.PilusScore, 0x45DCF88B);
        AssertFloatBits(scores.InjectisomeScore, 0x00000000);
        AssertFloatBits(scores.DefensivePilusScore, 0x00000000);
        AssertFloatBits(scores.DefensiveInjectisomeScore, 0x00000000);
        AssertFloatBits(scores.AverageToxicity, 0x3E800000);
        AssertFloatBits(scores.OxytoxyScore, 0x4B26AB62);
        AssertFloatBits(scores.CytotoxinScore, 0x00000000);
        AssertFloatBits(scores.MacrolideScore, 0x00000000);
        AssertFloatBits(scores.ChannelInhibitorScore, 0x00000000);
        AssertFloatBits(scores.OxygenMetabolismInhibitorScore, 0x00000000);
        AssertFloatBits(scores.SlimeJetScore, 0x42BCFFFF);
        AssertFloatBits(scores.MucocystsScore, 0x00000000);
        AssertFloatBits(scores.PullingCiliaModifier, 0x401ADEF9);
    }

    private static void AssertMulticellularRecomputedScores(SimulationCache.PredationToolsRawScores scores)
    {
        AssertFloatBits(scores.PilusScore, 0x45DCF88B);
        AssertFloatBits(scores.InjectisomeScore, 0x00000000);
        AssertFloatBits(scores.DefensivePilusScore, 0x00000000);
        AssertFloatBits(scores.DefensiveInjectisomeScore, 0x00000000);
        AssertFloatBits(scores.AverageToxicity, 0x3E800000);
        AssertFloatBits(scores.OxytoxyScore, 0x4B7A0113);
        AssertFloatBits(scores.CytotoxinScore, 0x00000000);
        AssertFloatBits(scores.MacrolideScore, 0x00000000);
        AssertFloatBits(scores.ChannelInhibitorScore, 0x00000000);
        AssertFloatBits(scores.OxygenMetabolismInhibitorScore, 0x00000000);
        AssertFloatBits(scores.SlimeJetScore, 0x430DC000);
        AssertFloatBits(scores.MucocystsScore, 0x00000000);
        AssertFloatBits(scores.PullingCiliaModifier, 0x402F4B35);
    }

    private static void AssertFloatBits(float actual, int expectedBits)
    {
        AssertThat(BitConverter.SingleToInt32Bits(actual)).IsEqual(expectedBits);
    }
}
