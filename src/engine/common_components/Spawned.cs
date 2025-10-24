namespace Components;

using SharedBase.Archive;

/// <summary>
///   Entity that has been spawned by a spawn system and can be automatically despawned
/// </summary>
public struct Spawned : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   If the squared distance to the player of this object is greater than this, it is despawned.
    /// </summary>
    public float DespawnRadiusSquared;

    /// <summary>
    ///   How much this entity contributes to the entity limit relative to a single node
    /// </summary>
    public float EntityWeight;

    /// <summary>
    ///   Set to true when despawning is disallowed temporarily. For permanently disallowing despawning, remove this
    ///   component.
    /// </summary>
    public bool DisallowDespawning;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentSpawned;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(DespawnRadiusSquared);
        writer.Write(EntityWeight);
        writer.Write(DisallowDespawning);
    }
}

public static class SpawnedHelpers
{
    public static Spawned ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > Spawned.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, Spawned.SERIALIZATION_VERSION);

        return new Spawned
        {
            DespawnRadiusSquared = reader.ReadFloat(),
            EntityWeight = reader.ReadFloat(),
            DisallowDespawning = reader.ReadBool(),
        };
    }
}
