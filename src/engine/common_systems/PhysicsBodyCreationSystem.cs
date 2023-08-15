namespace Systems
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Creates physics bodies for entities that have a shape defined for them. Also handles deleting unused bodies.
    /// </summary>
    [With(typeof(Physics))]
    [With(typeof(PhysicsShapeHolder))]
    [With(typeof(WorldPosition))]
    public sealed class PhysicsBodyCreationSystem : AEntitySetSystem<float>
    {
        private readonly IWorldSimulationWithPhysics worldSimulationWithPhysics;
        private readonly OnBodyDeleted? deleteCallback;

        private readonly List<NativePhysicsBody> createdBodies = new();

        public PhysicsBodyCreationSystem(IWorldSimulationWithPhysics worldSimulationWithPhysics,
            OnBodyDeleted? deleteCallback, World world, IParallelRunner runner) : base(world, runner)
        {
            this.worldSimulationWithPhysics = worldSimulationWithPhysics;
            this.deleteCallback = deleteCallback;
        }

        public delegate void OnBodyDeleted(NativePhysicsBody body);

        protected override void PreUpdate(float delta)
        {
            // TODO: would it be better to have the world take care of destroying physics bodies when the entity
            // destruction is triggered?
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
            if (body != null && !shapeHolder.RecreateBody)
                return;

            // Skip if not ready to create the body yet
            if (shapeHolder.Shape == null)
                return;

            ref var position = ref entity.Get<WorldPosition>();

            if (shapeHolder.BodyIsStatic)
            {
                body = worldSimulationWithPhysics.CreateStaticBody(shapeHolder.Shape, position.Position,
                    position.Rotation);
            }
            else
            {
                if (physics.LockToYAxis)
                {
                    body = worldSimulationWithPhysics.CreateMovingBodyWithAxisLock(shapeHolder.Shape, position.Position,
                        position.Rotation, Vector3.Up, physics.LockRotationWithAxisLock);
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
            shapeHolder.RecreateBody = false;
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

            // Notify external things about the deleted body
            deleteCallback?.Invoke(body);

            // TODO: ensure this works fine if the body is currently in disabled state
            worldSimulationWithPhysics.DestroyBody(body);

            return true;
        }
    }
}
