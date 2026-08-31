using System;
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

        AssertFloatBits(score, 0x00000000);
    }

    [TestCase]
    public void EngulfingPredatorScoreIsCharacterized()
    {
        var predator = CreateMicrobe(3, "Engulfer", "single",
            "cytoplasm", "cytoplasm", "cytoplasm", "cytoplasm");
        var prey = CreateMicrobe(4, "Engulfed", "single", "cytoplasm");

        var score = CalculatePredationScore(predator, prey);

        AssertFloatBits(score, 0x4388D710);
    }

    [TestCase]
    public void PilusAndToxinPredatorScoreIsCharacterized()
    {
        var predator = CreateMicrobe(5, "Armed", "cellulose", "cytoplasm", "pilus", "oxytoxy");
        var prey = CreateMicrobe(6, "ArmouredPrey", "cellulose", "cytoplasm");

        var score = CalculatePredationScore(predator, prey);

        AssertFloatBits(score, 0x3E303F10);
    }

    [TestCase]
    public void MulticellularPredatorScoreIsCharacterized()
    {
        var predator = CreateMulticellularSpecies(7, "MulticellularPredator", "single",
            "cytoplasm", "cytoplasm", "cytoplasm", "cytoplasm");
        var prey = CreateMicrobe(8, "MulticellularPrey", "single", "cytoplasm");

        var score = CalculatePredationScore(predator, prey);

        AssertFloatBits(score, 0x438135F7);
    }

    [TestCase]
    public void MicrobePredatorAgainstMulticellularPreyScoreIsCharacterized()
    {
        var predator = CreateMicrobe(9, "MicrobePredator", "single",
            "cytoplasm", "cytoplasm", "cytoplasm", "cytoplasm");
        var prey = CreateMulticellularSpecies(10, "MulticellularPrey", "single", "cytoplasm");

        var score = CalculatePredationScore(predator, prey);

        AssertFloatBits(score, 0x446C238F);
    }

    [TestCase]
    public void MulticellularPredatorAgainstMulticellularPreyScoreIsCharacterized()
    {
        var predator = CreateMulticellularSpecies(11, "MulticellularPredator", "single",
            "cytoplasm", "cytoplasm", "cytoplasm", "cytoplasm");
        var prey = CreateMulticellularSpecies(12, "MulticellularPrey", "single", "cytoplasm");

        var score = CalculatePredationScore(predator, prey);

        AssertFloatBits(score, 0x43480050);
    }

    [TestCase]
    public void SameSupportedSpeciesScoresPositiveZero()
    {
        var species = CreateMicrobe(13, "Self", "single", "cytoplasm");

        var score = CalculatePredationScore(species, species);

        AssertFloatBits(score, 0x00000000);
    }

    [TestCase]
    public void UnsupportedPredatorThrowsBeforeScoring()
    {
        var cache = CreateCache();
        var predator = new MacroscopicSpecies(14, "Characterization", "UnsupportedPredator");
        var prey = CreateMicrobe(15, "SupportedPrey", "single", "cytoplasm");

        AssertThrown(() => cache.GetPredationScore(predator, prey, GetVolcanicVentBiome()))
            .IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    public void UnsupportedPreyThrowsBeforeScoring()
    {
        var cache = CreateCache();
        var predator = CreateMicrobe(16, "SupportedPredator", "single", "cytoplasm");
        var prey = new MacroscopicSpecies(17, "Characterization", "UnsupportedPrey");

        AssertThrown(() => cache.GetPredationScore(predator, prey, GetVolcanicVentBiome()))
            .IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    public void UnsupportedSelfThrowsBeforeSelfShortcut()
    {
        var cache = CreateCache();
        var species = new MacroscopicSpecies(22, "Characterization", "UnsupportedSelf");

        AssertThrown(() => cache.GetPredationScore(species, species, GetVolcanicVentBiome()))
            .IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    public void OxygenAvailabilityAndBiomeKeyAreCharacterized()
    {
        var cache = CreateCache();
        var predator = CreateMicrobe(18, "OxygenSensitivePredator", "cellulose",
            "cytoplasm", "pilus", "oxytoxy");
        var prey = CreateMicrobe(19, "OxygenSensitivePrey", "cellulose", "cytoplasm");
        var oxytoxy = predator.Organelles.Organelles[2];
        oxytoxy.ModifiableUpgrades = new OrganelleUpgrades
        {
            ModifiableUnlockedFeatures = [ToxinUpgradeNames.OXYTOXY_UPGRADE_NAME],
            CustomUpgradeData = new ToxinUpgrades(ToxinType.Oxytoxy, 0.25f),
        };
        predator.OnEdited();
        var oxygenatedBiome = CreateVolcanicVentBiome(0.2f);
        var oxygenFreeBiome = CreateVolcanicVentBiome(0.0f);

        var oxygenatedScore = cache.GetPredationScore(predator, prey, oxygenatedBiome);
        var oxygenFreeScore = cache.GetPredationScore(predator, prey, oxygenFreeBiome);
        var cachedOxygenatedScore = cache.GetPredationScore(predator, prey, oxygenatedBiome);

        AssertFloatBits(oxygenatedScore, 0x414D3CD4);
        AssertFloatBits(oxygenFreeScore, 0x413FE7BB);
        AssertFloatBits(cachedOxygenatedScore, 0x414D3CD4);
    }

    [TestCase]
    public void FinalScoreCacheStaysStaleUntilClear()
    {
        var cache = CreateCache();
        var predator = CreateMicrobe(20, "CachedPredator", "single",
            "cytoplasm", "cytoplasm", "cytoplasm", "cytoplasm");
        var prey = CreateMicrobe(21, "CachedPrey", "single", "cytoplasm");
        var biome = GetVolcanicVentBiome();

        var initial = cache.GetPredationScore(predator, prey, biome);
        AssertFloatBits(initial, 0x4388D710);

        predator.ModifiableBehaviour.Aggression = Constants.MAX_SPECIES_AGGRESSION;

        var cached = cache.GetPredationScore(predator, prey, biome);
        var freshCacheOracle = CreateCache().GetPredationScore(predator, prey, biome);

        AssertFloatBits(cached, BitConverter.SingleToInt32Bits(initial));
        AssertThat(BitConverter.SingleToInt32Bits(freshCacheOracle))
            .IsNotEqual(BitConverter.SingleToInt32Bits(initial));

        cache.Clear();

        var recomputed = cache.GetPredationScore(predator, prey, biome);
        AssertFloatBits(recomputed, BitConverter.SingleToInt32Bits(freshCacheOracle));
    }

    private static float CalculatePredationScore(Species predator, Species prey)
    {
        var cache = CreateCache();
        var biome = GetVolcanicVentBiome();

        return cache.GetPredationScore(predator, prey, biome);
    }

    private static SimulationCache CreateCache()
    {
        return new SimulationCache(new WorldGenerationSettings
        {
            Seed = 1,
        });
    }

    private static BiomeConditions GetVolcanicVentBiome()
    {
        return SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions;
    }

    private static BiomeConditions CreateVolcanicVentBiome(float ambientOxygen)
    {
        var biome = (BiomeConditions)GetVolcanicVentBiome().Clone();
        var oxygen = biome.ChangeableCompounds[Compound.Oxygen];
        oxygen.Ambient = ambientOxygen;
        biome.ChangeableCompounds[Compound.Oxygen] = oxygen;

        return biome;
    }

    private static void AssertFloatBits(float actual, int expectedBits)
    {
        AssertThat(BitConverter.SingleToInt32Bits(actual)).IsEqual(expectedBits);
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

    private static MulticellularSpecies CreateMulticellularSpecies(uint id, string epithet, string membrane,
        params string[] organelles)
    {
        var simulationParameters = SimulationParameters.Instance;
        var cellType = new CellType(simulationParameters.GetMembrane(membrane))
        {
            CellTypeName = epithet,
        };

        for (var i = 0; i < organelles.Length; ++i)
        {
            cellType.ModifiableOrganelles.Add(new OrganelleTemplate(
                simulationParameters.GetOrganelleType(organelles[i]), new Hex(i * 4, 0), 0));
        }

        var species = new MulticellularSpecies(id, "Characterization", epithet);
        species.ModifiableCellTypes.Add(cellType);
        species.ModifiableGameplayCells.AddFast(new CellTemplate(cellType, new Hex(0, 0), 0),
            new List<Hex>(), new List<Hex>());
        species.OnEdited();

        return species;
    }
}
