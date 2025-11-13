using System.IO;
using GdUnit4;
using Godot;
using Saving.Serializers;
using SharedBase.Archive;
using static GdUnit4.Assertions;

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
        const ulong desiredHash = 15688549965584827573;
        const ulong wantedHexesHash = 10218345136693314111;

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
        const ulong desiredHash1 = 15688549965584827573;
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
}
