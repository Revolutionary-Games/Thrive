using System;
using System.Collections.Generic;

/// <summary>
///   Tracks what one peer is known to have received, so that unchanged data isn't sent again
/// </summary>
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

    public uint AcknowledgedTick { get; private set; }

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

    public void MarkDespawnSent(int index, uint tick)
    {
        var despawn = pendingDespawns[index];

        if (despawn.FirstSentTick == 0)
        {
            despawn.FirstSentTick = tick;
            pendingDespawns[index] = despawn;
        }
    }

    /// <summary>
    ///   Marks everything that was first sent at or before the acknowledged tick as received
    /// </summary>
    public void Acknowledge(uint tick)
    {
        // Acknowledgements can arrive out of order as they travel unreliably
        if (tick <= AcknowledgedTick)
            return;

        AcknowledgedTick = tick;

        foreach (var entry in entities)
        {
            entry.Value.Acknowledge(tick);
        }

        for (int i = pendingDespawns.Count - 1; i >= 0; --i)
        {
            var despawn = pendingDespawns[i];

            if (despawn.FirstSentTick != 0 && despawn.FirstSentTick <= tick)
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
        AcknowledgedTick = 0;
    }

    /// <summary>
    ///   A despawn that is repeated until acknowledged
    /// </summary>
    public struct PendingDespawn(uint networkId)
    {
        public uint NetworkId = networkId;

        /// <summary>
        ///   Tick this was first included in a snapshot, or 0 when it hasn't been sent yet
        /// </summary>
        public uint FirstSentTick = 0;
    }
}

/// <summary>
///   What a peer knows about one entity
/// </summary>
public class EntityReplicationState(int componentTypeCount)
{
    private ComponentBaseline[] components = new ComponentBaseline[componentTypeCount];

    public bool SpawnAcknowledged { get; private set; }

    public uint SpawnFirstSentTick { get; private set; }

    public uint LastForcedFullTick { get; set; }

    public void MarkSpawnSent(uint tick)
    {
        if (SpawnFirstSentTick == 0)
            SpawnFirstSentTick = tick;
    }

    /// <summary>
    ///   Compares new data against what the peer is known to have, and records it as sent when it differs
    /// </summary>
    /// <param name="componentNetworkId">Which component this is</param>
    /// <param name="data">The component in its written form</param>
    /// <param name="tick">Current server tick</param>
    /// <returns>True when this needs to be included in the snapshot</returns>
    public bool NeedsSending(byte componentNetworkId, ReadOnlySpan<byte> data, uint tick)
    {
        EnsureComponentCapacity(componentNetworkId);

        ref var baseline = ref components[componentNetworkId];

        if (baseline.Removed)
        {
            baseline.Removed = false;
        }
        else if (baseline.Data != null && baseline.Length == data.Length &&
                 data.SequenceEqual(new ReadOnlySpan<byte>(baseline.Data, 0, baseline.Length)))
        {
            return !baseline.Acknowledged;
        }

        if (baseline.Data == null || baseline.Data.Length < data.Length)
            baseline.Data = new byte[data.Length];

        data.CopyTo(baseline.Data);
        baseline.Length = data.Length;
        baseline.FirstSentTick = tick;
        baseline.Acknowledged = false;

        return true;
    }

    /// <summary>
    ///   Records that the entity no longer has a component the peer was told about
    /// </summary>
    /// <returns>True when the removal needs to be included in the snapshot</returns>
    public bool NeedsRemovalSending(byte componentNetworkId, uint tick)
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
            baseline.FirstSentTick = tick;
            baseline.Acknowledged = false;
            return true;
        }

        // Like a value, a removal repeats until it is confirmed
        return !baseline.Acknowledged;
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

    public void Acknowledge(uint tick)
    {
        if (!SpawnAcknowledged && SpawnFirstSentTick != 0 && SpawnFirstSentTick <= tick)
            SpawnAcknowledged = true;

        for (int i = 0; i < components.Length; ++i)
        {
            ref var baseline = ref components[i];

            if (!baseline.Acknowledged && baseline.Data != null && baseline.FirstSentTick != 0 &&
                baseline.FirstSentTick <= tick)
            {
                if (baseline.Removed)
                {
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
        public uint FirstSentTick;
        public bool Acknowledged;
        public bool Removed;
    }
}
