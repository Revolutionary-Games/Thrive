using System.Collections.Generic;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;
using static SimulationCacheTestFixtures;

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

        var fresh = CreateCache().GetPredationToolsRawScores(species);
        AssertMicrobeRecomputedScores(fresh);

        cache.Clear();

        var recomputed = cache.GetPredationToolsRawScores(species);
        AssertDifferentBits(recomputed.OxytoxyScore, initial.OxytoxyScore);
        AssertDifferentBits(recomputed.SlimeJetScore, initial.SlimeJetScore);
        AssertDifferentBits(recomputed.PullingCiliaModifier, initial.PullingCiliaModifier);
        AssertMicrobeRecomputedScores(recomputed);
        AssertRawBits(recomputed, fresh);
    }

    [TestCase]
    public void MicrobeWithoutPullingCiliaDoesNotGainModifierFromSpecialization()
    {
        var cache = CreateCache();
        var species = CreateMicrobe(103);
        species.Organelles.Clear();
        species.Organelles.Add(CreateOrganelle("cytoplasm", new Hex(0, 0)));
        species.OnEdited();
        species.CellTypeSpecializationBonus = 2.0f;

        var scores = cache.GetPredationToolsRawScores(species);

        AssertThat(scores.PullingCiliaModifier).IsEqual(1.0f);
    }

    [TestCase]
    public void MicrobeOxygenMetabolismInhibitorScoreUsesSpecializationAndCache()
    {
        const float noSpecializationBonus = 1.0f;
        const float specializedBonus = 2.0f;

        var cache = CreateCache();
        var species = CreateMicrobeWithOxygenMetabolismInhibitor(103, noSpecializationBonus);

        var initial = cache.GetPredationToolsRawScores(species);
        AssertMicrobeOxygenInhibitorToxinScores(initial, 3467572.0f, 3294193.0f);

        species.CellTypeSpecializationBonus = specializedBonus;

        var cached = cache.GetPredationToolsRawScores(species);
        AssertThat(cached.OxygenMetabolismInhibitorScore)
            .IsEqual(initial.OxygenMetabolismInhibitorScore);
        AssertMicrobeOxygenInhibitorToxinScores(cached, 3467572.0f, 3294193.0f);

        cache.Clear();

        var recomputed = cache.GetPredationToolsRawScores(species);
        AssertThat(recomputed.OxygenMetabolismInhibitorScore).IsEqualApprox(
            initial.OxygenMetabolismInhibitorScore * specializedBonus / noSpecializationBonus,
            1.0f);
        AssertMicrobeOxygenInhibitorToxinScores(recomputed, 6935144.0f, 6588386.0f);
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

        var fresh = CreateCache().GetPredationToolsRawScores(species);
        AssertMulticellularRecomputedScores(fresh);

        cache.Clear();

        var recomputed = cache.GetPredationToolsRawScores(species);
        AssertDifferentBits(recomputed.OxytoxyScore, initial.OxytoxyScore);
        AssertDifferentBits(recomputed.SlimeJetScore, initial.SlimeJetScore);
        AssertDifferentBits(recomputed.PullingCiliaModifier, initial.PullingCiliaModifier);
        AssertMulticellularRecomputedScores(recomputed);
        AssertRawBits(recomputed, fresh);
    }

    [TestCase]
    public void MulticellularSlimeJetScoreIgnoresCellsWithoutSlimeJets()
    {
        var cache = CreateCache();
        var simulationParameters = SimulationParameters.Instance;
        var speciesWithoutSupport = CreateSlimeJetSpecies(103,
            (CreateSlimeJetCellType(simulationParameters, "Misaligned", new Hex(4, 0)), new Hex(0, 0)));
        var speciesWithSupport = CreateSlimeJetSpecies(104,
            (CreateSlimeJetCellType(simulationParameters, "Misaligned", new Hex(4, 0)), new Hex(0, 0)),
            (CreateSupportCellType(simulationParameters), new Hex(3, 0)));

        var scoreWithoutSupport = cache.GetPredationToolsRawScores(speciesWithoutSupport);
        var scoreWithSupport = cache.GetPredationToolsRawScores(speciesWithSupport);

        AssertThat(scoreWithoutSupport.SlimeJetScore).IsEqual(0.0f);
        AssertThat(scoreWithSupport.SlimeJetScore).IsEqual(scoreWithoutSupport.SlimeJetScore);

        cache.Clear();

        var recomputed = cache.GetPredationToolsRawScores(speciesWithSupport);
        AssertThat(recomputed.SlimeJetScore).IsEqual(scoreWithSupport.SlimeJetScore);
    }

    [TestCase]
    public void MulticellularSlimeJetScoreIsWeightedByJetContribution()
    {
        var cache = CreateCache();
        var simulationParameters = SimulationParameters.Instance;
        var alignedCellType = CreateSlimeJetCellType(simulationParameters, "Aligned", new Hex(0, 4), new Hex(0, 3));
        var misalignedCellType = CreateSlimeJetCellType(simulationParameters, "Misaligned", new Hex(4, 0));
        var species = CreateSlimeJetSpecies(105,
            (alignedCellType, new Hex(0, 0)),
            (misalignedCellType, new Hex(3, 0)));
        alignedCellType.CellTypeSpecializationBonus = 2.0f;
        misalignedCellType.CellTypeSpecializationBonus = 3.0f;

        var scores = cache.GetPredationToolsRawScores(species);

        AssertThat(scores.SlimeJetScore).IsEqual(Constants.AUTO_EVO_SLIME_JET_SCORE * 4.0f);
    }

    private static MicrobeSpecies CreateMicrobe(uint id)
    {
        var simulationParameters = SimulationParameters.Instance;
        var species = new MicrobeSpecies(id, "Characterization", "RawScoreMicrobe")
        {
            IsBacteria = true,
            MembraneType = simulationParameters.GetMembrane("single"),
        };

        AddPredationToolOrganelles(species.Organelles);
        species.OnEdited();
        species.CellTypeSpecializationBonus = 1.25f;

        return species;
    }

    private static MicrobeSpecies CreateMicrobeWithOxygenMetabolismInhibitor(uint id,
        float specializationBonus)
    {
        var simulationParameters = SimulationParameters.Instance;
        var species = new MicrobeSpecies(id, "Regression", "OxygenInhibitor")
        {
            IsBacteria = true,
            MembraneType = simulationParameters.GetMembrane("single"),
        };

        species.Organelles.Add(CreateOrganelle("cytoplasm", new Hex(0, 0)));
        species.Organelles.Add(CreateToxin(new Hex(-4, 0), ToxinType.Oxytoxy, 0.25f));
        species.Organelles.Add(CreateToxin(new Hex(4, 0), ToxinType.OxygenMetabolismInhibitor, 0.25f));
        species.OnEdited();
        species.CellTypeSpecializationBonus = specializationBonus;

        return species;
    }

    private static (MulticellularSpecies Species, CellType ContributingCellType) CreateMulticellularSpecies(uint id)
    {
        var simulationParameters = SimulationParameters.Instance;
        var contributingCellType = new CellType(simulationParameters.GetMembrane("single"))
        {
            CellTypeName = "PredationTools",
        };
        AddPredationToolOrganelles(contributingCellType.ModifiableOrganelles);

        var supportingCellType = new CellType(simulationParameters.GetMembrane("single"))
        {
            CellTypeName = "Support",
        };
        supportingCellType.ModifiableOrganelles.Add(CreateOrganelle("cytoplasm", new Hex(0, 0)));

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

    private static MulticellularSpecies CreateSlimeJetSpecies(uint id,
        params (CellType CellType, Hex Position)[] cells)
    {
        var species = new MulticellularSpecies(id, "Regression", $"SlimeJet{id}");

        foreach (var (cellType, position) in cells)
        {
            species.ModifiableCellTypes.Add(cellType);
            species.ModifiableGameplayCells.AddFast(new CellTemplate(cellType, position, 0),
                new List<Hex>(), new List<Hex>());
        }

        species.OnEdited();
        return species;
    }

    private static CellType CreateSlimeJetCellType(SimulationParameters simulationParameters, string name,
        params Hex[] slimeJetPositions)
    {
        var cellType = new CellType(simulationParameters.GetMembrane("single"))
        {
            CellTypeName = name,
        };
        cellType.ModifiableOrganelles.Add(CreateOrganelle("cytoplasm", new Hex(0, 0)));

        foreach (var slimeJetPosition in slimeJetPositions)
            cellType.ModifiableOrganelles.Add(CreateOrganelle("slimeJet", slimeJetPosition));

        return cellType;
    }

    private static CellType CreateSupportCellType(SimulationParameters simulationParameters)
    {
        var cellType = new CellType(simulationParameters.GetMembrane("single"))
        {
            CellTypeName = "Support",
        };
        cellType.ModifiableOrganelles.Add(CreateOrganelle("cytoplasm", new Hex(0, 0)));
        return cellType;
    }

    private static void AddPredationToolOrganelles(OrganelleLayout<OrganelleTemplate> organelles)
    {
        organelles.Add(CreateOrganelle("cytoplasm", new Hex(0, 0)));
        organelles.Add(CreateOrganelle("pilus", new Hex(0, -4)));
        organelles.Add(CreateOrganelle("slimeJet", new Hex(0, 4)));
        organelles.Add(CreateOrganelle("cilia", new Hex(4, 0),
            CiliaComponent.CILIA_PULL_UPGRADE_NAME));
        organelles.Add(CreateToxin(new Hex(-4, 0), ToxinType.Oxytoxy, 0.25f));
    }

    private static void AssertMicrobeOxygenInhibitorToxinScores(SimulationCache.PredationToolsRawScores scores,
        float expectedOxytoxyScore,
        float expectedOxygenMetabolismInhibitorScore)
    {
        AssertThat(scores.AverageToxicity).IsEqual(0.25f);
        AssertThat(scores.OxytoxyScore).IsEqual(expectedOxytoxyScore);
        AssertThat(scores.CytotoxinScore).IsEqual(0.0f);
        AssertThat(scores.MacrolideScore).IsEqual(0.0f);
        AssertThat(scores.ChannelInhibitorScore).IsEqual(0.0f);
        AssertThat(scores.OxygenMetabolismInhibitorScore).IsEqual(expectedOxygenMetabolismInhibitorScore);
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
        AssertBits(scores.PilusScore, 5000.0f);
        AssertBits(scores.InjectisomeScore, 0.0f);
        AssertBits(scores.DefensivePilusScore, 0.0f);
        AssertBits(scores.DefensiveInjectisomeScore, 0.0f);
        AssertBits(scores.AverageToxicity, 0.25f);
        AssertBits(scores.OxytoxyScore, 4334465.0f);
        AssertBits(scores.CytotoxinScore, 0.0f);
        AssertBits(scores.MacrolideScore, 0.0f);
        AssertBits(scores.ChannelInhibitorScore, 0.0f);
        AssertBits(scores.OxygenMetabolismInhibitorScore, 0.0f);
        AssertBits(scores.SlimeJetScore, 37.5f);
        AssertBits(scores.MucocystsScore, 0.0f);
        AssertBits(scores.PullingCiliaModifier, 2.25f);
    }

    private static void AssertMicrobeRecomputedScores(SimulationCache.PredationToolsRawScores scores)
    {
        AssertBits(scores.PilusScore, 5000.0f);
        AssertBits(scores.InjectisomeScore, 0.0f);
        AssertBits(scores.DefensivePilusScore, 0.0f);
        AssertBits(scores.DefensiveInjectisomeScore, 0.0f);
        AssertBits(scores.AverageToxicity, 0.25f);
        AssertBits(scores.OxytoxyScore, 6935144.0f);
        AssertBits(scores.CytotoxinScore, 0.0f);
        AssertBits(scores.MacrolideScore, 0.0f);
        AssertBits(scores.ChannelInhibitorScore, 0.0f);
        AssertBits(scores.OxygenMetabolismInhibitorScore, 0.0f);
        AssertBits(scores.SlimeJetScore, 60.0f);
        AssertBits(scores.MucocystsScore, 0.0f);
        AssertBits(scores.PullingCiliaModifier, 3.6f);
    }

    private static void AssertMulticellularInitialScores(SimulationCache.PredationToolsRawScores scores)
    {
        AssertBits(scores.PilusScore, 7071.068f);
        AssertBits(scores.InjectisomeScore, 0.0f);
        AssertBits(scores.DefensivePilusScore, 0.0f);
        AssertBits(scores.DefensiveInjectisomeScore, 0.0f);
        AssertBits(scores.AverageToxicity, 0.25f);
        AssertBits(scores.OxytoxyScore, 10922850.0f);
        AssertBits(scores.CytotoxinScore, 0.0f);
        AssertBits(scores.MacrolideScore, 0.0f);
        AssertBits(scores.ChannelInhibitorScore, 0.0f);
        AssertBits(scores.OxygenMetabolismInhibitorScore, 0.0f);
        AssertBits(scores.SlimeJetScore, 94.49999f);
        AssertBits(scores.MucocystsScore, 0.0f);
        AssertBits(scores.PullingCiliaModifier, 2.4198592f);
    }

    private static void AssertMulticellularRecomputedScores(SimulationCache.PredationToolsRawScores scores)
    {
        AssertBits(scores.PilusScore, 7071.068f);
        AssertBits(scores.InjectisomeScore, 0.0f);
        AssertBits(scores.DefensivePilusScore, 0.0f);
        AssertBits(scores.DefensiveInjectisomeScore, 0.0f);
        AssertBits(scores.AverageToxicity, 0.25f);
        AssertBits(scores.OxytoxyScore, 16384275.0f);
        AssertBits(scores.CytotoxinScore, 0.0f);
        AssertBits(scores.MacrolideScore, 0.0f);
        AssertBits(scores.ChannelInhibitorScore, 0.0f);
        AssertBits(scores.OxygenMetabolismInhibitorScore, 0.0f);
        AssertBits(scores.SlimeJetScore, 141.75f);
        AssertBits(scores.MucocystsScore, 0.0f);
        AssertBits(scores.PullingCiliaModifier, 2.7389653f);
    }
}
