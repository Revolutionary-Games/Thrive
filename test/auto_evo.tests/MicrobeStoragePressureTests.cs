using System;
using System.Collections.Generic;
using AutoEvo;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
using static SimulationCacheTestFixtures;

/// <summary>
///   Storage-dependent scores through the public pressure and predation entrypoints.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class MicrobeStoragePressureTests
{
    [TestCase(0, 1049475463, 1066034994, 1102323773)]
    [TestCase(1, 1041636084, 1057378690, 1093599037)]
    [TestCase(2, 1023134050, 1022469912, 1059252468)]
    public void StoragePressureScoresRemainConsistent(int specialization, int chunkBits, int cloudBits,
        int reproductionBits)
    {
        var species = CreateStorageSpecies(40, specialization);
        var biome = SimulationParameters.Instance.GetBiome("aavolcanic_vent");
        var patch = new Patch(new LocalizedString("Storage test"), 1, biome, BiomeType.Vents,
            new PatchRegion(1, "Storage region", PatchRegion.RegionType.Sea, Vector2.Zero), 1);
        string? chunkName = null;
        foreach (var (name, chunk) in patch.Biome.Chunks)
        {
            if (chunk.Compounds?.ContainsKey(Compound.Glucose) == true)
            {
                chunkName = name;
                break;
            }
        }

        AssertThat(chunkName).IsNotNull();
        var chunkPressure = new ChunkCompoundPressure(chunkName!, new LocalizedString("Chunk"),
            Compound.Glucose, Compound.ATP, false, 1);
        var cloudPressure = new CompoundCloudPressure(Compound.Glucose, Compound.ATP, false, 1);
        var reproductionPressure = new ReproductionCompoundPressure(Compound.Ammonia, false, 1);
        var chunkScore = chunkPressure.Score(species, patch, CreateCache());
        var cloudScore = cloudPressure.Score(species, patch, CreateCache());
        var reproductionScore = reproductionPressure.Score(species, patch, CreateCache());
        AssertThat(float.IsFinite(chunkScore) && chunkScore > 0).IsTrue();
        AssertThat(float.IsFinite(cloudScore) && cloudScore > 0).IsTrue();
        AssertThat(float.IsFinite(reproductionScore) && reproductionScore > 0).IsTrue();
        AssertThat(BitConverter.SingleToInt32Bits(chunkScore)).IsEqual(chunkBits);
        AssertThat(BitConverter.SingleToInt32Bits(cloudScore)).IsEqual(cloudBits);
        AssertThat(BitConverter.SingleToInt32Bits(reproductionScore)).IsEqual(reproductionBits);
    }

    [TestCase(0, 1101293455, 1062016911)]
    [TestCase(1, 1101293455, 1062016911)]
    [TestCase(2, 1000514235, 1085952532)]
    public void StoragePredationScoresRemainConsistent(int specialization, int predatorBits, int preyBits)
    {
        // Toxins exercise storage on both sides; fresh caches force data collection.
        var storage = CreateStorageSpecies(41, specialization);
        var other = CreateMicrobe(42, "OtherPredator", "single", "cytoplasm", "pilus", "oxytoxy");
        var weakPrey = CreateMicrobe(43, "WeakPrey", "cellulose", "cytoplasm");
        storage.ModifiableBehaviour.Fear = 0;
        storage.ModifiableBehaviour.Aggression = Constants.MAX_SPECIES_AGGRESSION * 0.1f;
        var biome = CreateBiome();
        var asPredator = CreateCache().GetPredationScore(storage, weakPrey, biome);
        var asPrey = CreateCache().GetPredationScore(other, storage, biome);
        AssertThat(float.IsFinite(asPredator) && asPredator > 0).IsTrue();
        AssertThat(float.IsFinite(asPrey) && asPrey > 0).IsTrue();
        AssertThat(BitConverter.SingleToInt32Bits(asPredator)).IsEqual(predatorBits);
        AssertThat(BitConverter.SingleToInt32Bits(asPrey)).IsEqual(preyBits);
    }

    private static MicrobeSpecies CreateStorageSpecies(uint id, int specialization)
    {
        var species = CreateMicrobe(id, "StorageScores", "single", "cytoplasm");
        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();
        var hexCache = new HashSet<Hex>();
        foreach (var name in new[] { "pilus", "oxytoxy", "vacuole", "vacuole", "vacuole" })
        {
            species.Organelles.FindAndPlaceAtValidPosition(CreateOrganelle(name, new Hex(0, 0)), 0, 0,
                workMemory1, workMemory2, hexCache);
        }

        if (specialization > 0)
        {
            species.Organelles.Organelles[3].ModifiableUpgrades = new OrganelleUpgrades
            {
                CustomUpgradeData = new StorageComponentUpgrades(Compound.Glucose),
            };
            species.Organelles.Organelles[4].ModifiableUpgrades = new OrganelleUpgrades
            {
                CustomUpgradeData = new StorageComponentUpgrades(Compound.Ammonia),
            };
        }

        if (specialization == 2)
        {
            foreach (var organelle in species.Organelles)
            {
                if (organelle.Definition.Components.Storage != null && organelle.ModifiableUpgrades == null)
                {
                    organelle.ModifiableUpgrades = new OrganelleUpgrades
                    {
                        CustomUpgradeData = new StorageComponentUpgrades(Compound.Phosphates),
                    };
                }
            }
        }

        species.OnEdited();
        return species;
    }
}
