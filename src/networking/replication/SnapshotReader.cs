using Arch.Core.Extensions;

/// <summary>
///   Applies snapshot messages to the client's copy of the world
/// </summary>
/// <remarks>
///   <para>
///     Unknown entities are skipped rather than treated as an error. A snapshot can arrive before the spawn
///     message for an entity, or after it was already despawned locally, and neither case is a problem.
///   </para>
/// </remarks>
public class SnapshotReader
{
    private readonly ReplicationRegistry registry;
    private readonly NetworkedEntityIndex entityIndex;

    public SnapshotReader(ReplicationRegistry registry, NetworkedEntityIndex entityIndex)
    {
        this.registry = registry;
        this.entityIndex = entityIndex;
    }

    /// <summary>
    ///   Tick of the last snapshot that was applied, which is what gets acknowledged back to the server
    /// </summary>
    public uint LastAppliedTick { get; private set; }

    /// <summary>
    ///   Reads a snapshot message. The message type byte must already be consumed.
    /// </summary>
    /// <param name="reader">Reader holding the message</param>
    /// <returns>True when the snapshot was applied, false when it was older than what is already applied</returns>
    public bool Apply(NetworkReader reader)
    {
        uint serverTick = reader.ReadUInt32();

        // Baseline tick is not used until delta encoding exists, but it is read to keep the layout correct
        reader.ReadUInt32();

        if (serverTick <= LastAppliedTick)
            return false;

        int entityCount = reader.ReadUInt16();

        for (int i = 0; i < entityCount; ++i)
        {
            uint networkId = reader.ReadUInt32();
            int componentCount = reader.ReadByte();

            bool known = entityIndex.TryGet(networkId, out var entity) && entity.IsAlive();

            for (int j = 0; j < componentCount; ++j)
            {
                var replicator = registry.GetComponentReplicator(reader.ReadByte());

                if (known)
                {
                    replicator.Read(entity, reader);
                }
                else
                {
                    replicator.Skip(reader);
                }
            }
        }

        LastAppliedTick = serverTick;
        return true;
    }

    public void Reset()
    {
        LastAppliedTick = 0;
    }
}
