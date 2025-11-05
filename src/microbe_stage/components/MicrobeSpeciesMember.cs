namespace Components;

using SharedBase.Archive;

/// <summary>
///   Entity is a member of a species and has species-related data applied to it. Note that for most things
///   <see cref="CellProperties"/> should be used instead as that works for multicellular things as well.
/// </summary>
[ComponentIsReadByDefault]
public struct MicrobeSpeciesMember : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public MicrobeSpecies Species;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentMicrobeSpeciesMember;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Species);
    }
}

public static class MicrobeSpeciesMemberHelpers
{
    public static MicrobeSpeciesMember ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > MicrobeSpeciesMember.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, MicrobeSpeciesMember.SERIALIZATION_VERSION);

        return new MicrobeSpeciesMember
        {
            Species = reader.ReadObject<MicrobeSpecies>(),
        };
    }
}
