namespace Components;

using Godot;
using SharedBase.Archive;

/// <summary>
///   World-space coordinates of an entity. Note a constructor must be used to get <see cref="Rotation"/>
///   initialized correctly
/// </summary>
public struct WorldPosition : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public Vector3 Position;
    public Quaternion Rotation;

    public WorldPosition(Vector3 position)
    {
        Position = position;
        Rotation = Quaternion.Identity;
    }

    public WorldPosition(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentWorldPosition;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Position);
        writer.Write(Rotation);
    }
}

public static class WorldPositionHelpers
{
    public static WorldPosition ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > WorldPosition.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, WorldPosition.SERIALIZATION_VERSION);

        return new WorldPosition
        {
            Position = reader.ReadVector3(),
            Rotation = reader.ReadQuaternion(),
        };
    }

    public static Transform3D ToTransform(this ref WorldPosition position)
    {
        return new Transform3D(new Basis(position.Rotation), position.Position);
    }
}
