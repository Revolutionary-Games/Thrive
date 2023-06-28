namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    [With(typeof(Physics))]
    [With(typeof(CollisionManagement))]
    public sealed class PhysicsCollisionManagementSystem : AEntitySetSystem<float>
    {
        private readonly List<NativePhysicsBody> resolvedBodyReferences = new();

        public PhysicsCollisionManagementSystem(World world, IParallelRunner runner)
            : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var physics = ref entity.Get<Physics>();
            ref var collisionManagement = ref entity.Get<CollisionManagement>();

            if (collisionManagement.StateApplied)
                return;

            var physicsBody = physics.Body;

            if (physicsBody == null)
            {
                // Body not initialized yet
                return;
            }

            collisionManagement.StateApplied = true;

            if (!physics.CheckHasWorldReference())
                return;

            var physicalWorld = physics.BodyCreatedInWorld!;

            // All collision disable
            if (collisionManagement.AllCollisionsDisabled != collisionManagement.CurrentCollisionState)
            {
                collisionManagement.CurrentCollisionState = collisionManagement.AllCollisionsDisabled;

                physicalWorld.SetBodyCollisionsEnabledState(physicsBody, !collisionManagement.CurrentCollisionState);
            }

            // Collision disable against specific bodies
            try
            {
                ref var ignoreCollisions = ref collisionManagement.IgnoredCollisionsWith;
                if (ignoreCollisions == null)
                {
                    physicalWorld.BodyClearCollisionsIgnores(physicsBody);
                }
                else if (ignoreCollisions.Count > 0)
                {
                    if (ignoreCollisions.Count < 2)
                    {
                        // When ignoring just one collision use the single body API as that doesn't need to allocate
                        // any lists
                        physicalWorld.BodyClearCollisionsIgnores(physicsBody);

                        var ignoreWith = GetPhysicsForEntity(ignoreCollisions[0], ref collisionManagement);
                        if (ignoreWith != null)
                            physicalWorld.BodyIgnoreCollisionsWithBody(physicsBody, ignoreWith);
                    }
                    else
                    {
                        foreach (var ignoredEntity in ignoreCollisions)
                        {
                            var ignoreWith = GetPhysicsForEntity(ignoredEntity, ref collisionManagement);

                            if (ignoreWith != null)
                                resolvedBodyReferences.Add(ignoreWith);
                        }

                        physicalWorld.BodySetCollisionIgnores(physicsBody, resolvedBodyReferences);

                        resolvedBodyReferences.Clear();
                    }
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("Failed to apply body collision ignores: ", e);
            }

            // TODO: physics contact callbacks / collision storing
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NativePhysicsBody? GetPhysicsForEntity(Entity entity, ref CollisionManagement management)
        {
            NativePhysicsBody? body;

            try
            {
                ref var physics = ref entity.Get<Physics>();
                body = physics.Body;
            }
            catch (Exception e)
            {
                GD.PrintErr("Collision management refers to another entity that doesn't have the physics component: ",
                    e);
                return null;
            }

            // In case the body we don't want to collide with is not ready yet, we return null here to skip it, but
            // make sure we will try again next update until we get it

            if (body == null)
            {
                management.StateApplied = false;

                // TODO: could show an error after some time if still failing?

                return null;
            }

            return body;
        }
    }
}
