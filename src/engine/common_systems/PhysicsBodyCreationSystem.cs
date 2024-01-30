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
    ///   Creates physics bodies for entities that have a shape defined for them. Also handles deleting unused bodies.
    /// </summary>
    [With(typeof(Physics))]
    [With(typeof(PhysicsShapeHolder))]
    [With(typeof(WorldPosition))]
    [ReadsComponent(typeof(WorldPosition))]
    [RunsBefore(typeof(PhysicsUpdateAndPositionSystem))]
    [RunsOnMainThread]
    public sealed class PhysicsBodyCreationSystem : AEntitySetSystem<float>
    {
        private readonly IWorldSimulationWithPhysics worldSimulationWithPhysics;
        private readonly PhysicsBodyDisablingSystem disablingSystem;

        private readonly List<NativePhysicsBody> createdBodies = new();

        // This is not parallel as we don't use the parallel body add method of Jolt
        public PhysicsBodyCreationSystem(IWorldSimulationWithPhysics worldSimulationWithPhysics,
            PhysicsBodyDisablingSystem disablingSystem, World world) : base(world, null)
        {
            this.worldSimulationWithPhysics = worldSimulationWithPhysics;
            this.disablingSystem = disablingSystem;
        }

        /// <summary>
        ///   Destroys a body collision body immediately. This is needed to be called by the world to ensure that
        ///   physics bodies of destroyed entities are immediately destroyed
        /// </summary>
        public void OnEntityDestroyed(in Entity entity)
        {
            if (!entity.Has<Physics>())
                return;

            ref var physics = ref entity.Get<Physics>();

            if (physics.Body != null)
            {
                if (!createdBodies.Remove(physics.Body))
                    GD.PrintErr("Body creation system told about a destroyed physics body it didn't create");

                worldSimulationWithPhysics.DestroyBody(physics.Body);

                physics.Body = null;
            }
        }

        protected override void PreUpdate(float delta)
        {
            // Immediate body destruction is handled by the world, but we still do this to detect if a physics
            // component gets removed while things are running
            foreach (var createdBody in createdBodies)
            {
                createdBody.Marked = false;
            }
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var physics = ref entity.Get<Physics>();

            var body = physics.Body;

            // Mark bodies in use
            if (body != null)
                body.Marked = true;

            if (physics.BodyDisabled)
                return;

            ref var shapeHolder = ref entity.Get<PhysicsShapeHolder>();

            // Don't need to do anything if body is already created and it is not requested to be recreated
            if (body != null && !shapeHolder.UpdateBodyShapeIfCreated)
                return;

            // Skip if not ready to create the body yet
            if (shapeHolder.Shape == null)
                return;

            if (body != null && shapeHolder.UpdateBodyShapeIfCreated)
            {
                // Change the shape of the body
                var physicalWorld = worldSimulationWithPhysics.PhysicalWorld;
                physicalWorld.ChangeBodyShape(body, shapeHolder.Shape);
                shapeHolder.UpdateBodyShapeIfCreated = false;

                // TODO: apply changing shapeHolder.BodyIsStatic variable if that needs to ever work (probably
                // should just fallback to the normal creation logic, as switching from static to moving doesn't need
                // to preserve velocity)

                return;
            }

            // Create a new body

            ref var position = ref entity.Get<WorldPosition>();

            if (shapeHolder.BodyIsStatic)
            {
                body = worldSimulationWithPhysics.CreateStaticBody(shapeHolder.Shape, position.Position,
                    position.Rotation);
            }
            else
            {
                if (physics.AxisLock != Physics.AxisLockType.None)
                {
                    body = worldSimulationWithPhysics.CreateMovingBodyWithAxisLock(shapeHolder.Shape, position.Position,
                        position.Rotation, Vector3.Up, (physics.AxisLock & Physics.AxisLockType.AlsoLockRotation) != 0);
                }
                else
                {
                    body = worldSimulationWithPhysics.CreateMovingBody(shapeHolder.Shape, position.Position,
                        position.Rotation);
                }

                var physicalWorld = worldSimulationWithPhysics.PhysicalWorld;

                // Apply initial velocity
                physicalWorld.SetBodyVelocity(body, physics.Velocity, physics.AngularVelocity);

                if (physics.LinearDamping != null)
                {
                    physicalWorld.SetDamping(body, physics.LinearDamping.Value, physics.AngularDamping);
                }
            }

            // Store the entity in the body to make physics callbacks reported back from the physics system tell us
            // the entities involved in them
            body.SetEntityReference(entity);

            body.Marked = true;
            createdBodies.Add(body);

            physics.VelocitiesApplied = true;
            physics.DampingApplied = true;

            physics.Body = body;
            shapeHolder.UpdateBodyShapeIfCreated = false;
        }

        protected override void PostUpdate(float delta)
        {
            createdBodies.RemoveAll(DestroyBodyIfNotMarked);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DestroyBodyIfNotMarked(NativePhysicsBody body)
        {
            if (body.Marked)
                return false;

            disablingSystem.OnBodyDeleted(body);

            worldSimulationWithPhysics.DestroyBody(body);

            return true;
        }
    }
}
