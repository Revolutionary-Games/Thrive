namespace Components;

using SharedBase.Archive;

/// <summary>
///   Holds the sex of a microbe, which gamete type it produces. In microbe stage this is always "All".
/// </summary>
[ComponentIsReadByDefault]
public struct MicrobeSex : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public GameteType Sex;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentMicrobeSex;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)Sex);
    }
}

public static class MicrobeSexHelpers
{
    public static MicrobeSex ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > MicrobeSex.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, MicrobeSex.SERIALIZATION_VERSION);

        var result = new MicrobeSex
        {
            Sex = (GameteType)reader.ReadInt32(),
        };

        return result;
    }
}
