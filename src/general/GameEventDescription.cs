using System;
using SharedBase.Archive;

/// <summary>
///   A text-based description of what has happened in a game environment. Decorated with an icon if there's any.
/// </summary>
public class GameEventDescription : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public GameEventDescription(LocalizedString description, string? iconPath, bool highlighted, bool showInReport)
    {
        Description = description;
        IconPath = iconPath;
        Highlighted = highlighted;
        ShowInReport = showInReport;
    }

    /// <summary>
    ///   The text description of this event
    /// </summary>
    public LocalizedString Description { get; private set; }

    /// <summary>
    ///   The resource path to the associated icon
    /// </summary>
    public string? IconPath { get; private set; }

    /// <summary>
    ///   If true, this event will be highlighted in the timeline UI
    /// </summary>
    public bool Highlighted { get; private set; }

    /// <summary>
    ///   Some events show up in the report tab to draw extra attention to them
    /// </summary>
    public bool ShowInReport { get; private set; }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.GameEventDescription;
    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.GameEventDescription)
            throw new NotSupportedException();

        writer.WriteObject((GameEventDescription)obj);
    }

    public static GameEventDescription ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new GameEventDescription(reader.ReadObject<LocalizedString>(), reader.ReadString(),
            reader.ReadBool(), reader.ReadBool());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Description);
        writer.Write(IconPath);
        writer.Write(Highlighted);
        writer.Write(ShowInReport);
    }
}
