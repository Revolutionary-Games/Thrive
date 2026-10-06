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
    ///   Checks exact nominal and compound capacities when adding a vacuole, specializing it for ammonia,
    ///   and changing the same upgrade to glucose.
    /// </summary>
    [TestCase]
    public void VacuoleSpecializationUpdatesCompoundCapacities()
    {
        var species = CreateMicrobe(37, "StorageSpecialization", "single", "cytoplasm");
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
    }

    /// <summary>
    ///   Compares the nominal-only and complete capacity results for absent, ordinary, and specialized storage.
    /// </summary>
    [TestCase]
    public void NominalCapacityMatchesCompleteCapacityAcrossStorageLayouts()
    {
        var noStorage = CreateMicrobe(30, "NoStorage", "single", "flagellum");
        AssertBits(noStorage.NominalStorageCapacity, 0);
        AssertCurrentCapacity(noStorage);

        var ordinary = CreateMicrobe(31, "OrdinaryStorage", "single", "cytoplasm", "vacuole");
        AssertThat(ordinary.StorageCapacities.Specific).IsEmpty();
        AssertThat(ordinary.NominalStorageCapacity).IsGreater(0);
        AssertCurrentCapacity(ordinary);

        var specialized = CreateMicrobe(32, "SpecializedStorage", "single", "vacuole", "vacuole");
        Specialize(specialized.Organelles.Organelles[0], Compound.Glucose);
        Specialize(specialized.Organelles.Organelles[1], Compound.Ammonia);
        AssertBits(specialized.NominalStorageCapacity, 0);
        AssertThat(specialized.StorageCapacities.Specific.Count).IsEqual(2);
        AssertCurrentCapacity(specialized);

        specialized.Organelles.Add(CreateOrganelle("cytoplasm", new Hex(12, 0)));
        specialized.Organelles.Add(CreateOrganelle("vacuole", new Hex(16, 0)));
        AssertThat(specialized.NominalStorageCapacity).IsGreater(0);
        AssertCurrentCapacity(specialized);
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
    ///   Checks dictionary independence and preserves capacities and initial compounds across lifecycle changes.
    /// </summary>
    [TestCase]
    public void StorageResultsRemainIndependentAcrossReadsAndLifecycleChanges()
    {
        var species = CreateMicrobe(34, "OwnedStorage", "single", "cytoplasm", "vacuole", "vacuole");
        Specialize(species.Organelles.Organelles[1], Compound.Glucose);
        Specialize(species.Organelles.Organelles[2], Compound.Ammonia);
        species.OnEdited();

        var original = species.StorageCapacities;
        var another = species.StorageCapacities;
        AssertThat(ReferenceEquals(original.Specific, another.Specific)).IsFalse();
        AssertCompounds(another.Specific, original.Specific);
        another.Specific.Clear();
        another.Specific[Compound.Iron] = 123;
        AssertCompounds(species.StorageCapacities.Specific, original.Specific);

        var initial = new Dictionary<Compound, float>(species.InitialCompounds);
        var clone = (MicrobeSpecies)species.Clone();
        AssertBits(clone.NominalStorageCapacity, species.NominalStorageCapacity);
        AssertCompounds(clone.StorageCapacities.Specific, original.Specific);
        AssertCompounds(clone.InitialCompounds, initial);
        AssertCurrentCapacity(clone);

        var target = CreateMicrobe(35, "MutationTarget", "single", "cytoplasm");
        target.ApplyMutation(species);
        AssertBits(target.NominalStorageCapacity, species.NominalStorageCapacity);
        AssertCompounds(target.StorageCapacities.Specific, original.Specific);
        AssertCompounds(target.InitialCompounds, initial);
        AssertCurrentCapacity(target);
        target.UpdateInitialCompounds();
        AssertCompounds(target.InitialCompounds, initial);

        // Reading nominal capacity cannot alter initialization or a previously obtained complete result.
        _ = species.NominalStorageCapacity;
        AssertCompounds(species.InitialCompounds, initial);
        AssertCompounds(species.StorageCapacities.Specific, original.Specific);
        species.OnAttemptedInAutoEvo(true);
        AssertCurrentCapacity(species);
        AssertCompounds(species.InitialCompounds, initial);
    }

    /// <summary>
    ///   Preserves specialized capacities and initial compounds through an archive round trip and reinitialization.
    /// </summary>
    [TestCase]
    public void SpecializedStorageAndInitialCompoundsSurviveSerialization()
    {
        var species = CreateMicrobe(36, "SavedStorage", "single", "cytoplasm", "vacuole", "vacuole");
        Specialize(species.Organelles.Organelles[1], Compound.Glucose);
        Specialize(species.Organelles.Organelles[2], Compound.Ammonia);
        species.OnEdited();
        var capacity = species.StorageCapacities;
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
