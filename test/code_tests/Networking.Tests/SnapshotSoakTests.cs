namespace ThriveTest.Networking.Tests;

using System;
using System.Collections.Generic;
using Xunit;

/// <summary>
///   Runs many ticks of changing state through a lossy link and checks the client ends up matching the server
/// </summary>
/// <remarks>
///   <para>
///     This is the test that guards the whole delta scheme. The targeted tests each check one rule; this one
///     checks that the rules together actually converge when packets go missing, which is the only thing that
///     matters in the end. A mistake in the acknowledgement bookkeeping, in the packet size cap, or in any
///     future change to how snapshots are built shows up here as a value that never catches up.
///   </para>
/// </remarks>
public class SnapshotSoakTests
{
    [Theory]
    [InlineData(0.0f, 12345)]
    [InlineData(0.3f, 999)]
    [InlineData(0.5f, 2024)]
    [InlineData(0.8f, 7)]
    public void Snapshot_ClientConvergesThroughPacketLoss(float lossFraction, int seed)
    {
        var random = new Random(seed);

        using var setup = new ReplicationFixture();

        var entities = new List<Arch.Core.Entity>();
        var expected = new Dictionary<uint, float>();

        // Enough entities that packets regularly hit the size cap, so entities are left out and have to be
        // picked up by later packets. Without that this would never exercise deferral at all.
        for (int i = 0; i < 150; ++i)
        {
            var entity = setup.CreateEntity(i, 0);
            entities.Add(entity);
            expected[setup.NetworkIdOf(entity)] = 0;
        }

        for (int step = 0; step < 400; ++step)
        {
            // Change a few entities each step, the way a running simulation would
            for (int i = 0; i < 3; ++i)
            {
                var entity = entities[random.Next(entities.Count)];
                float value = (float)Math.Round(random.NextDouble() * 50 - 25, 2);

                setup.SetValue(entity, value);
                expected[setup.NetworkIdOf(entity)] = value;
            }

            var snapshot = setup.WriteSnapshotCopy();

            if (snapshot.Length == 0)
                continue;

            // The packet is lost before reaching the client, so it is neither applied nor acknowledged
            if (random.NextDouble() < lossFraction)
                continue;

            setup.ApplyOnClient(snapshot);
            setup.Acknowledge(setup.ClientAcknowledgementSequence);
        }

        // Let the link settle so anything still unconfirmed can arrive
        for (int step = 0; step < 200; ++step)
        {
            var snapshot = setup.WriteSnapshotCopy();

            if (snapshot.Length == 0)
                break;

            setup.ApplyOnClient(snapshot);
            setup.Acknowledge(setup.ClientAcknowledgementSequence);
        }

        foreach (var entry in expected)
        {
            Assert.True(setup.ClientHasValue(entry.Key), $"Client is missing entity {entry.Key}");

            // Values are quantized on the wire, so exact equality is not the bar
            Assert.Equal(entry.Value, setup.ClientValueOf(entry.Key), 2);
        }
    }

    /// <summary>
    ///   The same run, but with entities appearing and disappearing, since spawns and despawns are repeated
    ///   through the same mechanism as values
    /// </summary>
    [Fact]
    public void Snapshot_ClientConvergesWithSpawnsAndDespawnsThroughLoss()
    {
        var random = new Random(4242);

        using var setup = new ReplicationFixture();

        var alive = new List<Arch.Core.Entity>();
        var expected = new Dictionary<uint, float>();

        for (int step = 0; step < 400; ++step)
        {
            if (alive.Count < 12 && random.NextDouble() < 0.3)
            {
                float value = (float)Math.Round(random.NextDouble() * 20, 2);
                var entity = setup.CreateEntity(step, value);
                alive.Add(entity);
                expected[setup.NetworkIdOf(entity)] = value;
            }
            else if (alive.Count > 2 && random.NextDouble() < 0.15)
            {
                int index = random.Next(alive.Count);
                var entity = alive[index];

                expected.Remove(setup.NetworkIdOf(entity));
                alive.RemoveAt(index);
                setup.DespawnEntity(entity);
            }
            else if (alive.Count > 0)
            {
                var entity = alive[random.Next(alive.Count)];
                float value = (float)Math.Round(random.NextDouble() * 20, 2);

                setup.SetValue(entity, value);
                expected[setup.NetworkIdOf(entity)] = value;
            }

            var snapshot = setup.WriteSnapshotCopy();

            if (snapshot.Length == 0 || random.NextDouble() < 0.4)
                continue;

            setup.ApplyOnClient(snapshot);
            setup.Acknowledge(setup.ClientAcknowledgementSequence);
        }

        for (int step = 0; step < 200; ++step)
        {
            var snapshot = setup.WriteSnapshotCopy();

            if (snapshot.Length == 0)
                break;

            setup.ApplyOnClient(snapshot);
            setup.Acknowledge(setup.ClientAcknowledgementSequence);
        }

        foreach (var entry in expected)
        {
            Assert.True(setup.ClientHasValue(entry.Key), $"Client is missing entity {entry.Key}");
            Assert.Equal(entry.Value, setup.ClientValueOf(entry.Key), 2);
        }

        // Despawned entities must be gone on the client as well, not left behind as ghosts
        Assert.Equal(expected.Count, setup.ClientEntityIds.Count);
    }
}
