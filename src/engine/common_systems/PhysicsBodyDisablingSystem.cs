namespace Systems
{
    using System;
    using System.Collections.Generic;
    using Components;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles disabling and enabling the physics body for an entity (disabled bodies don't exist in the physical
    ///   world at all)
    /// </summary>
    public sealed class PhysicsBodyDisablingSystem : AComponentSystem<float, Physics>
    {
        private readonly PhysicalWorld physicalWorld;

        private readonly HashSet<NativePhysicsBody> disabledBodies = new();

        public PhysicsBodyDisablingSystem(PhysicalWorld physicalWorld, World world) : base(world)
        {
            this.physicalWorld = physicalWorld;
        }

        /// <summary>
        ///   Needs to be called when a body is deleted so that state tracking for body disabling can remove it
        /// </summary>
        /// <param name="body">The deleted body</param>
        public void OnBodyDeleted(NativePhysicsBody body)
        {
            // TODO: if needed for deletion this could reattach the body here?

            disabledBodies.Remove(body);
        }

        protected override void Update(float state, Span<Physics> components)
        {
            foreach (ref Physics physics in components)
            {
                // Skip objects that are up to date
                if (physics.InternalDisableState == physics.BodyDisabled)
                    continue;

                var body = physics.Body;
                if (body == null)
                    continue;

                if (physics.InternalDisableState)
                {
                    // Need to restore body
                    physics.InternalDisableState = false;

                    // In case the body was recreated, then we need to skip this (as the body instance was not removed
                    // from the world by us)
                    if (disabledBodies.Contains(body))
                    {
                        physicalWorld.AddBody(body);
                        disabledBodies.Remove(body);
                    }
                }
                else
                {
                    // Disable the body
                    physics.InternalDisableState = true;

                    if (disabledBodies.Add(body))
                    {
                        physicalWorld.DetachBody(body);
                    }
                    else
                    {
                        GD.PrintErr("Body that was to be disabled was already disabled somehow");
                    }
                }
            }
        }
    }
}
