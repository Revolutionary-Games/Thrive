namespace Components;

using SharedBase.Archive;

/// <summary>
///   Player readable name for an entity. Must be set on init so always use the constructor.
/// </summary>
public struct ReadableName : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public LocalizedString Name;

    public ReadableName(LocalizedString name)
    {
        Name = name;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentReadableName;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Name);
    }
}

public static class ReadableNameHelpers
{
    public static ReadableName ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > ReadableName.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, ReadableName.SERIALIZATION_VERSION);

        return new ReadableName
        {
            Name = reader.ReadObject<LocalizedString>(),
        };
    }
}
