using System;
using SharedBase.Archive;

/// <summary>
///   Class that stores species-wide information.
/// </summary>
/// <remarks>
///   <para>
///     It can be expanded to have any species parameter we might want to keep,
///     e.g. base structure, behavioural values... and such to draw species history.
///     Note that specificities of individuals, such as duplicated organelles, should not be stored here.
///   </para>
/// </remarks>
public class SpeciesInfo : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public uint ID;
    public long Population;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.SpeciesInfo;
    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.SpeciesInfo)
            throw new NotSupportedException();

        writer.WriteObject((SpeciesInfo)obj);
    }

    public static SpeciesInfo ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new SpeciesInfo
        {
            ID = reader.ReadUInt32(),
            Population = reader.ReadInt64(),
        };
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(ID);
        writer.Write(Population);
    }
}
