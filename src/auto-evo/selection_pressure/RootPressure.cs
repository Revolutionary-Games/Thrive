namespace AutoEvo;

using SharedBase.Archive;

public class RootPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_ROOT_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public RootPressure() : base(1, [
        new RemoveOrganelle(_ => true),
        new AddOrganelleAnywhere(_ => true),
        new AddOrganelleAnywhere(organelle => organelle.InternalName == "nucleus"),
    ])
    {
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.RootPressure;

    public static RootPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new RootPressure();

        // Weight is not used
        reader.ReadFloat();

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        return 1;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
