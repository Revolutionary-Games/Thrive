namespace Components;

using Godot;
using SharedBase.Archive;

public struct SpatialAnimation : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public Vector3 InitialPosition;
    public Vector3 FinalPosition;

    public Vector3 InitialScale;
    public Vector3 FinalScale;

    public float AnimationTime;
    public float TimeSpent;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentSpatialAnimation;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(InitialPosition);
        writer.Write(FinalPosition);

        writer.Write(InitialScale);
        writer.Write(FinalScale);

        writer.Write(AnimationTime);
        writer.Write(TimeSpent);
    }
}

public static class SpatialAnimationHelpers
{
    public static SpatialAnimation ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SpatialAnimation.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SpatialAnimation.SERIALIZATION_VERSION);

        return new SpatialAnimation
        {
            InitialPosition = reader.ReadVector3(),
            FinalPosition = reader.ReadVector3(),
            InitialScale = reader.ReadVector3(),
            FinalScale = reader.ReadVector3(),

            AnimationTime = reader.ReadFloat(),
            TimeSpent = reader.ReadFloat(),
        };
    }
}
