namespace Components;

using SharedBase.Archive;

/// <summary>
///   Specifies an exact scene path to load <see cref="SpatialInstance"/> from. Using
///   <see cref="PredefinedVisuals"/> should be preferred for all cases where that is usable for the situation.
/// </summary>
public struct PathLoadedSceneVisuals : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   The scene to display. Setting this to null stops displaying the current scene
    /// </summary>
    public string? ScenePath;

    /// <summary>
    ///   Internal variable for the loading system; do not touch
    /// </summary>
    public string? LastLoadedScene;

    /// <summary>
    ///   If true then the loaded scene is directly attached to a <see cref="SpatialInstance"/>. When this is done,
    ///   the scene's root scale or transform does not work. So only scenes that work fine with this should set
    ///   this to true.
    /// </summary>
    public bool AttachDirectlyToScene;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentPathLoadedSceneVisuals;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(ScenePath);
        writer.Write(AttachDirectlyToScene);
    }
}

public static class PathLoadedSceneVisualsHelpers
{
    public static PathLoadedSceneVisuals ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > PathLoadedSceneVisuals.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, PathLoadedSceneVisuals.SERIALIZATION_VERSION);

        return new PathLoadedSceneVisuals
        {
            ScenePath = reader.ReadString(),
            AttachDirectlyToScene = reader.ReadBool(),
        };
    }
}
