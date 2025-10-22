namespace Components;

using SharedBase.Archive;

/// <summary>
///   Entity is roughly circular, and this provides easy access to that entity's radius
/// </summary>
/// <remarks>
///   <para>
///     This component type was added as I wasn't confident enough in remaking the
///     <see cref="Systems.EngulfingSystem"/> without having access to microbe chunk radius when calculating engulf
///     positions -hhyyrylainen
///   </para>
/// </remarks>
public struct EntityRadiusInfo : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public float Radius;

    public EntityRadiusInfo(float radius)
    {
        Radius = radius;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentEntityRadiusInfo;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Radius);
    }
}

public static class EntityRadiusInfoHelpers
{
    public static EntityRadiusInfo ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > EntityRadiusInfo.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, EntityRadiusInfo.SERIALIZATION_VERSION);

        return new EntityRadiusInfo
        {
            Radius = reader.ReadFloat(),
        };
    }
}
