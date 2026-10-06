using System;
using System.Collections.Generic;
using AutoEvo;
using static GdUnit4.Assertions;

/// <summary>
///   Small, test-local building blocks for public SimulationCache observations.
/// </summary>
internal static class SimulationCacheTestFixtures
{
    internal static SimulationCache CreateCache()
    {
        return new SimulationCache(new WorldGenerationSettings { Seed = 1 });
    }

    internal static BiomeConditions CreateBiome()
    {
        return (BiomeConditions)SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions.Clone();
    }

    internal static OrganelleTemplate CreateOrganelle(string name, Hex position, string? upgrade = null)
    {
        var organelle = new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType(name), position, 0);
        if (upgrade != null)
            organelle.ModifiableUpgrades = new OrganelleUpgrades { ModifiableUnlockedFeatures = [upgrade] };

        return organelle;
    }

    internal static OrganelleTemplate CreateToxin(Hex position, ToxinType type, float toxicity = 0)
    {
        var organelle = CreateOrganelle("oxytoxy", position, ToxinUpgradeNames.ToxinNameFromType(type));
        organelle.ModifiableUpgrades!.CustomUpgradeData = new ToxinUpgrades(type, toxicity);
        return organelle;
    }

    internal static MicrobeSpecies CreateMicrobe(uint id, string epithet, string membrane,
        params string[] organelles)
    {
        var species = new MicrobeSpecies(id, "Characterization", epithet)
        {
            IsBacteria = true,
            MembraneType = SimulationParameters.Instance.GetMembrane(membrane),
        };

        for (var i = 0; i < organelles.Length; ++i)
            species.Organelles.Add(CreateOrganelle(organelles[i], new Hex(i * 4, 0)));

        species.OnEdited();
        return species;
    }

    internal static CellType CreateCellType(string name, string membrane, params OrganelleTemplate[] organelles)
    {
        var cellType = new CellType(SimulationParameters.Instance.GetMembrane(membrane)) { CellTypeName = name };
        foreach (var organelle in organelles)
            cellType.ModifiableOrganelles.Add(organelle);

        return cellType;
    }

    internal static MulticellularSpecies CreateMulticellular(uint id, string epithet,
        params (CellType CellType, Hex Position)[] cells)
    {
        var species = new MulticellularSpecies(id, "Characterization", epithet);
        foreach (var (cellType, position) in cells)
        {
            if (!species.ModifiableCellTypes.Contains(cellType))
                species.ModifiableCellTypes.Add(cellType);

            species.ModifiableGameplayCells.AddFast(new CellTemplate(cellType, position, 0),
                new List<Hex>(), new List<Hex>());
        }

        species.OnEdited();
        return species;
    }

    internal static void AssertBits(float actual, float expected)
    {
        AssertThat(BitConverter.SingleToInt32Bits(actual)).IsEqual(BitConverter.SingleToInt32Bits(expected));
    }

    internal static void AssertDifferentBits(float actual, float other)
    {
        AssertThat(BitConverter.SingleToInt32Bits(actual)).IsNotEqual(BitConverter.SingleToInt32Bits(other));
    }

    internal static void AssertPredationLifecycle(Species predator, Species prey, float expected,
        BiomeConditions? biome = null)
    {
        var cache = CreateCache();
        biome ??= CreateBiome();
        AssertBits(cache.GetPredationScore(predator, prey, biome), expected);
        AssertBits(cache.GetPredationScore(predator, prey, biome), expected);
        cache.Clear();
        AssertBits(cache.GetPredationScore(predator, prey, biome), expected);
    }

    internal static void AssertRawBits(SimulationCache.PredationToolsRawScores actual,
        SimulationCache.PredationToolsRawScores expected)
    {
        AssertBits(actual.PilusScore, expected.PilusScore);
        AssertBits(actual.InjectisomeScore, expected.InjectisomeScore);
        AssertBits(actual.DefensivePilusScore, expected.DefensivePilusScore);
        AssertBits(actual.DefensiveInjectisomeScore, expected.DefensiveInjectisomeScore);
        AssertBits(actual.AverageToxicity, expected.AverageToxicity);
        AssertBits(actual.OxytoxyScore, expected.OxytoxyScore);
        AssertBits(actual.CytotoxinScore, expected.CytotoxinScore);
        AssertBits(actual.MacrolideScore, expected.MacrolideScore);
        AssertBits(actual.ChannelInhibitorScore, expected.ChannelInhibitorScore);
        AssertBits(actual.OxygenMetabolismInhibitorScore, expected.OxygenMetabolismInhibitorScore);
        AssertBits(actual.SlimeJetScore, expected.SlimeJetScore);
        AssertBits(actual.MucocystsScore, expected.MucocystsScore);
        AssertBits(actual.PullingCiliaModifier, expected.PullingCiliaModifier);
    }
}
