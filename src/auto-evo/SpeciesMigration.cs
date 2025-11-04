namespace AutoEvo;

using System;
using SharedBase.Archive;

/// <summary>
///   Data for a Species migration between two patches
/// </summary>
public class SpeciesMigration : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public Patch From;
    public Patch To;
    public long Population;

    public SpeciesMigration(Patch from, Patch to, long population)
    {
        From = from ?? throw new ArgumentNullException(nameof(from));
        To = to ?? throw new ArgumentNullException(nameof(to));
        Population = population;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.SpeciesMigration;
    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.SpeciesMigration)
            throw new NotSupportedException();

        writer.WriteObject((SpeciesMigration)obj);
    }

    public static SpeciesMigration ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new SpeciesMigration(reader.ReadObject<Patch>(), reader.ReadObject<Patch>(), reader.ReadInt64());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(From);
        writer.WriteObject(To);
        writer.Write(Population);
    }
}
