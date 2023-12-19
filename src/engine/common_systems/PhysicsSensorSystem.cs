namespace Systems
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
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
        private readonly IWorldSimulationWithPhysics worldSimulationWithPhysics;

        private readonly List<NativePhysicsBody> createdSensors = new();
        private readonly Dictionary<Entity, NativePhysicsBody> detachedBodies = new();

        public PhysicsSensorSystem(IWorldSimulationWithPhysics worldSimulationWithPhysics, World world) :
            base(world, null)
        {
            this.worldSimulationWithPhysics = worldSimulationWithPhysics;
        }

        /// <summary>
        ///   Make sure sensor related to entity is destroyed
        /// </summary>
        public void OnEntityDestroyed(in Entity entity)
        {
            if (detachedBodies.TryGetValue(entity, out var detached))
            {
                detachedBodies.Remove(entity);
                createdSensors.Remove(detached);
                worldSimulationWithPhysics.DestroyBody(detached);
            }

            if (!entity.Has<PhysicsSensor>())
                return;

            ref var sensor = ref entity.Get<PhysicsSensor>();

            if (sensor.SensorBody != null && detached != sensor.SensorBody)
            {
                if (!createdSensors.Remove(sensor.SensorBody))
                    GD.PrintErr("Sensor system told about a destroyed sensor it didn't create");

                worldSimulationWithPhysics.DestroyBody(sensor.SensorBody);
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            base.Dispose();
        }

        protected override void PreUpdate(float delta)
        {
            // Immediate sensor destruction is handled by the world, but we still do this to detect if a sensor gets
            // removed without deleting the entity
            foreach (var createdSensor in createdSensors)
            {
                createdSensor.Marked = false;
            }
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
                        worldSimulationWithPhysics.PhysicalWorld.AddBody(disabledBody);

                        // Update position before the sensor should have any time to collide with anything
                        // Note that rotation is not updated as it's not updated elsewhere for sensors either
                        ref var newPosition = ref entity.Get<WorldPosition>();
                        worldSimulationWithPhysics.PhysicalWorld.SetBodyPosition(disabledBody, newPosition.Position);

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
                    worldSimulationWithPhysics.PhysicalWorld.DetachBody(sensor.SensorBody);

                    // For now don't set this to null so that the body marking still works for disabled bodies
                    // sensor.SensorBody = null;
                }
            }

            if (sensor.Disabled)
            {
                // Keep created sensors even while disabled
                if (sensor.SensorBody != null)
                    sensor.SensorBody.Marked = true;

                return;
            }

            // See if everything is up to date first
            if (sensor.SensorBody == null && sensor.ActiveArea != null)
            {
                // Time to create a body
                ref var position = ref entity.Get<WorldPosition>();
                sensor.SensorBody = worldSimulationWithPhysics.CreateSensor(sensor.ActiveArea, position.Position,
                    Quat.Identity, sensor.DetectSleepingBodies, sensor.DetectStaticBodies);

                // Set no entity on the sensor so anything colliding with the sensor can't do anything
                sensor.SensorBody.SetEntityReference(default(Entity));

                sensor.ActiveCollisions = worldSimulationWithPhysics.PhysicalWorld.BodyStartCollisionRecording(
                    sensor.SensorBody, sensor.MaxActiveContacts > 0 ?
                        sensor.MaxActiveContacts :
                        Constants.MAX_SIMULTANEOUS_COLLISIONS_SENSOR, out sensor.ActiveCollisionCountPtr);

                createdSensors.Add(sensor.SensorBody);
                sensor.SensorBody.Marked = true;
                return;
            }

            // Applying a new shape
            if (sensor.ApplyNewShape && sensor.SensorBody != null && sensor.ActiveArea != null)
            {
                worldSimulationWithPhysics.PhysicalWorld.ChangeBodyShape(sensor.SensorBody, sensor.ActiveArea);
                sensor.ApplyNewShape = false;
            }

            // TODO: this could maybe be put in a threaded update as writing body positions is allowed to happen from
            // multiple threads (but creation isn't)
            // Update sensor position if a sensor exists
            if (sensor.SensorBody != null)
            {
                ref var position = ref entity.Get<WorldPosition>();

                // TODO: should sensors have their rotation also apply? (see also above in the creation)
                worldSimulationWithPhysics.PhysicalWorld.SetBodyPosition(sensor.SensorBody, position.Position);

                sensor.SensorBody.Marked = true;
            }
        }

        protected override void PostUpdate(float delta)
        {
            createdSensors.RemoveAll(DestroySensorIfNotMarked);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DestroySensorIfNotMarked(NativePhysicsBody body)
        {
            if (body.Marked)
                return false;

            foreach (var detachedBody in detachedBodies)
            {
                if (detachedBody.Value == body)
                {
                    detachedBodies.Remove(detachedBody.Key);
                    break;
                }
            }

            worldSimulationWithPhysics.DestroyBody(body);

            return true;
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
