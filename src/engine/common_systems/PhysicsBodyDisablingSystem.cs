namespace Systems
{
    using System.Collections.Generic;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles disabling and enabling the physics body for an entity (disabled bodies don't exist in the physical
    ///   world at all)
    /// </summary>
    [With(typeof(Physics))]
    [WritesToComponent(typeof(Physics))]
    [WritesToComponent(typeof(ManualPhysicsControl))]
    [RunsOnMainThread]
    public sealed class PhysicsBodyDisablingSystem : AEntitySetSystem<float>
    {
        private readonly PhysicalWorld physicalWorld;

        private readonly HashSet<NativePhysicsBody> disabledBodies = new();

        public PhysicsBodyDisablingSystem(PhysicalWorld physicalWorld, World world) : base(world, null)
        {
            this.physicalWorld = physicalWorld;
        }

        /// <summary>
        ///   Forgets about a disabled body on entity. Needs to be called before
        ///   <see cref="PhysicsBodyCreationSystem.OnEntityDestroyed"/> so that this can see the body to be destroyed
        /// </summary>
        public void OnEntityDestroyed(in Entity entity)
        {
            if (!entity.Has<Physics>())
                return;

            ref var physics = ref entity.Get<Physics>();

            if (physics.Body != null)
            {
                disabledBodies.Remove(physics.Body);
            }
        }

        /// <summary>
        ///   Needs to be called when a body is deleted so that state tracking for body disabling can remove it
        /// </summary>
        /// <param name="body">The deleted body</param>
        public void OnBodyDeleted(NativePhysicsBody body)
        {
            disabledBodies.Remove(body);
        }

        public override void Dispose()
        {
            Dispose(true);
            base.Dispose();
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var physics = ref entity.Get<Physics>();

            // Skip objects that are up to date
            if (physics.InternalDisableState == physics.BodyDisabled)
                return;

            var body = physics.Body;
            if (body == null)
                return;

            if (physics.InternalDisableState)
            {
                // Need to restore body
                physics.InternalDisableState = false;

                // In case the body was recreated, then we need to skip this (as the body instance was not removed
                // from the world by us)
                if (disabledBodies.Remove(body))
                {
                    // TODO: should a new position be applied first?
                    physicalWorld.AddBody(body);
                }

                // Reset physics applied impulse which may have accumulated to be very large
                if (entity.Has<ManualPhysicsControl>())
                {
                    ref var manual = ref entity.Get<ManualPhysicsControl>();
                    manual.ResetAccumulatedForce();
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

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                disabledBodies.Clear();

                // The bodies are destroyed by the creation system / the world. Also see OnEntityDestroyed
            }
        }
    }
}
