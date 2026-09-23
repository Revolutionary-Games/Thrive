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
/// </remarks>
public class SnapshotWriter
{
    /// <summary>
    ///   Set in the per-entity flags when spawn data is included
    /// </summary>
    public const byte FLAG_INCLUDES_SPAWN = 1;

    private readonly ReplicationRegistry registry;

    private readonly NetworkWriter writer = new();

    /// <summary>
    ///   Component data is written here first so it can be compared against the baseline before deciding to
    ///   include it
    /// </summary>
    private readonly NetworkWriter componentScratch = new(256);

    private readonly Dictionary<int, PeerReplicationState> peerStates = new();

    private readonly HashSet<uint> relevantIds = new();

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

        writer.Reset();
        writer.Write(MessageType.Snapshot);
        writer.Write(serverTick);
        writer.Write(peerState.AcknowledgedTick);

        WriteDespawns(peerState, serverTick);

        int countPosition = writer.Length;
        writer.Write((ushort)0);

        ushort writtenEntities = 0;
        relevantIds.Clear();

        for (int i = 0; i < entities.Count; ++i)
        {
            var entity = entities[i];

            if (!entity.IsAliveAndHas<NetworkEntity>())
                continue;

            ref var networkEntity = ref entity.Get<NetworkEntity>();

            if (!interestManager.IsRelevant(peerId, entity))
                continue;

            relevantIds.Add(networkEntity.Id);

            if (WriteEntity(peerState, peerId, entity, in networkEntity, serverTick))
                ++writtenEntities;
        }

        // An entity that stopped being relevant has to be sent in full when it returns, as the peer may have
        // missed any number of changes in the meantime
        peerState.ForgetEntitiesOutsideOf(relevantIds);

        writer.OverwriteUInt16(countPosition, writtenEntities);

        if (writtenEntities == 0 && peerState.PendingDespawns.Count == 0)
            return ReadOnlySpan<byte>.Empty;

        return writer.WrittenData;
    }

    /// <summary>
    ///   Applies an acknowledgement from a peer, which is what allows data to stop being sent
    /// </summary>
    public void OnAcknowledged(int peerId, uint tick)
    {
        if (peerStates.TryGetValue(peerId, out var state))
            state.Acknowledge(tick);
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

    private void WriteDespawns(PeerReplicationState peerState, uint serverTick)
    {
        var despawns = peerState.PendingDespawns;

        writer.Write((ushort)despawns.Count);

        for (int i = 0; i < despawns.Count; ++i)
        {
            writer.Write(despawns[i].NetworkId);
            peerState.MarkDespawnSent(i, serverTick);
        }
    }

    /// <summary>
    ///   Writes one entity, or nothing at all when the peer is already up to date on it
    /// </summary>
    /// <returns>True when the entity was written</returns>
    private bool WriteEntity(PeerReplicationState peerState, int peerId, in Entity entity,
        in NetworkEntity networkEntity, uint serverTick)
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
            entityState.MarkSpawnSent(serverTick);
        }

        int componentCountPosition = writer.Length;
        writer.Write((byte)0);

        byte writtenComponents = WriteComponents(entityState, peerId, entity, serverTick, forcedFullUpdate);

        if (writtenComponents == 0 && flags == 0)
        {
            // Nothing to say about this entity, so it is dropped from the message
            writer.Truncate(entityStart);
            return false;
        }

        writer.OverwriteByte(flagsPosition, flags);
        writer.OverwriteByte(componentCountPosition, writtenComponents);

        if (forcedFullUpdate)
            entityState.LastForcedFullTick = serverTick;

        return true;
    }

    private byte WriteComponents(EntityReplicationState entityState, int peerId, in Entity entity, uint serverTick,
        bool forcedFullUpdate)
    {
        var replicators = registry.Replicators;
        byte writtenComponents = 0;

        for (int i = 0; i < replicators.Count; ++i)
        {
            var replicator = replicators[i];
            var componentNetworkId = replicator.ComponentNetworkId;

            if (!replicator.ShouldSend(entity, peerId))
            {
                // The entity doesn't have this component now, so a later addition of it must be sent again
                if (entityState.HasComponentData(componentNetworkId))
                    entityState.ForgetComponent(componentNetworkId);

                continue;
            }

            componentScratch.Reset();
            replicator.Write(entity, componentScratch);

            var data = componentScratch.WrittenData;

            bool changed = entityState.NeedsSending(componentNetworkId, data, serverTick);

            if (!changed && !forcedFullUpdate)
                continue;

            writer.Write(componentNetworkId);
            writer.WriteRaw(data);
            ++writtenComponents;
        }

        return writtenComponents;
    }
}
