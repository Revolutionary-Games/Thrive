namespace Components;

using Arch.Core;
using Newtonsoft.Json;
using SharedBase.Archive;

/// <summary>
///   Defines how siderophore projectile behaves
/// </summary>
public struct SiderophoreProjectile : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Sender
    /// </summary>
    public Entity Sender;

    /// <summary>
    ///   Scales the efficiency
    /// </summary>
    public float Amount;

    /// <summary>
    ///   Is already used and to be disposed
    /// </summary>
    public bool IsUsed;

    /// <summary>
    ///   Used by systems internally to know when they have processed the initial adding of a siderophore. Should not be
    ///   modified from other places.
    /// </summary>
    [JsonIgnore]
    public bool ProjectileInitialized;

    public SiderophoreProjectile(Entity sender)
    {
        Sender = sender;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentSiderophoreProjectile;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(A PROPERTY);
        writer.WriteObject(A PROPERTY OF COMPLEX TYPE);
    }
}

public static class SiderophoreProjectileHelpers
{
    public static SiderophoreProjectile ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SiderophoreProjectile.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SiderophoreProjectile.SERIALIZATION_VERSION);

        return new SiderophoreProjectile
        {
            AProperty = reader.ReadFloat(),
            AnotherProperty = reader.ReadObject<PropertyTypeGoesHere>(),
        };
    }
}
