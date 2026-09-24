using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Components;

/// <summary>
///   Builds the per-peer snapshot messages a server sends
/// </summary>
/// <remarks>
///   <para>
///     Only data a peer is not known to have already is included. A component is sent when its written form
///     differs from the baseline, and keeps being sent until the peer acknowledges a snapshot containing it. An
///     entity with nothing to say is left out of the message entirely, and a snapshot with nothing in it at all
///     isn't sent.
///   </para>
///   <para>
///     Spawns, despawns and component removals all follow the same rule as values: they repeat until the peer
///     acknowledges a snapshot containing them.
///   </para>
///   <para>
///     INVARIANT: which the acknowledgement logic and <see cref="SnapshotReader"/>'s rule for dropping
///     out-of-order packets both depend on: every packet carries *all* of that peer's unconfirmed state,
///     never juat what changed since the last packet. Sending only the changes since the last *sent*
///     packet looks like an obvious saving and breaks both. One lost packet would lose a value forever,
///     and acknowledgements would confirm data the peer never received.
///   </para>
///   <para>
///     The one allowed exception is the packet size cap: entities left out of a full packet are marked deferred
///     so they are restamped when next included, which is what keeps the invariant true for them.
///   </para>
/// </remarks>
public class SnapshotWriter
{
    /// <summary>
    ///   Set in the per-entity flags when spawn data is included
    /// </summary>
    public const byte FLAG_INCLUDES_SPAWN = 1;

    /// <summary>
    ///   Set in the per-entity flags when the entity lost components the peer still has
    /// </summary>
    public const byte FLAG_INCLUDES_REMOVALS = 2;

    private readonly ReplicationRegistry registry;

    private readonly NetworkWriter writer = new();

    /// <summary>
    ///   Component data is written here first so it can be compared against the baseline before deciding to
    ///   include it
    /// </summary>
    private readonly NetworkWriter componentScratch = new(256);

    private readonly Dictionary<int, PeerReplicationState> peerStates = new();

    private readonly HashSet<uint> relevantIds = new();
    private readonly List<byte> removedComponents = new();

    public SnapshotWriter(ReplicationRegistry registry)
    {
        this.registry = registry;
    }

    /// <summary>
    ///   Writes a snapshot for one peer
    /// </summary>
    /// <param name="serverTick">Tick the server is on. Must be above zero and increasing.</param>
    /// <param name="peerId">Peer this snapshot is for</param>
    /// <param name="entities">Entities to consider</param>
    /// <param name="interestManager">Decides which of the entities this peer gets</param>
    /// <returns>The message to send, or an empty span when there is nothing to tell this peer</returns>
    public ReadOnlySpan<byte> Write(uint serverTick, int peerId, List<Entity> entities,
        IInterestManager interestManager)
    {
        if (serverTick == 0)
            throw new ArgumentException("Server tick must be above zero", nameof(serverTick));

        var peerState = GetOrCreatePeerState(peerId);
        uint sequence = peerState.NextSequence;

        writer.Reset();
        writer.Write(MessageType.Snapshot);
        writer.Write(sequence);
        writer.Write(serverTick);

        WriteDespawns(peerState, sequence);

        int countPosition = writer.Length;
        writer.Write((ushort)0);

        ushort writtenEntities = 0;
        relevantIds.Clear();

        // Starting the sweep at a rotating offset stops a full packet from always cutting off the same
        // entities, which would leave them permanently stale
        int entityCount = entities.Count;
        int startOffset = entityCount > 0 ? peerState.SendRotationOffset % entityCount : 0;
        bool packetFull = false;
        int stoppedAt = 0;

        for (int i = 0; i < entityCount; ++i)
        {
            int index = (startOffset + i) % entityCount;
            var entity = entities[index];

            if (!entity.IsAliveAndHas<NetworkEntity>())
                continue;

            ref var networkEntity = ref entity.Get<NetworkEntity>();

            if (!interestManager.IsRelevant(peerId, entity))
                continue;

            relevantIds.Add(networkEntity.Id);

            if (packetFull)
            {
                // Everything left out of a packet that gets sent has to be restamped when it is next
                // included, or an acknowledgement of this packet would confirm data that isn't in it
                if (peerState.TryGet(networkEntity.Id, out var skippedState))
                    skippedState.MarkDeferred();

                continue;
            }

            if (WriteEntity(peerState, peerId, entity, in networkEntity, serverTick, sequence))
                ++writtenEntities;

            if (writer.Length >= NetworkConstants.SNAPSHOT_MAX_PACKET_SIZE)
            {
                packetFull = true;
                stoppedAt = index + 1;
            }
        }

        // An entity that stopped being relevant has to be sent in full when it returns, as the peer may have
        // missed any number of changes in the meantime
        peerState.ForgetEntitiesOutsideOf(relevantIds);

        writer.OverwriteUInt16(countPosition, writtenEntities);

        if (writtenEntities == 0 && peerState.PendingDespawns.Count == 0)
            return ReadOnlySpan<byte>.Empty;

        peerState.SendRotationOffset = packetFull ? stoppedAt : 0;
        peerState.OnPacketSent();

        return writer.WrittenData;
    }

    /// <summary>
    ///   Applies an acknowledgement from a peer, which is what allows data to stop being sent
    /// </summary>
    public void OnAcknowledged(int peerId, uint sequence)
    {
        if (peerStates.TryGetValue(peerId, out var state))
            state.Acknowledge(sequence);
    }

    /// <summary>
    ///   Tells every peer that an entity is gone. Repeated until each of them acknowledges it.
    /// </summary>
    public void OnEntityDespawned(uint networkId)
    {
        foreach (var entry in peerStates)
        {
            entry.Value.QueueDespawn(networkId);
        }
    }

    public void ForgetPeer(int peerId)
    {
        peerStates.Remove(peerId);
    }

    public void Reset()
    {
        peerStates.Clear();
    }

    private PeerReplicationState GetOrCreatePeerState(int peerId)
    {
        if (peerStates.TryGetValue(peerId, out var state))
            return state;

        state = new PeerReplicationState(registry.Replicators.Count);
        peerStates[peerId] = state;
        return state;
    }

    private void WriteDespawns(PeerReplicationState peerState, uint sequence)
    {
        var despawns = peerState.PendingDespawns;

        writer.Write((ushort)despawns.Count);

        for (int i = 0; i < despawns.Count; ++i)
        {
            writer.Write(despawns[i].NetworkId);
            peerState.MarkDespawnSent(i, sequence);
        }
    }

    /// <summary>
    ///   Writes one entity, or nothing at all when the peer is already up to date on it
    /// </summary>
    /// <returns>True when the entity was written</returns>
    private bool WriteEntity(PeerReplicationState peerState, int peerId, in Entity entity,
        in NetworkEntity networkEntity, uint serverTick, uint sequence)
    {
        var entityState = peerState.GetOrCreate(networkEntity.Id);

        bool forcedFullUpdate = serverTick - entityState.LastForcedFullTick >=
            NetworkConstants.SNAPSHOT_FORCED_FULL_UPDATE_TICKS;

        int entityStart = writer.Length;

        writer.Write(networkEntity.Id);

        int flagsPosition = writer.Length;
        writer.Write((byte)0);

        byte flags = 0;

        if (!entityState.SpawnAcknowledged)
        {
            flags |= FLAG_INCLUDES_SPAWN;

            writer.Write(networkEntity.ArchetypeId);
            writer.Write(networkEntity.OwningPeerId);

            registry.GetSpawnRecipe(networkEntity.ArchetypeId).WriteSpawnData(entity, writer);
            entityState.MarkSpawnSent(sequence);
        }

        int componentCountPosition = writer.Length;
        writer.Write((byte)0);

        byte writtenComponents = WriteComponents(entityState, peerId, entity, sequence, forcedFullUpdate);

        if (writtenComponents == 0 && flags == 0 && removedComponents.Count == 0)
        {
            // Nothing to say about this entity, so it is dropped from the message
            writer.Truncate(entityStart);
            return false;
        }

        if (removedComponents.Count > 0)
        {
            flags |= FLAG_INCLUDES_REMOVALS;

            writer.Write((byte)removedComponents.Count);

            for (int i = 0; i < removedComponents.Count; ++i)
            {
                writer.Write(removedComponents[i]);
            }
        }

        writer.OverwriteByte(flagsPosition, flags);
        writer.OverwriteByte(componentCountPosition, writtenComponents);

        if (forcedFullUpdate)
            entityState.LastForcedFullTick = serverTick;

        return true;
    }

    private byte WriteComponents(EntityReplicationState entityState, int peerId, in Entity entity, uint sequence,
        bool forcedFullUpdate)
    {
        var replicators = registry.Replicators;
        byte writtenComponents = 0;

        removedComponents.Clear();

        for (int i = 0; i < replicators.Count; ++i)
        {
            var replicator = replicators[i];
            var componentNetworkId = replicator.ComponentNetworkId;

            if (!replicator.ShouldSend(entity, peerId))
            {
                // The entity lost this component, which the peer has to be told about or it would keep showing
                // the last value forever
                if (entityState.NeedsRemovalSending(componentNetworkId, sequence))
                    removedComponents.Add(componentNetworkId);

                continue;
            }

            componentScratch.Reset();
            replicator.Write(entity, componentScratch);

            var data = componentScratch.WrittenData;

            bool changed = entityState.NeedsSending(componentNetworkId, data, sequence);

            if (!changed && !forcedFullUpdate)
                continue;

            writer.Write(componentNetworkId);
            writer.WriteRaw(data);
            ++writtenComponents;
        }

        return writtenComponents;
    }
}
