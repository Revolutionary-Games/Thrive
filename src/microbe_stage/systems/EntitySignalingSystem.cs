namespace Systems;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
using SharedBase.Archive;
using World = Arch.Core.World;

/// <summary>
///   Updates <see cref="CommandSignaler"/> components that also have <see cref="WorldPosition"/>
/// </summary>
[ReadsComponent(typeof(WorldPosition))]
[RunsBefore(typeof(MicrobeAISystem))]
[RuntimeCost(1)]
public partial class EntitySignalingSystem : BaseSystem<World, float>, IArchiveUpdatable
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly Dictionary<ulong, List<(Entity Entity, Vector3 Position)>> entitiesOnChannels = new();

    private float elapsedSinceUpdate;

    private bool timeToUpdate;

    public EntitySignalingSystem(World world) : base(world)
    {
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.EntitySignalingSystem;

    public void OnEntityDestroyed(in Entity entity)
    {
        if (!entity.Has<CommandSignaler>())
            return;

        // As signalling channels aren't immediately cleared, we need to clear data on entity death to not have invalid
        // entities on the channels
        // TODO: we could instead refresh the sent signals cache each update which would make this death callback not
        // required
        ref var commandSignaler = ref entity.Get<CommandSignaler>();
        if (commandSignaler.Command != MicrobeSignalCommand.None)
        {
            if (!entitiesOnChannels.TryGetValue(commandSignaler.SignalingChannel, out var channel))
                return;

            foreach (var tuple in channel)
            {
                if (tuple.Entity == entity)
                {
                    if (!channel.Remove(tuple))
                        GD.PrintErr("Failed to remove dead entity from a signaling channel");
                    break;
                }
            }
        }
    }

    public override void Update(in float delta)
    {
        // We manually call these to ensure the order

        // Update the queued commands to active commands first in a non-multithreaded way

        // TODO: this could also be multithreaded as long as this finishes before the Update calls start running and
        // there's locking on the data lists
        UpdateSignalSendQuery(World);

        UpdateSignalReceiveQuery(World);
    }

    public override void BeforeUpdate(in float delta)
    {
        elapsedSinceUpdate += delta;

        if (elapsedSinceUpdate > Constants.ENTITY_SIGNAL_UPDATE_INTERVAL)
        {
            elapsedSinceUpdate = 0;
            timeToUpdate = true;
        }
        else
        {
            timeToUpdate = false;
        }

        if (!timeToUpdate)
            return;

        // Clear old signalling cache (and delete any cache categories that aren't in use any more)
        foreach (var entry in entitiesOnChannels)
        {
            // Skip non-empty channels
            if (entry.Value.Count > 0)
                continue;

            entitiesOnChannels.Remove(entry.Key);

            // It should be fine to just delete up to one category per system run as we shouldn't have that many
            // categories being abandoned multiple times per second. Though if we end up with many signallers
            // turning off and on often, then we might not see the actually abandoned channels quickly.
            // This is done to avoid having to take a clone of the dictionary keys
            break;
        }

        // Clear away the still left categories (this makes sure that signal channels are emptied before each run
        // and don't persistently contain data)
        foreach (var value in entitiesOnChannels.Values)
        {
            value.Clear();
        }
    }

    public void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.Write(elapsedSinceUpdate);
    }

    public void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        elapsedSinceUpdate = reader.ReadFloat();
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateSignalSend(ref CommandSignaler signaling, ref WorldPosition position, in Entity entity)
    {
        // We can refresh our signal immediately to make receivers receive info faster, even if the full cache refresh
        // doesn't happen each game update
        if (signaling.QueuedSignalingCommand != null)
        {
            signaling.Command = signaling.QueuedSignalingCommand.Value;
            signaling.QueuedSignalingCommand = null;
        }

        if (signaling.Command == MicrobeSignalCommand.None)
            return;

        // As the channels are cleared sometimes, it's very important to not run this all the time as that
        // adds duplicate entries to the channel which makes things more difficult to make sure there aren't dead
        // entities on the channel
        if (!timeToUpdate)
            return;

        // Build a mapping of signallers by their channel and position to speed up the update logic below

        if (!entitiesOnChannels.TryGetValue(signaling.SignalingChannel, out var channel))
        {
            channel = new List<(Entity Entity, Vector3 Position)>();

            entitiesOnChannels[signaling.SignalingChannel] = channel;
        }

        // TODO: determine if it is faster to copy the position here rather than continuously looking up
        // the position again in Update when comparing positions to signal receivers
        channel.Add((entity, position.Position));
    }

    // TODO: could parallelize
    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateSignalReceive(ref CommandSignaler signaling, ref WorldPosition position, in Entity entity)
    {
        // Find the closest signaller on the channel this entity is on
        bool foundSignal = false;

        if (entitiesOnChannels.TryGetValue(signaling.SignalingChannel, out var signalers))
        {
            // We kind of simulate how strong the "smell" of a signal is by finding the closest active signal
            (Entity Entity, Vector3 Position)? bestSignaler = null;
            float minDistanceFound = float.MaxValue;

            // In the old microbe AI implementation this actually used the last smelled position to calculate a new
            // min distance, which could result in different kind of "pinning" behaviour for previous commands. That
            // is now gone as this does a fresh look each time.

            foreach (var signaler in signalers)
            {
                var distance = position.Position.DistanceSquaredTo(signaler.Position);
                if (distance < minDistanceFound)
                {
                    // Ignore our own signals
                    if (signaler.Entity == entity)
                        continue;

                    minDistanceFound = distance;

                    bestSignaler = signaler;
                }
            }

            if (bestSignaler != null)
            {
                // TODO: should there be a max distance after which the signaling agent is considered to be so
                // weak that it is not detected?

                var sourceEntity = bestSignaler.Value.Entity;

                if (!sourceEntity.IsAliveAndHas<CommandSignaler>())
                {
                    GD.PrintErr(
                        $"Received a signal command from an invalid entity (no signaler component): {sourceEntity}");
                }
                else
                {
                    signaling.ReceivedCommandSource = bestSignaler.Value.Position;
                    signaling.ReceivedCommandFromEntity = bestSignaler.Value.Entity;

                    ref var signalerData = ref sourceEntity.Get<CommandSignaler>();
                    signaling.ReceivedCommand = signalerData.Command;

                    foundSignal = true;
                }
            }
        }

        if (!foundSignal)
        {
            signaling.ReceivedCommand = MicrobeSignalCommand.None;
        }
    }
}
