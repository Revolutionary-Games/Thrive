namespace Systems;

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
[RunsAfter(typeof(PhysicsBodyCreationSystem))]
[RuntimeCost(1)]
public sealed class PhysicsCollisionManagementSystem : AEntitySetSystem<float>
{
    private readonly PhysicalWorld physicalWorld;

    // This doesn't actually need to be accessed, just needs to hold data so that callback objects that are given to
    // the native side cannot be garbage collected
    // ReSharper disable once CollectionNeverQueried.Local
    private readonly Dictionary<Entity, PhysicalWorld.OnCollisionFilterCallback> activeCollisionFilterCallbacks = new();

    /// <summary>
    ///   Used for temporary storage during an update
    /// </summary>
    private readonly List<NativePhysicsBody> resolvedBodyReferences = new();

    public PhysicsCollisionManagementSystem(PhysicalWorld physicalWorld, World world, IParallelRunner runner) :
        base(world, runner)
    {
        this.physicalWorld = physicalWorld;
    }

    /// <summary>
    ///   Handles physics destroy actions for an entity that has done collision management
    /// </summary>
    public void OnEntityDestroyed(in Entity entity)
    {
        if (!entity.Has<CollisionManagement>())
            return;

        lock (activeCollisionFilterCallbacks)
        {
            activeCollisionFilterCallbacks.Remove(entity);
        }

        ref var collisionManagement = ref entity.Get<CollisionManagement>();

        if (collisionManagement.ActiveCollisions != null)
        {
            collisionManagement.ActiveCollisions = null;
            collisionManagement.ActiveCollisionCountPtr = IntPtr.Zero;

            // Note the other systems handle destroying the physics body, which will also force disable the collision
            // recording, so that is not required to be done here
            /*ref var physics = ref entity.Get<Physics>();

            if (physics.Body != null)
                physicalWorld.BodyStopCollisionRecording(physics.Body);*/
        }
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

        // All collision disable is now in Physics directly and applied by PhysicsUpdateAndPositionSystem

        // Collision disable against specific bodies
        try
        {
            ref var ignoreCollisions = ref collisionManagement.IgnoredCollisionsWith;
            if (ignoreCollisions == null)
            {
                if (collisionManagement.CollisionIgnoresUsed)
                {
                    collisionManagement.CollisionIgnoresUsed = false;
                    physicalWorld.BodyClearCollisionsIgnores(physicsBody);
                }
            }
            else if (ignoreCollisions.Count > 0)
            {
                collisionManagement.CollisionIgnoresUsed = true;

                if (ignoreCollisions.Count < 2)
                {
                    // When ignoring just one collision use the single body API as that doesn't need to allocate
                    // any lists
                    var ignoreWith = GetPhysicsForEntity(ignoreCollisions[0], ref collisionManagement);
                    if (ignoreWith != null)
                        physicalWorld.BodySetCollisionIgnores(physicsBody, ignoreWith);
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

        if (collisionManagement.RecordActiveCollisions > 0)
        {
            if (collisionManagement.ActiveCollisions == null || collisionManagement.ActiveCollisions.Length !=
                collisionManagement.RecordActiveCollisions)
            {
                // Start recording collisions
                collisionManagement.ActiveCollisions = physicalWorld.BodyStartCollisionRecording(physicsBody,
                    collisionManagement.RecordActiveCollisions, out collisionManagement.ActiveCollisionCountPtr);
            }
        }
        else if (collisionManagement.ActiveCollisions != null)
        {
            // Stop recording collisions
            collisionManagement.ActiveCollisions = null;
            collisionManagement.ActiveCollisionCountPtr = IntPtr.Zero;

            physicalWorld.BodyStopCollisionRecording(physicsBody);
        }

        bool wantedFilterState = collisionManagement.CollisionFilter != null;

        if (wantedFilterState != collisionManagement.CollisionFilterCallbackRegistered)
        {
            collisionManagement.CollisionFilterCallbackRegistered = wantedFilterState;

            lock (activeCollisionFilterCallbacks)
            {
                if (wantedFilterState)
                {
                    var filter = collisionManagement.CollisionFilter!;

                    // Make sure the filter delegate stays alive for the duration of the registration
                    activeCollisionFilterCallbacks[entity] = filter;

                    physicalWorld.BodyAddCollisionFilter(physicsBody, filter);
                }
                else
                {
                    physicalWorld.BodyDisableCollisionFilter(physicsBody);

                    // No longer need to keep this callback object alive
                    activeCollisionFilterCallbacks.Remove(entity);
                }
            }
        }
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
