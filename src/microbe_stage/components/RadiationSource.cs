namespace Components;

using System.Collections.Generic;
using Arch.Core;
using SharedBase.Archive;

/// <summary>
///   Marks an entity as emitting radiation
/// </summary>
public struct RadiationSource : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public float RadiationStrength;
    public float Radius;

    public HashSet<Entity>? RadiatedEntities;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentRadiationSource;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(RadiationStrength);
        writer.Write(Radius);
        writer.WriteObjectOrNull(RadiatedEntities);
    }
}

public static class RadiationSourceHelpers
{
    public static RadiationSource ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > RadiationSource.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, RadiationSource.SERIALIZATION_VERSION);

        return new RadiationSource
        {
            RadiationStrength = reader.ReadFloat(),
            Radius = reader.ReadFloat(),
            RadiatedEntities = reader.ReadObjectOrNull<HashSet<Entity>>(),
        };
    }
}
