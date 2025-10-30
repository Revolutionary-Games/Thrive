using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;
using SharedBase.Archive;

/// <summary>
///   Details for a fossilised species saved on disk.
/// </summary>
public class FossilisedSpeciesInformation : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Details for a fossilised species saved on disk.
    /// </summary>
    /// <param name="type">The type of this species (e.g. microbe)</param>
    public FossilisedSpeciesInformation(SpeciesType type)
    {
        Type = type;
    }

    /// <summary>
    ///   Type of this species. This enum should not be reordered as that will break existing fossilized files which
    ///   can never be done (as there's no process for version-upgrading them).
    /// </summary>
    public enum SpeciesType
    {
        [Description("MICROBE")]
        Microbe,

        [Description("MULTICELLULAR")]
        Multicellular,

        [Description("MACROSCOPIC")]
        Macroscopic,
    }

    /// <summary>
    ///   The version of Thrive the species was saved in.
    /// </summary>
    public string ThriveVersion { get; set; } = Constants.Version;

    /// <summary>
    ///   The name of the user who saved this species.
    /// </summary>
    public string Creator { get; set; } = Settings.Instance.ActiveUsername;

    /// <summary>
    ///   The time at which this species was saved.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    ///   ID for this fossilised species.
    /// </summary>
    public Guid ID { get; set; } = Guid.NewGuid();

    /// <summary>
    ///   The type of this species, e.g. microbe.
    /// </summary>
    public SpeciesType Type { get; set; }

    public string FormattedName { get; set; } = string.Empty;

    /// <summary>
    ///   Set when data about a file could be loaded, but the file is not good to try to actually load a species from.
    /// </summary>
    [JsonIgnore]
    public bool IsInvalidOrOutdated { get; set; }

    [JsonIgnore]
    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    [JsonIgnore]
    public ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.FossilisedSpeciesInformation;

    [JsonIgnore]
    public bool CanBeReferencedInArchive => false;

    public static FossilisedSpeciesInformation ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new FossilisedSpeciesInformation((SpeciesType)reader.ReadInt32())
        {
            ThriveVersion = reader.ReadString() ?? throw new NullArchiveObjectException(),
            Creator = reader.ReadString() ?? throw new NullArchiveObjectException(),
            CreatedAt = DateTime.Parse(reader.ReadString() ?? throw new NullArchiveObjectException(),
                CultureInfo.InvariantCulture),
            ID = Guid.Parse(reader.ReadString() ?? throw new NullArchiveObjectException()),
            FormattedName = reader.ReadString() ?? throw new NullArchiveObjectException(),
        };

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)Type);
        writer.Write(ThriveVersion);
        writer.Write(Creator);
        writer.Write(CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        writer.Write(ID.ToString());
        writer.Write(FormattedName);
    }
}
