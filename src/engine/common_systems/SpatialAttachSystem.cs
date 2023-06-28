namespace Systems
{
    using System;
    using System.Collections.Generic;
    using Components;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Attaches <see cref="SpatialInstance"/> to the Godot scene and handles freeing unused spatial instances.
    ///   Must run before <see cref="SpatialPositionSystem"/>.
    /// </summary>
    public sealed class SpatialAttachSystem : AComponentSystem<float, SpatialInstance>
    {
        private readonly Node godotWorldRoot;
        private readonly World world;

        private readonly Dictionary<Spatial, AttachedInfo> attachedSpatialInstances = new();
        private readonly List<Spatial> instancesToDelete = new();

        public SpatialAttachSystem(Node godotWorldRoot, World world) : base(world)
        {
            this.godotWorldRoot = godotWorldRoot;
            this.world = world;
        }

        protected override void PreUpdate(float state)
        {
            // Unmark all
            foreach (var info in attachedSpatialInstances.Values)
            {
                info.Marked = false;
            }
        }

        protected override void PostUpdate(float state)
        {
            // Delete unmarked
            foreach (var pair in attachedSpatialInstances)
            {
                if (!pair.Value.Marked)
                    instancesToDelete.Add(pair.Key);
            }

            foreach (var spatial in instancesToDelete)
            {
                attachedSpatialInstances.Remove(spatial);
                spatial.QueueFree();
            }

            instancesToDelete.Clear();
        }

        protected override void Update(float state, Span<SpatialInstance> components)
        {
            foreach (ref SpatialInstance spatial in components)
            {
                var graphicalInstance = spatial.GraphicalInstance;
                if (graphicalInstance == null)
                    continue;

                if (!attachedSpatialInstances.TryGetValue(graphicalInstance, out var info))
                {
                    // New spatial to attach
                    godotWorldRoot.AddChild(graphicalInstance);

                    info = new AttachedInfo();
                    attachedSpatialInstances.Add(graphicalInstance, info);
                }
                else
                {
                    info.Marked = true;
                }
            }
        }

        /// <summary>
        ///   Info (really just a marked status) for spatial instances. This breaks the use of only value types by
        ///   systems, so there might be some more efficient way to implement this (for example with two hash sets)
        /// </summary>
        private class AttachedInfo
        {
            public bool Marked = true;
        }
    }
}
