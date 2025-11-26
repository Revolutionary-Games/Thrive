namespace Components;

using SharedBase.Archive;

/// <summary>
///   General marker for species members to be able to check other members of their species
/// </summary>
[ComponentIsReadByDefault]
public struct SpeciesMember : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Access to the full species data. Comparing species should be done with the ID, but this is required here
    ///   as entities need to know various properties about their species for various gameplay purposes.
    /// </summary>
    public Species Species;

    /// <summary>
    ///   ID of the species this is a member of. The <see cref="GameWorld"/> should make sure there can't be
    ///   duplicate IDs. If there are then it is a world or mutation problem. Used as an optimization to quickly
    ///   compare species.
    /// </summary>
    public uint ID;

    public SpeciesMember(Species species)
    {
        Species = species;
        ID = species.ID;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentSpeciesMember;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Species);
        writer.Write(ID);
    }
}

public static class SpeciesMemberHelpers
{
    public static SpeciesMember ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SpeciesMember.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SpeciesMember.SERIALIZATION_VERSION);

        return new SpeciesMember
        {
            Species = reader.ReadObject<Species>(),
            ID = reader.ReadUInt32(),
        };
    }
}
