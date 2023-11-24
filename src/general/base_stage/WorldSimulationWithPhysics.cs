using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using World = DefaultEcs.World;

/// <summary>
///   World simulation that uses the external physics engine in the native code module
/// </summary>
public abstract class WorldSimulationWithPhysics : WorldSimulation, IWorldSimulationWithPhysics
{
    protected readonly PhysicalWorld physics = PhysicalWorld.Create();

    /// <summary>
    ///   All created physics bodies. Must be tracked to correctly destroy them all
    /// </summary>
    protected readonly List<NativePhysicsBody> createdBodies = new();

    public WorldSimulationWithPhysics()
    {
    }

    [JsonConstructor]
    public WorldSimulationWithPhysics(World entities) : base(entities)
    {
    }

    ~WorldSimulationWithPhysics()
    {
        Dispose(false);
    }

    public PhysicalWorld PhysicalWorld => physics;

    public NativePhysicsBody CreateMovingBody(PhysicsShape shape, Vector3 position, Quat rotation)
    {
        var body = physics.CreateMovingBody(shape, position, rotation);
        createdBodies.Add(body);
        return body;
    }

    public NativePhysicsBody CreateMovingBodyWithAxisLock(PhysicsShape shape, Vector3 position, Quat rotation,
        Vector3 lockedAxis, bool lockRotation)
    {
        var body = physics.CreateMovingBodyWithAxisLock(shape, position, rotation, lockedAxis, lockRotation);
        createdBodies.Add(body);
        return body;
    }

    public NativePhysicsBody CreateStaticBody(PhysicsShape shape, Vector3 position, Quat rotation)
    {
        var body = physics.CreateStaticBody(shape, position, rotation);
        createdBodies.Add(body);
        return body;
    }

    public void DestroyBody(NativePhysicsBody body)
    {
        if (!createdBodies.Remove(body))
        {
            GD.PrintErr("Can't destroy body not in simulation");
            return;
        }

        // Stop collision recording if it is active to make sure the memory for that is returned to the pool
        if (body.ActiveCollisions != null)
            physics.BodyStopCollisionRecording(body);

        physics.DestroyBody(body);

        // Other code is not allowed to hold on to physics bodies on entities that are destroyed so we dispose this
        // here to get the native side wrapper released as well
        body.Dispose();
    }

    protected abstract override void InitSystemsEarly();

    protected override void WaitForStartedPhysicsRun()
    {
        physics.WaitUntilPhysicsRunEnds();
    }

    protected override void OnStartPhysicsRunIfTime(float delta)
    {
        physics.ProcessPhysicsOnBackgroundThread(delta);
    }

    protected override void Dispose(bool disposing)
    {
        // Derived classes should also wait for this before destroying things
        WaitForStartedPhysicsRun();

        ReleaseUnmanagedResources();

        // if (disposing)
        // {
        //
        // }

        base.Dispose(disposing);
    }

    private void ReleaseUnmanagedResources()
    {
        while (createdBodies.Count > 0)
        {
            var body = createdBodies[createdBodies.Count - 1];

            // This should never happen but this is here in case this does happen to give a better error message
            if (body.IsDisposed)
                throw new Exception("World physics body was disposed by someone else");

            DestroyBody(body);
        }

        physics.Dispose();
    }
}
