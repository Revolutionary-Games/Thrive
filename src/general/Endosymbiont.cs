using System;
using SharedBase.Archive;

/// <summary>
///   Finalised endosymbiosis action for <see cref="EndosymbiosisData"/> on a <see cref="Species"/>
/// </summary>
public class Endosymbiont : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public Endosymbiont(OrganelleDefinition resultingOrganelle, Species originallyFromSpecies)
    {
        ResultingOrganelle = resultingOrganelle;
        OriginallyFromSpecies = originallyFromSpecies;
    }

    public OrganelleDefinition ResultingOrganelle { get; private set; }

    public Species OriginallyFromSpecies { get; private set; }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.Endosymbiont;
    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.Endosymbiont)
            throw new NotSupportedException();

        writer.WriteObject((Endosymbiont)obj);
    }

    public static Endosymbiont ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new Endosymbiont(reader.ReadObject<OrganelleDefinition>(), reader.ReadObject<Species>());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(ResultingOrganelle);
        writer.WriteObject(OriginallyFromSpecies);
    }

    public Endosymbiont Clone()
    {
        return new Endosymbiont(ResultingOrganelle, OriginallyFromSpecies);
    }
}
