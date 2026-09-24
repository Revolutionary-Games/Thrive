namespace ThriveTest.Networking.Tests;

using System;
using System.Collections.Generic;
using Xunit;

/// <summary>
///   Tests packet sequencing and the packet size cap, which is what makes acknowledgements sound once there is
///   more data than fits in one packet
/// </summary>
public class SnapshotPacketTests
{
    /// <summary>
    ///   Enough entities that one packet cannot hold them all
    /// </summary>
    private const int ManyEntities = 200;

    [Fact]
    public void Snapshot_PacketsCarryIncreasingSequences()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);

        uint first = SnapshotParser.Parse(setup.WriteSnapshot()).Sequence;

        setup.SetValue(entity, 2.0f);
        uint second = SnapshotParser.Parse(setup.WriteSnapshot()).Sequence;

        Assert.True(second > first, "Each sent packet must use a new sequence");
    }

    /// <summary>
    ///   A packet that isn't sent must not burn a sequence, otherwise acknowledgements would refer to packets
    ///   that never existed
    /// </summary>
    [Fact]
    public void Snapshot_SkippedPacketDoesNotConsumeSequence()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);

        setup.WriteSnapshot();
        setup.Acknowledge();

        Assert.True(setup.WriteSnapshot().IsEmpty);
        Assert.True(setup.WriteSnapshot().IsEmpty);

        setup.SetValue(entity, 5.0f);

        var parsed = SnapshotParser.Parse(setup.WriteSnapshot());

        Assert.Equal(2u, parsed.Sequence);
    }

    [Fact]
    public void Snapshot_PacketStaysWithinTheSizeCap()
    {
        using var setup = new ReplicationFixture();
        CreateEntities(setup, ManyEntities);

        var snapshot = setup.WriteSnapshotCopy();

        // The cap is checked after each entity, so one entity's worth of overshoot is expected
        Assert.True(snapshot.Length < NetworkConstants.SNAPSHOT_MAX_PACKET_SIZE + 64,
            $"Packet of {snapshot.Length} bytes is past the cap");

        Assert.True(SnapshotParser.Parse(snapshot).Entities.Count < ManyEntities,
            "Not all entities should fit in one packet");
    }

    /// <summary>
    ///   The core of why sequences are needed: an entity left out of a packet must not be confirmed by an
    ///   acknowledgement of that packet
    /// </summary>
    [Fact]
    public void Snapshot_EntityLeftOutOfAPacketIsNotConfirmedByIt()
    {
        using var setup = new ReplicationFixture();
        CreateEntities(setup, ManyEntities);

        var firstPacket = SnapshotParser.Parse(setup.WriteSnapshotCopy());

        // The client confirms the packet it received, which held only some of the entities
        setup.Acknowledge();

        var seen = new HashSet<uint>();
        CollectEntityIds(firstPacket, seen);

        // Everything that was left out must still be waiting to go
        var secondPacket = SnapshotParser.Parse(setup.WriteSnapshotCopy());

        Assert.NotEmpty(secondPacket.Entities);

        foreach (var entity in secondPacket.Entities)
        {
            Assert.DoesNotContain(entity.NetworkId, seen);
        }
    }

    /// <summary>
    ///   An entity sent in a packet that was never confirmed, and then left out of a later packet that was
    ///   confirmed, must still be delivered. Confirming it on the strength of the later packet would strand it
    ///   on the client forever.
    /// </summary>
    [Fact]
    public void Snapshot_UnconfirmedEntitiesSurviveBeingLeftOutOfALaterPacket()
    {
        using var setup = new ReplicationFixture();
        CreateEntities(setup, ManyEntities);

        var expected = new Dictionary<uint, float>();

        foreach (var entity in setup.Entities)
        {
            expected[setup.NetworkIdOf(entity)] = setup.ServerValueOf(entity);
        }

        // The first packet is lost: neither applied nor acknowledged
        setup.WriteSnapshotCopy();

        // Everything after it arrives normally
        for (int i = 0; i < 40; ++i)
        {
            var snapshot = setup.WriteSnapshotCopy();

            if (snapshot.Length == 0)
                break;

            setup.ApplyOnClient(snapshot);
            setup.Acknowledge(setup.ClientAcknowledgementSequence);
        }

        foreach (var entry in expected)
        {
            Assert.True(setup.ClientHasValue(entry.Key),
                $"Entity {entry.Key} was never delivered after its first packet was lost");

            Assert.Equal(entry.Value, setup.ClientValueOf(entry.Key), 2);
        }
    }

    /// <summary>
    ///   Every entity must eventually be sent, rather than the same ones winning the space every time
    /// </summary>
    [Fact]
    public void Snapshot_AllEntitiesAreSentEventually()
    {
        using var setup = new ReplicationFixture();
        CreateEntities(setup, ManyEntities);

        var seen = new HashSet<uint>();

        for (int i = 0; i < 20; ++i)
        {
            var snapshot = setup.WriteSnapshot();

            if (snapshot.IsEmpty)
                break;

            CollectEntityIds(SnapshotParser.Parse(snapshot), seen);
            setup.Acknowledge();
        }

        Assert.Equal(ManyEntities, seen.Count);
    }

    /// <summary>
    ///   With everything confirmed and nothing changing, a full arena should cost nothing at all
    /// </summary>
    [Fact]
    public void Snapshot_IdleWorldEventuallyCostsNothing()
    {
        using var setup = new ReplicationFixture();
        CreateEntities(setup, ManyEntities);

        bool quiet = false;

        for (int i = 0; i < 50; ++i)
        {
            if (setup.WriteSnapshot().IsEmpty)
            {
                quiet = true;
                break;
            }

            setup.Acknowledge();
        }

        Assert.True(quiet, "An unchanging world should stop producing packets once everything is confirmed");
    }

    private static void CreateEntities(ReplicationFixture setup, int count)
    {
        for (int i = 0; i < count; ++i)
        {
            setup.CreateEntity(i, i % 50);
        }
    }

    private static void CollectEntityIds(ParsedSnapshot snapshot, HashSet<uint> into)
    {
        for (int i = 0; i < snapshot.Entities.Count; ++i)
        {
            into.Add(snapshot.Entities[i].NetworkId);
        }
    }
}
