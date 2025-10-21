namespace AutoEvo;

using System;
using SharedBase.Archive;

/// <summary>
///   Species mutation and population data from a single generation, with or without the full species.
/// </summary>
public class SpeciesRecordLite : SpeciesRecord, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public SpeciesRecordLite(long population, uint? mutatedPropertiesID,
        uint? splitFromID, Species? species) : base(population, mutatedPropertiesID, splitFromID)
    {
        if (species == null && (mutatedPropertiesID != null || splitFromID != null))
            throw new InvalidOperationException("Species which newly mutated or split off must have species data");

        Species = species;
    }

    public SpeciesRecordLite(long population, Species species) : this(population, null, null, species)
    {
    }

    /// <summary>
    ///   Full species data for this species.
    ///   If null, the species is assumed to have full data earlier in the game history.
    /// </summary>
    public Species? Species { get; }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.SpeciesRecordLite;
    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.SpeciesRecordLite)
            throw new NotSupportedException();

        writer.WriteObject((SpeciesRecordLite)obj);
    }

    public static SpeciesRecordLite ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new SpeciesRecordLite(reader.ReadInt64(), reader.ReadObjectOrNull<uint>(),
            reader.ReadObjectOrNull<uint>(), reader.ReadObjectOrNull<Species>());
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        base.WriteToArchive(writer);

        if (Species != null)
        {
            writer.WriteObject(Species);
        }
        else
        {
            writer.WriteNullObject();
        }
    }
}
