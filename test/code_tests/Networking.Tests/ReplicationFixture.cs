namespace ThriveTest.Networking.Tests;

using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Components;

/// <summary>
///   A server world with one replicated component type, plus a client world that applies its snapshots
/// </summary>
public sealed class ReplicationFixture : IDisposable, INetworkEntityDestroyer
{
    private const int PeerId = 2;
    private const int OtherPeerId = 3;

    private readonly World serverWorld = World.Create();
    private readonly World clientWorld = World.Create();

    private readonly ReplicationRegistry registry = new();
    private readonly NetworkedEntityIndex serverIndex = new();
    private readonly NetworkedEntityIndex clientIndex = new();

    private readonly SnapshotWriter snapshotWriter;
    private readonly SnapshotReader snapshotReader;

    private readonly List<Entity> serverEntities = new();
    private readonly NetworkReader reader = new();

    private uint tick;

    public ReplicationFixture()
    {
        registry.RegisterComponent(new TestValueReplicator());
        registry.RegisterSpawnRecipe(new TestSpawnRecipe(clientWorld));
        registry.Seal();

        snapshotWriter = new SnapshotWriter(registry);
        snapshotReader = new SnapshotReader(registry, clientIndex, this);
    }

    public TestInterestManager InterestManager { get; } = new();

    public IReadOnlyList<Entity> Entities => serverEntities;

    /// <summary>
    ///   Tick the next written snapshot will use
    /// </summary>
    public uint NextTick => tick + 1;

    public uint ClientAcknowledgementTick => snapshotReader.LastAppliedTick;

    public IReadOnlyCollection<uint> ClientEntityIds => (IReadOnlyCollection<uint>)clientIndex.Entities.Keys;

    public Entity CreateEntity(int startingId, float value)
    {
        _ = startingId;

        uint networkId = serverIndex.GenerateId();

        var entity = serverWorld.Create(new NetworkEntity(networkId, 0, NetworkConstants.INVALID_PEER_ID),
            new TestValue(value));

        serverIndex.Register(networkId, entity);
        serverEntities.Add(entity);
        return entity;
    }

    public uint NetworkIdOf(in Entity entity)
    {
        return entity.Get<NetworkEntity>().Id;
    }

    public void SetValue(in Entity entity, float value)
    {
        entity.Get<TestValue>().Value = value;
        entity.Set(new TestValue(value));
    }

    /// <summary>
    ///   Takes the replicated component off a server entity, without destroying the entity
    /// </summary>
    public void RemoveValue(in Entity entity)
    {
        entity.Remove<TestValue>();
    }

    public void AddValue(in Entity entity, float value)
    {
        entity.Add(new TestValue(value));
    }

    public bool ClientHasValue(uint networkId)
    {
        return clientIndex.TryGet(networkId, out var entity) && entity.Has<TestValue>();
    }

    public void DespawnEntity(in Entity entity)
    {
        uint networkId = NetworkIdOf(entity);

        serverEntities.Remove(entity);
        serverIndex.Remove(networkId);
        snapshotWriter.OnEntityDespawned(networkId);
        serverWorld.Destroy(entity);
    }

    /// <summary>
    ///   Writes the next snapshot for a peer, advancing the server tick
    /// </summary>
    public ReadOnlySpan<byte> WriteSnapshot(bool otherPeer = false)
    {
        ++tick;
        return snapshotWriter.Write(tick, otherPeer ? OtherPeerId : PeerId, serverEntities, InterestManager);
    }

    /// <summary>
    ///   Same as <see cref="WriteSnapshot"/> but copied out, for holding on to a snapshot while writing another
    /// </summary>
    public byte[] WriteSnapshotCopy(bool otherPeer = false)
    {
        return WriteSnapshot(otherPeer).ToArray();
    }

    /// <summary>
    ///   Acknowledges the newest written tick, standing in for the client's acknowledgement
    /// </summary>
    public void Acknowledge()
    {
        Acknowledge(tick);
    }

    public void Acknowledge(uint acknowledgedTick)
    {
        snapshotWriter.OnAcknowledged(PeerId, acknowledgedTick);
    }

    public void ApplyOnClient(ReadOnlySpan<byte> snapshot)
    {
        if (snapshot.IsEmpty)
            return;

        var data = snapshot.ToArray();
        reader.SetData(data, data.Length);

        // The message type byte is consumed by the session in real use
        reader.ReadMessageType();

        snapshotReader.Apply(reader);
    }

    public float ClientValueOf(uint networkId)
    {
        if (!clientIndex.TryGet(networkId, out var entity))
            throw new InvalidOperationException("Client does not have that entity");

        return entity.Get<TestValue>().Value;
    }

    public void DestroyEntity(in Entity entity)
    {
        clientWorld.Destroy(entity);
    }

    public void Dispose()
    {
        World.Destroy(serverWorld);
        World.Destroy(clientWorld);
    }
}

/// <summary>
///   Test-only replicated component
/// </summary>
public struct TestValue(float value)
{
    public float Value = value;
}

/// <summary>
///   Writes <see cref="TestValue"/> quantized, so that tiny changes compare as equal like a real position
///   component would
/// </summary>
public class TestValueReplicator : IComponentReplicator
{
    public const float MIN_VALUE = -100;
    public const float MAX_VALUE = 100;

    public byte ComponentNetworkId { get; set; }
    public bool Interpolated => true;

    public bool ShouldSend(in Entity entity, int peerId)
    {
        return entity.Has<TestValue>();
    }

    public void Write(in Entity entity, NetworkWriter writer)
    {
        writer.WriteQuantized16(entity.Get<TestValue>().Value, MIN_VALUE, MAX_VALUE);
    }

    public void Read(in Entity entity, NetworkReader reader)
    {
        var value = reader.ReadQuantized16(MIN_VALUE, MAX_VALUE);

        if (entity.Has<TestValue>())
        {
            entity.Get<TestValue>().Value = value;
            entity.Set(new TestValue(value));
        }
        else
        {
            entity.Add(new TestValue(value));
        }
    }

    public void Skip(NetworkReader reader)
    {
        reader.ReadUInt16();
    }

    public void Remove(in Entity entity)
    {
        if (entity.Has<TestValue>())
            entity.Remove<TestValue>();
    }
}

/// <summary>
///   Creates the client side copy of a test entity
/// </summary>
public class TestSpawnRecipe(World clientWorld) : INetworkSpawnRecipe
{
    public ushort ArchetypeId { get; set; }

    public void WriteSpawnData(in Entity entity, NetworkWriter writer)
    {
        // A stand-in for something like a species reference
        writer.Write(entity.Get<NetworkEntity>().Id);
    }

    public void SkipSpawnData(NetworkReader reader)
    {
        reader.ReadUInt32();
    }

    public Entity Spawn(NetworkReader reader, in NetworkEntity networkEntity)
    {
        reader.ReadUInt32();

        return clientWorld.Create(networkEntity, new TestValue(0));
    }
}

/// <summary>
///   Interest manager that can hide chosen entities, for testing what happens when one leaves and returns
/// </summary>
public class TestInterestManager : IInterestManager
{
    public HashSet<uint> Hidden { get; } = new();

    public void PrepareForPeer(int peerId, in Entity peerEntity)
    {
    }

    public bool IsRelevant(int peerId, in Entity entity)
    {
        return !Hidden.Contains(entity.Get<NetworkEntity>().Id);
    }
}
