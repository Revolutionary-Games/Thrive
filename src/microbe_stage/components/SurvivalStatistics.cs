namespace Components;

using SharedBase.Archive;

/// <summary>
///   Collects information to give population bonuses and penalties to species based on how well they do in the
///   stage interacting with each other and the player for real
/// </summary>
public struct SurvivalStatistics : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public float EscapeInterval;

    /// <summary>
    ///   Used to prevent population bonus from escaping a predator triggering too much
    /// </summary>
    public bool HasEscaped;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentSurvivalStatistics;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(EscapeInterval);
        writer.Write(HasEscaped);
    }
}

public static class SurvivalStatisticsHelpers
{
    public static SurvivalStatistics ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SurvivalStatistics.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SurvivalStatistics.SERIALIZATION_VERSION);

        return new SurvivalStatistics
        {
            EscapeInterval = reader.ReadFloat(),
            HasEscaped = reader.ReadBool(),
        };
    }
}
