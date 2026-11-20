using System.Collections.Generic;
using System.IO;
using GdUnit4;
using Godot;
using Saving.Serializers;
using SharedBase.Archive;
using static GdUnit4.Assertions;
using static SimulationCacheTestFixtures;

[TestSuite]
[RequireGodotRuntime]
public class MicrobeSpeciesTests
{
    private readonly MicrobeSpecies testSpecies1;
    private readonly MicrobeSpecies testSpecies2;

    private readonly ThriveArchiveManager archiveManager;

    public MicrobeSpeciesTests()
    {
        archiveManager = new ThriveArchiveManager();

        testSpecies1 = new MicrobeSpecies(1, "Cyiyes", "Thrive")
        {
            IsBacteria = true,
            MembraneType = SimulationParameters.Instance.GetMembrane("double"),
            MembraneRigidity = 0.125f,
            SpeciesColour = new Color(1, 1, 0, 1),
            Population = 120,
            Generation = 2,
        };

        testSpecies1.BecomePlayerSpecies();
        testSpecies1.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"),
            new Hex(0, 0), 0));
        testSpecies1.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("rusticyanin"),
            new Hex(1, 0), 0));
        testSpecies1.Organelles.Add(new OrganelleTemplate(
            SimulationParameters.Instance.GetOrganelleType("chromatophore"),
            new Hex(1, 1), 0));
        testSpecies1.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("nitrogenase"),
            new Hex(0, 1), 0));
        testSpecies1.Organelles.Add(
            new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"), new Hex(2, 1), 3)
            {
                ModifiableUpgrades = new OrganelleUpgrades
                {
                    CustomUpgradeData = new FlagellumUpgrades(0.5f),
                },
            });
        testSpecies1.OnEdited();

        testSpecies2 = new MicrobeSpecies(2, "Test", "Thrive")
        {
            IsBacteria = true,
            MembraneType = SimulationParameters.Instance.GetMembrane("single"),
            MembraneRigidity = 0,
            SpeciesColour = new Color(0, 1, 0, 1),
            Population = 1044,
        };

        testSpecies2.BecomePlayerSpecies();
        testSpecies2.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"),
            new Hex(0, 0), 0));
        testSpecies2.OnEdited();
    }

    [TestCase]
    public void DeserializedHashIsConsistent()
    {
        const ulong desiredHash = 1311834546291663092;
        const ulong wantedHexesHash = 5062790629652194942;

        AssertThat(desiredHash).IsNotEqual(wantedHexesHash);

        var memoryStream = new MemoryStream();
        var writer = new SArchiveMemoryWriter(memoryStream, archiveManager);
        var reader = new SArchiveMemoryReader(memoryStream, archiveManager);

        archiveManager.OnStartNewWrite(writer);
        writer.WriteObject(testSpecies1);
        archiveManager.OnFinishWrite(writer);

        archiveManager.OnStartNewRead(reader);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var species = reader.ReadObjectOrNull<MicrobeSpecies>();
        archiveManager.OnFinishRead(reader);

        AssertThat(species).IsNotNull();

        AssertThat(species!.GetVisualHashCode()).IsEqual(desiredHash);

        var hexesHash = CellHexesPhotoBuilder.GetVisualHash(species);

        AssertThat(hexesHash).IsNotEqual(desiredHash).IsNotEqual(species.GetVisualHashCode())
            .IsEqual(wantedHexesHash);

        // Check that the result has not changed
        AssertThat(species.GetVisualHashCode()).IsEqual(desiredHash);
    }

    [TestCase]
    public void SpecificSpeciesHashIsConsistent()
    {
        const ulong desiredHash1 = 1311834546291663092;
        const ulong desiredHash2 = 3786447046140225937;

        var memoryStream = new MemoryStream();
        var writer = new SArchiveMemoryWriter(memoryStream, archiveManager);
        var reader = new SArchiveMemoryReader(memoryStream, archiveManager);

        archiveManager.OnStartNewWrite(writer);
        writer.WriteObject(testSpecies1);
        writer.WriteObject(testSpecies2);
        archiveManager.OnFinishWrite(writer);

        archiveManager.OnStartNewRead(reader);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var species1 = reader.ReadObjectOrNull<MicrobeSpecies>();
        var species2 = reader.ReadObjectOrNull<MicrobeSpecies>();
        archiveManager.OnFinishRead(reader);

        AssertThat(species1).IsNotNull();
        AssertThat(species2).IsNotNull();

        // Check that the result has not changed
        AssertThat(species1!.GetVisualHashCode()).IsEqual(desiredHash1);
        AssertThat(species2!.GetVisualHashCode()).IsEqual(desiredHash2);
    }

    /// <summary>
    ///   Adding and specialising a vacuole changes the capacity available for each compound.
    ///   Once the last cytoplasm is removed, a glucose-specific vacuole provides no general storage.
    /// </summary>
    [TestCase]
    public void VacuoleSpecializationUpdatesCompoundCapacities()
    {
        var species = CreateMicrobe(37, "StorageSpecialization", "single", "cytoplasm");
        var cytoplasm = species.Organelles.Organelles[0];
        species.CellTypeSpecializationBonus = 1;

        // Derive expected capacities from balance data without using the capacity calculation helpers.
        var parameters = SimulationParameters.Instance;
        var cytoplasmCapacity = parameters.GetOrganelleType("cytoplasm").Components.Storage!.Capacity;
        var vacuoleCapacity = parameters.GetOrganelleType("vacuole").Components.Storage!.Capacity;
        var ordinaryCapacity = cytoplasmCapacity + vacuoleCapacity;
        var specializedCapacity = vacuoleCapacity * Constants.VACUOLE_SPECIALIZED_MULTIPLIER;
        var specializedTotalCapacity = cytoplasmCapacity + specializedCapacity;

        var capacity = species.StorageCapacities;
        AssertBits(species.NominalStorageCapacity, cytoplasmCapacity);
        AssertBits(capacity.Nominal, cytoplasmCapacity);
        AssertThat(capacity.Specific).IsEmpty();
        AssertBits(capacity.Nominal + capacity.Specific.GetValueOrDefault(Compound.Ammonia), cytoplasmCapacity);
        AssertBits(capacity.Nominal + capacity.Specific.GetValueOrDefault(Compound.Glucose), cytoplasmCapacity);

        var vacuole = CreateOrganelle("vacuole", new Hex(1, 0));
        species.Organelles.Add(vacuole);
        capacity = species.StorageCapacities;
        AssertBits(species.NominalStorageCapacity, ordinaryCapacity);
        AssertBits(capacity.Nominal, ordinaryCapacity);
        AssertThat(capacity.Specific).IsEmpty();
        AssertBits(capacity.Nominal + capacity.Specific.GetValueOrDefault(Compound.Ammonia), ordinaryCapacity);
        AssertBits(capacity.Nominal + capacity.Specific.GetValueOrDefault(Compound.Glucose), ordinaryCapacity);

        Specialize(vacuole, Compound.Ammonia);
        capacity = species.StorageCapacities;
        AssertBits(species.NominalStorageCapacity, cytoplasmCapacity);
        AssertBits(capacity.Nominal, cytoplasmCapacity);
        AssertCompounds(capacity.Specific,
            new Dictionary<Compound, float> { { Compound.Ammonia, specializedCapacity } });
        AssertBits(capacity.Nominal + capacity.Specific.GetValueOrDefault(Compound.Ammonia), specializedTotalCapacity);
        AssertBits(capacity.Nominal + capacity.Specific.GetValueOrDefault(Compound.Glucose), cytoplasmCapacity);

        var upgrades = (StorageComponentUpgrades)vacuole.ModifiableUpgrades!.CustomUpgradeData!;
        upgrades.SpecializedFor = Compound.Glucose;
        capacity = species.StorageCapacities;
        AssertBits(species.NominalStorageCapacity, cytoplasmCapacity);
        AssertBits(capacity.Nominal, cytoplasmCapacity);
        AssertCompounds(capacity.Specific,
            new Dictionary<Compound, float> { { Compound.Glucose, specializedCapacity } });
        AssertBits(capacity.Nominal + capacity.Specific.GetValueOrDefault(Compound.Ammonia), cytoplasmCapacity);
        AssertBits(capacity.Nominal + capacity.Specific.GetValueOrDefault(Compound.Glucose), specializedTotalCapacity);

        species.Organelles.Remove(cytoplasm);
        AssertBits(species.NominalStorageCapacity, 0);
    }

    /// <summary>
    ///   Verifies that direct organelle, upgrade, and bonus changes are visible without edit notifications.
    /// </summary>
    [TestCase]
    public void NominalCapacityImmediatelyObservesDirectChanges()
    {
        var species = CreateMicrobe(33, "ChangingStorage", "single", "cytoplasm", "vacuole");
        var vacuole = species.Organelles.Organelles[1];
        var original = species.NominalStorageCapacity;
        Specialize(vacuole, Compound.Glucose);
        AssertThat(species.NominalStorageCapacity).IsLess(original);
        AssertCurrentCapacity(species);

        var upgrades = (StorageComponentUpgrades)vacuole.ModifiableUpgrades!.CustomUpgradeData!;
        upgrades.SpecializedFor = Compound.Ammonia;
        AssertCurrentCapacity(species);
        AssertThat(species.StorageCapacities.Specific.ContainsKey(Compound.Ammonia)).IsTrue();
        upgrades.SpecializedFor = Compound.Invalid;
        AssertBits(species.NominalStorageCapacity, original);

        species.CellTypeSpecializationBonus = 1.375f;
        AssertCurrentCapacity(species);
        var beforeAddition = species.NominalStorageCapacity;
        var added = CreateOrganelle("vacuole", new Hex(12, 0));
        species.Organelles.Add(added);
        AssertThat(species.NominalStorageCapacity).IsGreater(beforeAddition);
        AssertCurrentCapacity(species);
        species.Organelles.Remove(added);
        AssertBits(species.NominalStorageCapacity, beforeAddition);
        AssertCurrentCapacity(species);

        Specialize(vacuole, Compound.Glucose);
        vacuole.ModifiableUpgrades = null;
        AssertCurrentCapacity(species);
        AssertBits(species.NominalStorageCapacity, beforeAddition);
    }

    /// <summary>
    ///   Clearing one returned storage dictionary leaves the earlier and subsequent reads intact.
    ///   Both specialised capacities and the initial compounds survive an archive round trip and reinitialisation.
    /// </summary>
    [TestCase]
    public void SpecializedStorageAndInitialCompoundsSurviveSerialization()
    {
        var species = CreateMicrobe(36, "SavedStorage", "single", "cytoplasm", "vacuole", "vacuole");
        Specialize(species.Organelles.Organelles[1], Compound.Glucose);
        Specialize(species.Organelles.Organelles[2], Compound.Ammonia);
        species.OnEdited();
        var capacity = species.StorageCapacities;
        var expectedSpecific = new Dictionary<Compound, float>
        {
            { Compound.Glucose, capacity.Specific[Compound.Glucose] },
            { Compound.Ammonia, capacity.Specific[Compound.Ammonia] },
        };
        species.StorageCapacities.Specific.Clear();
        AssertCompounds(capacity.Specific, expectedSpecific);
        AssertCompounds(species.StorageCapacities.Specific, expectedSpecific);

        var initial = new Dictionary<Compound, float>(species.InitialCompounds);
        var memoryStream = new MemoryStream();
        var writer = new SArchiveMemoryWriter(memoryStream, archiveManager);
        var reader = new SArchiveMemoryReader(memoryStream, archiveManager);
        archiveManager.OnStartNewWrite(writer);
        writer.WriteObject(species);
        archiveManager.OnFinishWrite(writer);

        archiveManager.OnStartNewRead(reader);
        memoryStream.Position = 0;
        var loaded = reader.ReadObjectOrNull<MicrobeSpecies>()!;
        archiveManager.OnFinishRead(reader);
        AssertThat(loaded).IsNotNull();
        AssertBits(loaded.NominalStorageCapacity, capacity.Nominal);
        AssertCompounds(loaded.StorageCapacities.Specific, capacity.Specific);
        AssertCompounds(loaded.InitialCompounds, initial);
        AssertCurrentCapacity(loaded);
        loaded.UpdateInitialCompounds();
        AssertCompounds(loaded.InitialCompounds, initial);
    }

    /// <summary>
    ///   Replaces the organelle upgrades with storage specialization for the requested compound.
    /// </summary>
    private static void Specialize(OrganelleTemplate organelle, Compound compound)
    {
        organelle.ModifiableUpgrades = new OrganelleUpgrades
        {
            CustomUpgradeData = new StorageComponentUpgrades(compound),
        };
    }

    /// <summary>
    ///   Checks bitwise agreement between the nominal-only and complete capacity getters.
    /// </summary>
    private static void AssertCurrentCapacity(MicrobeSpecies species)
    {
        AssertBits(species.NominalStorageCapacity, species.StorageCapacities.Nominal);
    }

    /// <summary>
    ///   Checks the complete compound key set and bitwise equality of every expected value.
    /// </summary>
    private static void AssertCompounds(Dictionary<Compound, float> actual, Dictionary<Compound, float> expected)
    {
        AssertThat(actual.Count).IsEqual(expected.Count);
        foreach (var (compound, value) in expected)
        {
            AssertThat(actual.ContainsKey(compound)).IsTrue();
            AssertBits(actual[compound], value);
        }
    }
}
