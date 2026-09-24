using System;
using System.Collections.Generic;

/// <summary>
///   Tracks what one peer is known to have received, so that unchanged data isn't sent again
/// </summary>
/// <remarks>
///   <para>
///     Confirmation is given to the packet sequence an item was sent in, not to a world tick. A tick can span
///     more than one packet once there is more data than fits in a single one, and a peer that received some
///     of those packets would otherwise confirm data it never got.
///   </para>
///   <para>
///     An item is confirmed when the peer acknowledges a sequence at or after the one it was first sent in.
///     That works only because an unconfirmed item is included in *every* packet from then on. When the
///     packet size cap forces an item to be left out, it is marked deferred and restamped the next time it is
///     actually included, which restores that property.
///   </para>
/// </remarks>
public class PeerReplicationState
{
    private readonly int componentTypeCount;

    private readonly Dictionary<uint, EntityReplicationState> entities = new();
    private readonly List<PendingDespawn> pendingDespawns = new();

    private readonly List<uint> temporaryIdList = new();

    public PeerReplicationState(int componentTypeCount)
    {
        this.componentTypeCount = componentTypeCount;
    }

    public uint AcknowledgedSequence { get; private set; }

    /// <summary>
    ///   Sequence the next packet sent to this peer will use. Only advances when a packet is actually sent.
    /// </summary>
    public uint NextSequence { get; private set; } = 1;

    /// <summary>
    ///   Where the entity iteration starts next time, so that a full packet doesn't starve the same entities
    ///   every time
    /// </summary>
    public int SendRotationOffset { get; set; }

    public IReadOnlyList<PendingDespawn> PendingDespawns => pendingDespawns;

    public EntityReplicationState GetOrCreate(uint networkId)
    {
        if (entities.TryGetValue(networkId, out var state))
            return state;

        state = new EntityReplicationState(componentTypeCount);
        entities[networkId] = state;
        return state;
    }

    public bool TryGet(uint networkId, out EntityReplicationState state)
    {
        return entities.TryGetValue(networkId, out state!);
    }

    public void Forget(uint networkId)
    {
        entities.Remove(networkId);
    }

    public void QueueDespawn(uint networkId)
    {
        entities.Remove(networkId);

        for (int i = 0; i < pendingDespawns.Count; ++i)
        {
            if (pendingDespawns[i].NetworkId == networkId)
                return;
        }

        pendingDespawns.Add(new PendingDespawn(networkId));
    }

    public void MarkDespawnSent(int index, uint sequence)
    {
        var despawn = pendingDespawns[index];

        if (despawn.FirstSentSequence == 0 || despawn.Deferred)
        {
            despawn.FirstSentSequence = sequence;
            despawn.Deferred = false;
            pendingDespawns[index] = despawn;
        }
    }

    /// <summary>
    ///   Records that a despawn had to be left out of a packet that was sent
    /// </summary>
    public void MarkDespawnDeferred(int index)
    {
        var despawn = pendingDespawns[index];

        if (despawn.FirstSentSequence == 0)
            return;

        despawn.Deferred = true;
        pendingDespawns[index] = despawn;
    }

    /// <summary>
    ///   Called when a packet was actually sent, so the next one gets a new sequence
    /// </summary>
    public void OnPacketSent()
    {
        ++NextSequence;
    }

    /// <summary>
    ///   Marks everything sent at or before the acknowledged sequence as received
    /// </summary>
    public void Acknowledge(uint sequence)
    {
        // Acknowledgements travel unreliably, so they can arrive out of order
        if (sequence <= AcknowledgedSequence)
            return;

        AcknowledgedSequence = sequence;

        foreach (var entry in entities)
        {
            entry.Value.Acknowledge(sequence);
        }

        for (int i = pendingDespawns.Count - 1; i >= 0; --i)
        {
            var despawn = pendingDespawns[i];

            if (!despawn.Deferred && despawn.FirstSentSequence != 0 && despawn.FirstSentSequence <= sequence)
                pendingDespawns.RemoveAt(i);
        }
    }

    /// <summary>
    ///   Forgets entities that are no longer in the given set of relevant IDs, as a peer that stops seeing an
    ///   entity must be sent its full state again when it comes back into view
    /// </summary>
    public void ForgetEntitiesOutsideOf(HashSet<uint> relevantIds)
    {
        temporaryIdList.Clear();

        foreach (var entry in entities)
        {
            if (!relevantIds.Contains(entry.Key))
                temporaryIdList.Add(entry.Key);
        }

        for (int i = 0; i < temporaryIdList.Count; ++i)
        {
            entities.Remove(temporaryIdList[i]);
        }
    }

    public void Clear()
    {
        entities.Clear();
        pendingDespawns.Clear();
        AcknowledgedSequence = 0;
        NextSequence = 1;
        SendRotationOffset = 0;
    }

    /// <summary>
    ///   A despawn that is repeated until acknowledged
    /// </summary>
    public struct PendingDespawn(uint networkId)
    {
        public uint NetworkId = networkId;

        /// <summary>
        ///   Packet sequence this was first included in, or 0 when it hasn't been sent yet
        /// </summary>
        public uint FirstSentSequence = 0;

        public bool Deferred = false;
    }
}

/// <summary>
///   What a peer knows about one entity
/// </summary>
public class EntityReplicationState(int componentTypeCount)
{
    private ComponentBaseline[] components = new ComponentBaseline[componentTypeCount];

    public bool SpawnAcknowledged { get; private set; }

    public uint SpawnFirstSentSequence { get; private set; }

    public bool SpawnDeferred { get; private set; }

    /// <summary>
    ///   Tick of the last resend of everything, used to recover from any tracking mistake.
    /// </summary>
    public uint LastForcedFullTick { get; set; }

    public void MarkSpawnSent(uint sequence)
    {
        if (SpawnFirstSentSequence == 0 || SpawnDeferred)
        {
            SpawnFirstSentSequence = sequence;
            SpawnDeferred = false;
        }
    }

    /// <summary>
    ///   Compares new data against what the peer is known to have, and records it as sent when it differs
    /// </summary>
    /// <param name="componentNetworkId">Which component this is</param>
    /// <param name="data">The component in its written form</param>
    /// <param name="sequence">Sequence of the packet being built</param>
    /// <returns>True when this needs to be included in the packet</returns>
    public bool NeedsSending(byte componentNetworkId, ReadOnlySpan<byte> data, uint sequence)
    {
        EnsureComponentCapacity(componentNetworkId);

        ref var baseline = ref components[componentNetworkId];

        if (baseline.Removed)
        {
            // The component came back before its removal was confirmed, so the value has to go out again even
            // if it is the same one the peer had before the removal
            baseline.Removed = false;
        }
        else if (baseline.Data != null && baseline.Length == data.Length &&
                 data.SequenceEqual(new ReadOnlySpan<byte>(baseline.Data, 0, baseline.Length)))
        {
            if (baseline.Acknowledged)
                return false;

            // Same value, still unconfirmed, so it repeats. A value that missed a packet has to be restamped,
            // otherwise an acknowledgement of that packet would wrongly confirm it.
            if (baseline.Deferred)
            {
                baseline.FirstSentSequence = sequence;
                baseline.Deferred = false;
            }

            return true;
        }

        if (baseline.Data == null || baseline.Data.Length < data.Length)
            baseline.Data = new byte[data.Length];

        data.CopyTo(baseline.Data);
        baseline.Length = data.Length;
        baseline.FirstSentSequence = sequence;
        baseline.Deferred = false;
        baseline.Acknowledged = false;

        return true;
    }

    /// <summary>
    ///   Records that the entity no longer has a component the peer was told about
    /// </summary>
    /// <returns>True when the removal needs to be included in the packet</returns>
    public bool NeedsRemovalSending(byte componentNetworkId, uint sequence)
    {
        if (componentNetworkId >= components.Length)
            return false;

        ref var baseline = ref components[componentNetworkId];

        // Nothing to remove if the peer was never told about this component in the first place
        if (baseline.Data == null)
            return false;

        if (!baseline.Removed)
        {
            baseline.Removed = true;
            baseline.FirstSentSequence = sequence;
            baseline.Deferred = false;
            baseline.Acknowledged = false;
            return true;
        }

        if (baseline.Acknowledged)
            return false;

        if (baseline.Deferred)
        {
            baseline.FirstSentSequence = sequence;
            baseline.Deferred = false;
        }

        // Like a value, a removal repeats until it is confirmed
        return true;
    }

    /// <summary>
    ///   Records that everything unconfirmed about this entity was left out of a packet that was sent
    /// </summary>
    public void MarkDeferred()
    {
        if (!SpawnAcknowledged && SpawnFirstSentSequence != 0)
            SpawnDeferred = true;

        for (int i = 0; i < components.Length; ++i)
        {
            ref var baseline = ref components[i];

            if (!baseline.Acknowledged && baseline.Data != null && baseline.FirstSentSequence != 0)
                baseline.Deferred = true;
        }
    }

    /// <summary>
    ///   Forgets a component, so that it is sent in full if the entity gains it again
    /// </summary>
    public void ForgetComponent(byte componentNetworkId)
    {
        if (componentNetworkId >= components.Length)
            return;

        components[componentNetworkId] = default(ComponentBaseline);
    }

    public bool HasComponentData(byte componentNetworkId)
    {
        return componentNetworkId < components.Length && components[componentNetworkId].Data != null;
    }

    public void Acknowledge(uint sequence)
    {
        if (!SpawnAcknowledged && !SpawnDeferred && SpawnFirstSentSequence != 0 &&
            SpawnFirstSentSequence <= sequence)
        {
            SpawnAcknowledged = true;
        }

        for (int i = 0; i < components.Length; ++i)
        {
            ref var baseline = ref components[i];

            if (!baseline.Acknowledged && !baseline.Deferred && baseline.Data != null &&
                baseline.FirstSentSequence != 0 && baseline.FirstSentSequence <= sequence)
            {
                if (baseline.Removed)
                {
                    // The peer knows the component is gone, so this can be forgotten entirely. Gaining the
                    // component again then sends it as something new.
                    baseline = default(ComponentBaseline);
                }
                else
                {
                    baseline.Acknowledged = true;
                }
            }
        }
    }

    private void EnsureComponentCapacity(byte componentNetworkId)
    {
        if (componentNetworkId < components.Length)
            return;

        Array.Resize(ref components, componentNetworkId + 1);
    }

    private struct ComponentBaseline
    {
        public byte[]? Data;
        public int Length;
        public uint FirstSentSequence;
        public bool Acknowledged;
        public bool Removed;
        public bool Deferred;
    }
}
