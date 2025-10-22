namespace Components;

using SharedBase.Archive;

/// <summary>
///   Marks an entity as the one for the game's camera to follow. Also requires a <see cref="WorldPosition"/>
///   component.
/// </summary>
public struct CameraFollowTarget : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   If set to true, this target is ignored. Only one active target should exist as once, otherwise a random
    ///   one is selected to show with the camera.
    /// </summary>
    public bool Disabled;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentCameraFollowTarget;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Disabled);
    }
}

public static class CameraFollowTargetHelpers
{
    public static CameraFollowTarget ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > CameraFollowTarget.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, CameraFollowTarget.SERIALIZATION_VERSION);

        return new CameraFollowTarget
        {
            Disabled = reader.ReadBool(),
        };
    }
}
