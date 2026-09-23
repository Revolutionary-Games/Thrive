using Arch.Core;
using Arch.Core.Extensions;
using Components;

/// <summary>
///   Applies snapshot messages to the client's copy of the world
/// </summary>
/// <remarks>
///   <para>
///     Only what changed is in a snapshot, so anything not mentioned keeps the value it already had. That makes
///     acknowledging correctly important: the server stops sending a value once this client says it has it.
///   </para>
/// </remarks>
public class SnapshotReader
{
    private readonly ReplicationRegistry registry;
    private readonly NetworkedEntityIndex entityIndex;
    private readonly INetworkEntityDestroyer entityDestroyer;

    public SnapshotReader(ReplicationRegistry registry, NetworkedEntityIndex entityIndex,
        INetworkEntityDestroyer entityDestroyer)
    {
        this.registry = registry;
        this.entityIndex = entityIndex;
        this.entityDestroyer = entityDestroyer;
    }

    /// <summary>
    ///   Tick of the newest snapshot that was applied, which is what gets acknowledged back to the server
    /// </summary>
    public uint LastAppliedTick { get; private set; }

    /// <summary>
    ///   True when something was applied that hasn't been acknowledged yet
    /// </summary>
    public bool NeedsToSendAcknowledgement { get; private set; }

    /// <summary>
    ///   Reads a snapshot message. The message type byte must already be consumed.
    /// </summary>
    /// <param name="reader">Reader holding the message</param>
    /// <returns>True when the snapshot was applied, false when it was older than what is already applied</returns>
    public bool Apply(NetworkReader reader)
    {
        uint serverTick = reader.ReadUInt32();

        // What the server thinks this client has confirmed, only useful for debugging
        reader.ReadUInt32();

        // Snapshots travel unreliably so they can arrive out of order. An older one holds only changes that the
        // newer one already accounts for, so applying it would undo current values.
        if (serverTick <= LastAppliedTick)
            return false;

        int despawnCount = reader.ReadUInt16();

        for (int i = 0; i < despawnCount; ++i)
        {
            ApplyDespawn(reader.ReadUInt32());
        }

        int entityCount = reader.ReadUInt16();

        for (int i = 0; i < entityCount; ++i)
        {
            ApplyEntity(reader);
        }

        LastAppliedTick = serverTick;
        NeedsToSendAcknowledgement = true;
        return true;
    }

    /// <summary>
    ///   Writes the acknowledgement the server needs in order to stop repeating what this client has
    /// </summary>
    public void WriteAcknowledgement(NetworkWriter writer)
    {
        writer.Write(MessageType.SnapshotAcknowledgement);
        writer.Write(LastAppliedTick);
        NeedsToSendAcknowledgement = false;
    }

    public void Reset()
    {
        LastAppliedTick = 0;
        NeedsToSendAcknowledgement = false;
    }

    private void ApplyDespawn(uint networkId)
    {
        if (!entityIndex.TryGet(networkId, out var entity))
            return;

        entityIndex.Remove(networkId);

        if (entity.IsAlive())
            entityDestroyer.DestroyEntity(entity);
    }

    private void ApplyEntity(NetworkReader reader)
    {
        uint networkId = reader.ReadUInt32();
        byte flags = reader.ReadByte();

        bool known = entityIndex.TryGet(networkId, out var entity) && entity.IsAlive();

        if ((flags & SnapshotWriter.FLAG_INCLUDES_SPAWN) != 0)
        {
            ushort archetypeId = reader.ReadUInt16();
            int owningPeerId = reader.ReadInt32();

            var recipe = registry.GetSpawnRecipe(archetypeId);

            if (known)
            {
                // This is a repeat because an earlier acknowledgement didn't reach the server
                recipe.SkipSpawnData(reader);
            }
            else
            {
                entity = recipe.Spawn(reader, new NetworkEntity(networkId, archetypeId, owningPeerId));
                entityIndex.Register(networkId, entity);
                known = true;
            }
        }

        int componentCount = reader.ReadByte();

        for (int i = 0; i < componentCount; ++i)
        {
            var replicator = registry.GetComponentReplicator(reader.ReadByte());

            if (known)
            {
                replicator.Read(entity, reader);
            }
            else
            {
                // Without spawn data there is nothing to apply this to. The server keeps sending until it is
                // acknowledged, so nothing is lost.
                replicator.Skip(reader);
            }
        }

        if ((flags & SnapshotWriter.FLAG_INCLUDES_REMOVALS) != 0)
            ApplyComponentRemovals(reader, entity, known);
    }

    private void ApplyComponentRemovals(NetworkReader reader, in Entity entity, bool known)
    {
        int removedCount = reader.ReadByte();

        for (int i = 0; i < removedCount; ++i)
        {
            var replicator = registry.GetComponentReplicator(reader.ReadByte());

            // A removal carries no data, so an unknown entity only needs the ID read past
            if (known)
                replicator.Remove(entity);
        }
    }
}
