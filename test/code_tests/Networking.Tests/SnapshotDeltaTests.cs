namespace ThriveTest.Networking.Tests;

using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using Xunit;

/// <summary>
///   Tests that snapshots only carry what a peer is not known to have already
/// </summary>
public class SnapshotDeltaTests
{
    private const int PeerId = 2;

    [Fact]
    public void Snapshot_FirstSnapshotContainsSpawnAndComponents()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);

        var snapshot = setup.WriteSnapshot();

        Assert.False(snapshot.IsEmpty);

        var parsed = SnapshotParser.Parse(snapshot);

        Assert.Single(parsed.Entities);
        Assert.True(parsed.Entities[0].HasSpawn);
        Assert.Equal(1, parsed.Entities[0].ComponentCount);
        Assert.Equal(setup.NetworkIdOf(entity), parsed.Entities[0].NetworkId);
    }

    /// <summary>
    ///   The point of the whole exercise: a still entity stops costing bandwidth once the client confirms it
    /// </summary>
    [Fact]
    public void Snapshot_UnchangedComponentIsNotResentAfterAcknowledgement()
    {
        using var setup = new ReplicationFixture();
        setup.CreateEntity(10, 1.0f);

        setup.WriteSnapshot();
        setup.Acknowledge();

        Assert.True(setup.WriteSnapshot().IsEmpty);
    }

    /// <summary>
    ///   Until the acknowledgement arrives the server cannot know the client got the value, so it must keep
    ///   sending. This is the case that a naive "send once then stop" implementation gets wrong.
    /// </summary>
    [Fact]
    public void Snapshot_ValueKeepsBeingSentWhileUnacknowledged()
    {
        using var setup = new ReplicationFixture();
        setup.CreateEntity(10, 1.0f);

        for (int i = 0; i < 5; ++i)
        {
            var snapshot = setup.WriteSnapshot();

            Assert.False(snapshot.IsEmpty);
            Assert.Equal(1, SnapshotParser.Parse(snapshot).Entities[0].ComponentCount);
        }

        setup.Acknowledge();

        Assert.True(setup.WriteSnapshot().IsEmpty);
    }

    /// <summary>
    ///   An acknowledgement of a tick before the value was first sent says nothing about that value
    /// </summary>
    [Fact]
    public void Snapshot_OldAcknowledgementDoesNotConfirmNewerValue()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);

        setup.WriteSnapshot();
        setup.Acknowledge();

        setup.SetValue(entity, 2.0f);
        uint tickOfChange = setup.NextTick;
        setup.WriteSnapshot();

        // The client confirms an older tick, from before the change was sent
        setup.Acknowledge(tickOfChange - 1);

        Assert.False(setup.WriteSnapshot().IsEmpty);

        setup.Acknowledge(tickOfChange);

        Assert.True(setup.WriteSnapshot().IsEmpty);
    }

    [Fact]
    public void Snapshot_ChangedComponentIsSentAgain()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);

        setup.WriteSnapshot();
        setup.Acknowledge();

        setup.SetValue(entity, 5.0f);

        var snapshot = setup.WriteSnapshot();
        var parsed = SnapshotParser.Parse(snapshot);

        Assert.Single(parsed.Entities);
        Assert.False(parsed.Entities[0].HasSpawn);
        Assert.Equal(1, parsed.Entities[0].ComponentCount);
    }

    /// <summary>
    ///   Values are compared after quantization, otherwise float noise in a resting entity would defeat the
    ///   whole optimization
    /// </summary>
    [Fact]
    public void Snapshot_ChangeSmallerThanQuantizationIsNotSent()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);

        setup.WriteSnapshot();
        setup.Acknowledge();

        setup.SetValue(entity, 1.0f + 0.00001f);

        Assert.True(setup.WriteSnapshot().IsEmpty);
    }

    [Fact]
    public void Snapshot_SpawnIsRepeatedUntilAcknowledged()
    {
        using var setup = new ReplicationFixture();
        setup.CreateEntity(10, 1.0f);

        Assert.True(SnapshotParser.Parse(setup.WriteSnapshot()).Entities[0].HasSpawn);
        Assert.True(SnapshotParser.Parse(setup.WriteSnapshot()).Entities[0].HasSpawn);

        setup.Acknowledge();
        setup.SetValue(setup.Entities[0], 3.0f);

        Assert.False(SnapshotParser.Parse(setup.WriteSnapshot()).Entities[0].HasSpawn);
    }

    [Fact]
    public void Snapshot_DespawnIsRepeatedUntilAcknowledged()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);

        setup.WriteSnapshot();
        setup.Acknowledge();

        setup.DespawnEntity(entity);

        Assert.Single(SnapshotParser.Parse(setup.WriteSnapshot()).DespawnedIds);
        Assert.Single(SnapshotParser.Parse(setup.WriteSnapshot()).DespawnedIds);

        setup.Acknowledge();

        Assert.True(setup.WriteSnapshot().IsEmpty);
    }

    /// <summary>
    ///   A peer that stops seeing an entity may miss any number of changes, so it needs everything again when
    ///   the entity comes back
    /// </summary>
    [Fact]
    public void Snapshot_EntityReturningToInterestIsSentInFull()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);

        setup.WriteSnapshot();
        setup.Acknowledge();

        setup.InterestManager.Hidden.Add(setup.NetworkIdOf(entity));
        Assert.True(setup.WriteSnapshot().IsEmpty);

        setup.InterestManager.Hidden.Clear();

        var parsed = SnapshotParser.Parse(setup.WriteSnapshot());

        Assert.Single(parsed.Entities);
        Assert.True(parsed.Entities[0].HasSpawn);
        Assert.Equal(1, parsed.Entities[0].ComponentCount);
    }

    /// <summary>
    ///   Insurance against a tracking bug leaving a client permanently wrong
    /// </summary>
    [Fact]
    public void Snapshot_StateIsResentPeriodicallyEvenWhenUnchanged()
    {
        using var setup = new ReplicationFixture();
        setup.CreateEntity(10, 1.0f);

        setup.WriteSnapshot();
        setup.Acknowledge();

        bool resent = false;

        for (uint i = 0; i < NetworkConstants.SNAPSHOT_FORCED_FULL_UPDATE_TICKS + 2; ++i)
        {
            if (!setup.WriteSnapshot().IsEmpty)
            {
                resent = true;
                break;
            }
        }

        Assert.True(resent, "State should be resent periodically as a recovery measure");
    }

    [Fact]
    public void Snapshot_SeparatePeersTrackedIndependently()
    {
        using var setup = new ReplicationFixture();
        setup.CreateEntity(10, 1.0f);

        setup.WriteSnapshot();
        setup.WriteSnapshot(otherPeer: true);

        // Only one peer confirms
        setup.Acknowledge();

        Assert.True(setup.WriteSnapshot().IsEmpty);
        Assert.False(setup.WriteSnapshot(otherPeer: true).IsEmpty);
    }

    /// <summary>
    ///   A client applying the snapshots must end up with what the server has, which is what makes skipping
    ///   data safe
    /// </summary>
    [Fact]
    public void Snapshot_ClientEndsUpWithServerValuesAfterDeltaUpdates()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);

        setup.ApplyOnClient(setup.WriteSnapshot());
        setup.Acknowledge(setup.ClientAcknowledgementTick);

        Assert.Equal(1.0f, setup.ClientValueOf(setup.NetworkIdOf(entity)), 2);

        setup.SetValue(entity, 7.5f);
        setup.ApplyOnClient(setup.WriteSnapshot());
        setup.Acknowledge(setup.ClientAcknowledgementTick);

        Assert.Equal(7.5f, setup.ClientValueOf(setup.NetworkIdOf(entity)), 2);

        // Nothing more is sent, and the client keeps the value it already has
        Assert.True(setup.WriteSnapshot().IsEmpty);
        Assert.Equal(7.5f, setup.ClientValueOf(setup.NetworkIdOf(entity)), 2);
    }

    /// <summary>
    ///   Repeated spawn data must not create a second copy of an entity
    /// </summary>
    [Fact]
    public void Snapshot_RepeatedSpawnDoesNotDuplicateClientEntity()
    {
        using var setup = new ReplicationFixture();
        setup.CreateEntity(10, 1.0f);

        setup.ApplyOnClient(setup.WriteSnapshot());

        // The acknowledgement never reaches the server, so it sends the spawn again
        setup.ApplyOnClient(setup.WriteSnapshot());

        Assert.Single(setup.ClientEntityIds);
    }

    [Fact]
    public void Snapshot_DespawnRemovesTheClientEntity()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);

        setup.ApplyOnClient(setup.WriteSnapshot());
        Assert.Single(setup.ClientEntityIds);

        setup.DespawnEntity(entity);
        setup.ApplyOnClient(setup.WriteSnapshot());

        Assert.Empty(setup.ClientEntityIds);
    }

    /// <summary>
    ///   A component taken off an entity has to be signalled, or the client would show the last value forever
    /// </summary>
    [Fact]
    public void Snapshot_ComponentRemovalIsSentAndApplied()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);
        uint networkId = setup.NetworkIdOf(entity);

        setup.ApplyOnClient(setup.WriteSnapshot());
        setup.Acknowledge();
        Assert.True(setup.ClientHasValue(networkId));

        setup.RemoveValue(entity);

        var snapshot = setup.WriteSnapshotCopy();
        var parsed = SnapshotParser.Parse(snapshot);

        Assert.Single(parsed.Entities);
        Assert.Equal(0, parsed.Entities[0].ComponentCount);
        Assert.Single(parsed.Entities[0].RemovedComponentIds);

        setup.ApplyOnClient(snapshot);

        Assert.False(setup.ClientHasValue(networkId));
    }

    [Fact]
    public void Snapshot_RemovalIsRepeatedUntilAcknowledged()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);

        setup.WriteSnapshot();
        setup.Acknowledge();

        setup.RemoveValue(entity);

        Assert.Single(SnapshotParser.Parse(setup.WriteSnapshot()).Entities[0].RemovedComponentIds);
        Assert.Single(SnapshotParser.Parse(setup.WriteSnapshot()).Entities[0].RemovedComponentIds);

        setup.Acknowledge();

        Assert.True(setup.WriteSnapshot().IsEmpty);
    }

    /// <summary>
    ///   Regaining a component must send its value again, even when it is the same value the peer had before
    ///   the removal
    /// </summary>
    [Fact]
    public void Snapshot_ComponentRegainedAfterRemovalIsSentAgain()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);
        uint networkId = setup.NetworkIdOf(entity);

        setup.ApplyOnClient(setup.WriteSnapshot());
        setup.Acknowledge();

        setup.RemoveValue(entity);
        setup.ApplyOnClient(setup.WriteSnapshot());
        setup.Acknowledge();
        Assert.False(setup.ClientHasValue(networkId));

        setup.AddValue(entity, 1.0f);
        setup.ApplyOnClient(setup.WriteSnapshot());

        Assert.True(setup.ClientHasValue(networkId));
        Assert.Equal(1.0f, setup.ClientValueOf(networkId), 2);
    }

    /// <summary>
    ///   A component that comes back before its removal was confirmed must end as present on the client, not
    ///   removed by the repeat of a removal that is no longer true
    /// </summary>
    [Fact]
    public void Snapshot_ComponentRegainedBeforeRemovalAcknowledgedStaysPresent()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);
        uint networkId = setup.NetworkIdOf(entity);

        setup.ApplyOnClient(setup.WriteSnapshot());
        setup.Acknowledge();

        setup.RemoveValue(entity);

        // The removal goes out but is never acknowledged
        setup.WriteSnapshot();

        setup.AddValue(entity, 4.0f);
        setup.ApplyOnClient(setup.WriteSnapshot());

        Assert.True(setup.ClientHasValue(networkId));
        Assert.Equal(4.0f, setup.ClientValueOf(networkId), 2);
    }

    /// <summary>
    ///   Out of order delivery must not let an older snapshot undo newer values
    /// </summary>
    [Fact]
    public void Snapshot_OlderSnapshotIsIgnoredByTheClient()
    {
        using var setup = new ReplicationFixture();
        var entity = setup.CreateEntity(10, 1.0f);

        var first = setup.WriteSnapshotCopy();
        setup.SetValue(entity, 9.0f);
        var second = setup.WriteSnapshotCopy();

        setup.ApplyOnClient(second);
        Assert.Equal(9.0f, setup.ClientValueOf(setup.NetworkIdOf(entity)), 2);

        setup.ApplyOnClient(first);
        Assert.Equal(9.0f, setup.ClientValueOf(setup.NetworkIdOf(entity)), 2);
    }
}
