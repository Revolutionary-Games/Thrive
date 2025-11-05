namespace Components;

using SharedBase.Archive;

/// <summary>
///   Defines toxin damage dealt by an entity
/// </summary>
public struct ToxinDamageSource : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Scales the damage (or other effect the toxin does)
    /// </summary>
    public float ToxinAmount;

    public AgentProperties ToxinProperties;

    /// <summary>
    ///   Set to true when this projectile has hit and can't no longer deal damage
    /// </summary>
    public bool ProjectileUsed;

    /// <summary>
    ///   Used by systems internally to know when they have processed the initial adding of a toxin. Should not be
    ///   modified from other places.
    /// </summary>
    public bool ProjectileInitialized;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentToxinDamageSource;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(ToxinAmount);
        writer.WriteObject(ToxinProperties);
        writer.Write(ProjectileUsed);
    }
}

public static class ToxinDamageSourceHelpers
{
    public static ToxinDamageSource ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > ToxinDamageSource.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, ToxinDamageSource.SERIALIZATION_VERSION);

        return new ToxinDamageSource
        {
            ToxinAmount = reader.ReadFloat(),
            ToxinProperties = reader.ReadObject<AgentProperties>(),
            ProjectileUsed = reader.ReadBool(),
        };
    }
}
