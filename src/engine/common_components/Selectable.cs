namespace Components;

using SharedBase.Archive;

/// <summary>
///   Entity can be selected somehow (using stage-specific mechanics)
/// </summary>
public struct Selectable : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public bool Selected;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentSelectable;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Selected);
    }
}

public static class SelectableHelpers
{
    public static Selectable ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > Selectable.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, Selectable.SERIALIZATION_VERSION);

        return new Selectable
        {
            Selected = reader.ReadBool(),
        };
    }
}
