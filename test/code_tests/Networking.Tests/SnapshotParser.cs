namespace ThriveTest.Networking.Tests;

using System;
using System.Collections.Generic;

/// <summary>
///   Reads a snapshot message structurally, so tests can assert on what a snapshot contains without applying it
/// </summary>
public static class SnapshotParser
{
    public static ParsedSnapshot Parse(ReadOnlySpan<byte> snapshot)
    {
        if (snapshot.IsEmpty)
            throw new ArgumentException("Snapshot is empty", nameof(snapshot));

        var data = snapshot.ToArray();

        var reader = new NetworkReader();
        reader.SetData(data, data.Length);

        var type = reader.ReadMessageType();

        if (type != MessageType.Snapshot)
            throw new InvalidOperationException("Message is not a snapshot");

        var result = new ParsedSnapshot
        {
            Sequence = reader.ReadUInt32(),
            ServerTick = reader.ReadUInt32(),
        };

        int despawnCount = reader.ReadUInt16();

        for (int i = 0; i < despawnCount; ++i)
        {
            result.DespawnedIds.Add(reader.ReadUInt32());
        }

        int entityCount = reader.ReadUInt16();

        for (int i = 0; i < entityCount; ++i)
        {
            var entity = new ParsedEntity
            {
                NetworkId = reader.ReadUInt32(),
            };

            byte flags = reader.ReadByte();
            entity.HasSpawn = (flags & SnapshotWriter.FLAG_INCLUDES_SPAWN) != 0;

            if (entity.HasSpawn)
            {
                entity.ArchetypeId = reader.ReadUInt16();
                entity.OwningPeerId = reader.ReadInt32();

                // The test spawn payload is a single uint, see TestSpawnRecipe
                reader.ReadUInt32();
            }

            entity.ComponentCount = reader.ReadByte();

            for (int j = 0; j < entity.ComponentCount; ++j)
            {
                entity.ComponentIds.Add(reader.ReadByte());

                // The test component is a single quantized value
                reader.ReadUInt16();
            }

            if ((flags & SnapshotWriter.FLAG_INCLUDES_REMOVALS) != 0)
            {
                int removedCount = reader.ReadByte();

                for (int j = 0; j < removedCount; ++j)
                {
                    entity.RemovedComponentIds.Add(reader.ReadByte());
                }
            }

            result.Entities.Add(entity);
        }

        if (!reader.AtEnd)
            throw new InvalidOperationException("Snapshot had unread data left, the layout does not match");

        return result;
    }
}

public class ParsedSnapshot
{
    public uint Sequence { get; set; }
    public uint ServerTick { get; set; }
    public List<uint> DespawnedIds { get; } = new();
    public List<ParsedEntity> Entities { get; } = new();
}

public class ParsedEntity
{
    public uint NetworkId { get; set; }
    public bool HasSpawn { get; set; }
    public ushort ArchetypeId { get; set; }
    public int OwningPeerId { get; set; }
    public int ComponentCount { get; set; }
    public List<byte> ComponentIds { get; } = new();
    public List<byte> RemovedComponentIds { get; } = new();
}
