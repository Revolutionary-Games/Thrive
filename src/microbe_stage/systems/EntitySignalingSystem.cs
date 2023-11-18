namespace Systems
{
    using System;
    using System.Collections.Generic;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using Newtonsoft.Json;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Updates <see cref="CommandSignaler"/> components that also have <see cref="WorldPosition"/>
    /// </summary>
    [With(typeof(CommandSignaler))]
    [With(typeof(WorldPosition))]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EntitySignalingSystem : AEntitySetSystem<float>
    {
        private readonly Dictionary<ulong, List<(Entity Entity, Vector3 Position)>> entitiesOnChannels = new();

        [JsonProperty]
        private float elapsedSinceUpdate;

        private bool timeToUpdate;

        public EntitySignalingSystem(World world, IParallelRunner runner) : base(world, runner,
            Constants.SYSTEM_HIGH_ENTITIES_PER_THREAD)
        {
        }

        [JsonConstructor]
        public EntitySignalingSystem(float elapsedSinceUpdate) :
            base(TemporarySystemHelper.GetDummyWorldForLoad(), null)
        {
            this.elapsedSinceUpdate = elapsedSinceUpdate;
        }

        protected override void PreUpdate(float state)
        {
            base.PreUpdate(state);

            elapsedSinceUpdate += state;

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

            // Clear old signaling cache (and delete any cache categories that aren't in use anymore)
            foreach (var entry in entitiesOnChannels)
            {
                // Skip non-empty channels
                if (entry.Value.Count > 0)
                    continue;

                entitiesOnChannels.Remove(entry.Key);

                // It should be fine to just delete up to one category per system run as we shouldn't have that many
                // categories being abandoned multiple times per second. Though if we end up with many signalers
                // turning off and on often then we might not see the actually abandoned channels quickly.
                // This is done to avoid having to take a clone of the dictionary keys
                break;
            }

            // Clear still left categories
            foreach (var value in entitiesOnChannels.Values)
            {
                value.Clear();
            }

            // Update the queued commands to active commands first in a non-multithreaded way
            // TODO: this could also be multithreaded as long as this finishes before the Update calls start running
            // as long as the channel cache updating can be fast enough
            foreach (ref readonly var entity in Set.GetEntities())
            {
                ref var signaling = ref entity.Get<CommandSignaler>();

                if (signaling.QueuedSignalingCommand != null)
                {
                    signaling.Command = signaling.QueuedSignalingCommand.Value;
                    signaling.QueuedSignalingCommand = null;
                }

                // Build a mapping of signalers by their channel and position to speed up the update logic below
                if (signaling.Command == MicrobeSignalCommand.None)
                    continue;

                if (!entitiesOnChannels.TryGetValue(signaling.SignalingChannel, out var channel))
                {
                    channel = new List<(Entity Entity, Vector3 Position)>();

                    entitiesOnChannels[signaling.SignalingChannel] = channel;
                }

                ref var position = ref entity.Get<WorldPosition>();

                // TODO: determine if it is faster to copy the position here rather than continuously looking up
                // the position again in Update when comparing positions to signal receivers
                channel.Add((entity, position.Position));
            }
        }

        protected override void Update(float delta, ReadOnlySpan<Entity> entities)
        {
            if (!timeToUpdate)
                return;

            base.Update(delta, entities);
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var signaling = ref entity.Get<CommandSignaler>();

            // Find closest signaler on the channel this entity is on
            bool foundSignal = false;

            if (entitiesOnChannels.TryGetValue(signaling.SignalingChannel, out var signalers))
            {
                ref var position = ref entity.Get<WorldPosition>();

                // We kind of simulate how strong the "smell" of a signal is by finding the closest active signal
                (Entity Entity, Vector3 Position)? bestSignaler = null;
                float minDistanceFound = float.MaxValue;

                // In the old microbe AI implementation this actually used the last smelled position to calculate a new
                // min distance, which could result in different kind of "pinning" behaviour of previous commands. That
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

                    signaling.ReceivedCommandSource = bestSignaler.Value.Position;
                    signaling.ReceivedCommandFromEntity = bestSignaler.Value.Entity;

                    ref var signalerData = ref bestSignaler.Value.Entity.Get<CommandSignaler>();
                    signaling.ReceivedCommand = signalerData.Command;

                    foundSignal = true;
                }
            }

            if (!foundSignal)
            {
                signaling.ReceivedCommand = MicrobeSignalCommand.None;
            }
        }
    }
}
