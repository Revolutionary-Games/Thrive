using System;
using SharedBase.Archive;

/// <summary>
///   Population effect external to the auto-evo simulation
/// </summary>
public class ExternalEffect : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public ExternalEffect(Species species, int constant, float coefficient, string eventType, Patch patch)
    {
        if (string.IsNullOrEmpty(eventType))
            throw new ArgumentException("May not be empty or null", nameof(eventType));

        Species = species;
        Constant = constant;
        Coefficient = coefficient;
        EventType = eventType;
        Patch = patch;
    }

    public Species Species { get; }

    public int Constant { get; set; }

    public float Coefficient { get; set; }

    public string EventType { get; set; }

    /// <summary>
    ///   The patch this effect affects.
    /// </summary>
    public Patch Patch { get; set; }

    public bool Immediate { get; set; }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.ExternalEffect;
    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.ExternalEffect)
            throw new NotSupportedException();

        writer.WriteObject((ExternalEffect)obj);
    }

    public static ExternalEffect ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new ExternalEffect(reader.ReadObject<Species>(), reader.ReadInt32(), reader.ReadFloat(),
            reader.ReadString() ?? throw new NullArchiveObjectException(), reader.ReadObject<Patch>())
        {
            Immediate = reader.ReadBool(),
        };
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Species);
        writer.Write(Constant);
        writer.Write(Coefficient);
        writer.Write(EventType);
        writer.WriteObject(Patch);
        writer.Write(Immediate);
    }
}
