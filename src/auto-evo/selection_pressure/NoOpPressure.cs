namespace AutoEvo;

using SharedBase.Archive;

/// <summary>
///   This pressure does nothing but is used as a placeholder node in the Miche Tree
/// </summary>
public class NoOpPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_NO_OP_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public NoOpPressure() : base(1, [])
    {
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.NoOpPressure;

    public static NoOpPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new NoOpPressure();

        // We don't use weight
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
