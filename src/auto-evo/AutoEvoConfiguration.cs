using SharedBase.Archive;

/// <summary>
///   Settings for auto-evo that have been (potentially) tweaked
/// </summary>
public class AutoEvoConfiguration : IAutoEvoConfiguration
{
    public const ushort SERIALIZATION_VERSION = 1;

    public int MutationsPerSpecies { get; set; }

    public int MoveAttemptsPerSpecies { get; set; }

    public bool StrictNicheCompetition { get; set; }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.AutoEvoConfiguration;
    public bool CanBeReferencedInArchive => true;

    public static AutoEvoConfiguration ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new AutoEvoConfiguration
        {
            MutationsPerSpecies = reader.ReadInt32(),
            MoveAttemptsPerSpecies = reader.ReadInt32(),
            StrictNicheCompetition = reader.ReadBool(),
        };
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(MutationsPerSpecies);
        writer.Write(MoveAttemptsPerSpecies);
        writer.Write(StrictNicheCompetition);
    }
}
