namespace Systems
{
    using System.Collections.Generic;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handling system for <see cref="PhysicsSensorSystem"/>. Keeps the sensor at the world position of the entity.
    /// </summary>
    [With(typeof(PhysicsSensor))]
    [With(typeof(WorldPosition))]
    [ReadsComponent(typeof(WorldPosition))]
    [RunsAfter(typeof(OrganelleTickSystem))]
    [RunsOnMainThread]
    public sealed class PhysicsSensorSystem : AEntitySetSystem<float>
    {
        private readonly PhysicalWorld physicalWorld;

        private readonly Dictionary<Entity, NativePhysicsBody> detachedBodies = new();

        public PhysicsSensorSystem(PhysicalWorld physicalWorld, World world) : base(world, null)
        {
            this.physicalWorld = physicalWorld;
        }

        /// <summary>
        ///   Make sure sensor related to entity is destroyed
        /// </summary>
        public void OnEntityDestroyed(in Entity entity)
        {
            if (detachedBodies.TryGetValue(entity, out var detached))
            {
                detachedBodies.Remove(entity);
                physicalWorld.DestroyBody(detached);
            }

            if (!entity.Has<PhysicsSensor>())
                return;

            ref var sensor = ref entity.Get<PhysicsSensor>();

            if (sensor.SensorBody != null)
            {
                physicalWorld.DestroyBody(sensor.SensorBody);
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            base.Dispose();
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var sensor = ref entity.Get<PhysicsSensor>();

            // Handle disabling
            if (sensor.Disabled != sensor.InternalDisabledState)
            {
                sensor.InternalDisabledState = sensor.Disabled;

                if (!sensor.Disabled)
                {
                    // Re-enable (not an error if body doesn't exist, it will be created brand-new soon)
                    if (detachedBodies.TryGetValue(entity, out var disabledBody))
                    {
                        // TODO: should a new position be applied first?
                        physicalWorld.AddBody(disabledBody);

                        detachedBodies.Remove(entity);

                        if (sensor.SensorBody != null && sensor.SensorBody != disabledBody)
                        {
                            GD.PrintErr("Overwriting sensor body with re-enabled one, leaking a created body");
                        }

                        sensor.SensorBody = disabledBody;
                    }
                }
                else if (sensor.SensorBody != null)
                {
                    // Disable
                    if (detachedBodies.ContainsKey(entity))
                        GD.PrintErr("Sensor already had a disabled body stored, the reference will be leaked");

                    detachedBodies[entity] = sensor.SensorBody;
                    physicalWorld.DetachBody(sensor.SensorBody);

                    sensor.SensorBody = null;
                }
            }

            if (sensor.Disabled)
                return;

            // See if everything is up to date first
            if (sensor.SensorBody == null && sensor.ActiveArea != null)
            {
                // Time to create a body
                ref var position = ref entity.Get<WorldPosition>();
                sensor.SensorBody = physicalWorld.CreateSensor(sensor.ActiveArea, position.Position, Quat.Identity,
                    sensor.DetectSleepingBodies, sensor.DetectStaticBodies);

                // Set no entity on the sensor so anything colliding with the sensor can't do anything
                sensor.SensorBody.SetEntityReference(default(Entity));

                sensor.ActiveCollisions = physicalWorld.BodyStartCollisionRecording(sensor.SensorBody,
                    sensor.MaxActiveContacts > 0 ?
                        sensor.MaxActiveContacts :
                        Constants.MAX_SIMULTANEOUS_COLLISIONS_SENSOR, out sensor.ActiveCollisionCountPtr);

                return;
            }

            // Applying a new shape
            if (sensor.ApplyNewShape && sensor.SensorBody != null && sensor.ActiveArea != null)
            {
                physicalWorld.ChangeBodyShape(sensor.SensorBody, sensor.ActiveArea);
                sensor.ApplyNewShape = false;
            }

            // TODO: this could maybe be put in a threaded update as writing body positions is allowed to happen from
            // multiple threads (but creation isn't)
            // Update sensor position if a sensor exists
            if (sensor.SensorBody != null)
            {
                ref var position = ref entity.Get<WorldPosition>();

                // TODO: should sensors have their rotation also apply? (see also above in the creation)
                physicalWorld.SetBodyPosition(sensor.SensorBody, position.Position);
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // The bodies cleanup is handled by the world simulation
                detachedBodies.Clear();
            }
        }
    }
}
