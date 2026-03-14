namespace AutoEvo;

using SharedBase.Archive;

/// <summary>
///   Makes sure species need to be adapted well enough to the environmental conditions in their patch to survive. Also
///   has the part to generate mutations to better match the environment.
/// </summary>
public class EnvironmentalTolerancePressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_ENVIRONMENTAL_TOLERANCE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public EnvironmentalTolerancePressure(float weight) : base(weight, [new ModifyEnvironmentalTolerance()])
    {
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION_BASE;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.EnvironmentalTolerancePressure;

    public static EnvironmentalTolerancePressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION_BASE or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_BASE);

        var instance = new EnvironmentalTolerancePressure(reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, version);
        return instance;
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        // TODO: multicellular tolerance (only needed once multicellular works in general in auto-evo)
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        // Use scores to encourage species to be adapted to their environment
        return (float)MicrobeEnvironmentalToleranceCalculations.CalculateTotalToleranceScore(microbeSpecies,
            patch.Biome);
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
