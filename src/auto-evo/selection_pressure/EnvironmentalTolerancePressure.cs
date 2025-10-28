namespace AutoEvo;

using SharedBase.Archive;

/// <summary>
///   Makes sure species need to be adapted well enough to the environmental conditions in their patch to survive. Also
///   has the part to generate mutations to better match the environment.
/// </summary>
public class EnvironmentalTolerancePressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_ENVIRONMENTAL_TOLERANCE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public EnvironmentalTolerancePressure(float weight) : base(weight, [new ModifyEnvironmentalTolerance()])
    {
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.EnvironmentalTolerancePressure;

    public static EnvironmentalTolerancePressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new EnvironmentalTolerancePressure(reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        // Use scores to encourage species to be adapted to their environment
        return MicrobeEnvironmentalToleranceCalculations.CalculateTotalToleranceScore(microbeSpecies, patch.Biome);
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
