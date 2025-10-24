namespace Components;

using SharedBase.Archive;

/// <summary>
///   Entity uses a predefined visual that is automatically loaded by
///   <see cref="Systems.PredefinedVisualLoaderSystem"/>. This is much better to use for save compatibility than
///   directly setting the visuals when creating en entity as that can't be automatically redone when loading a
///   save.
/// </summary>
public struct PredefinedVisuals : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Specifies what this entity should display as its visuals
    /// </summary>
    public VisualResourceIdentifier VisualIdentifier;

    /// <summary>
    ///   Don't touch this, used by the system for handling this. Not saved so that after a load the visual is
    ///   properly reloaded.
    /// </summary>
    public VisualResourceIdentifier LoadedInstance;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentPredefinedVisuals;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)VisualIdentifier);
    }
}

public static class PredefinedVisualsHelpers
{
    public static PredefinedVisuals ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > PredefinedVisuals.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, PredefinedVisuals.SERIALIZATION_VERSION);

        return new PredefinedVisuals
        {
            VisualIdentifier = (VisualResourceIdentifier)reader.ReadInt32(),
        };
    }
}
