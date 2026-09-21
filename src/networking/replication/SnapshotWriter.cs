using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Components;

/// <summary>
///   Builds the per-peer snapshot messages a server sends
/// </summary>
public class SnapshotWriter
{
    private readonly ReplicationRegistry registry;
    private readonly NetworkWriter writer = new();

    public SnapshotWriter(ReplicationRegistry registry)
    {
        this.registry = registry;
    }

    /// <summary>
    ///   Writes a snapshot for one peer
    /// </summary>
    /// <param name="serverTick">Tick the server is on</param>
    /// <param name="baselineTick">
    ///   Newest tick this peer acknowledged, or 0 when it has nothing to delta against
    /// </param>
    /// <param name="peerId">Peer this snapshot is for</param>
    /// <param name="entities">Entities to consider, filtered by the interest manager</param>
    /// <param name="interestManager">Decides which of the entities this peer gets</param>
    /// <returns>The written message data</returns>
    public ReadOnlySpan<byte> Write(uint serverTick, uint baselineTick, int peerId,
        List<Entity> entities, IInterestManager interestManager)
    {
        writer.Reset();
        writer.Write(MessageType.Snapshot);
        writer.Write(serverTick);
        writer.Write(baselineTick);

        int countPosition = writer.Length;
        writer.Write((ushort)0);

        ushort writtenEntities = 0;
        var replicators = registry.Replicators;

        for (int i = 0; i < entities.Count; ++i)
        {
            var entity = entities[i];

            if (!entity.IsAliveAndHas<NetworkEntity>())
                continue;

            if (!interestManager.IsRelevant(peerId, entity))
                continue;

            ref var networkEntity = ref entity.Get<NetworkEntity>();

            writer.Write(networkEntity.Id);

            int componentCountPosition = writer.Length;
            writer.Write((byte)0);

            byte writtenComponents = 0;

            for (int j = 0; j < replicators.Count; ++j)
            {
                var replicator = replicators[j];

                if (!replicator.ShouldSend(entity, peerId))
                    continue;

                writer.Write(replicator.ComponentNetworkId);
                replicator.Write(entity, writer);
                ++writtenComponents;
            }

            writer.OverwriteByte(componentCountPosition, writtenComponents);
            ++writtenEntities;
        }

        writer.OverwriteUInt16(countPosition, writtenEntities);

        return writer.WrittenData;
    }
}
