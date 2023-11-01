using System.Collections.Generic;
using Godot;

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

    protected override void WaitForStartedPhysicsRun()
    {
        // TODO: implement multithreading
    }

    protected override bool RunPhysicsIfBehind()
    {
        // TODO: implement this once multithreaded running is added
        return false;
    }

    protected override void OnStartPhysicsRunIfTime(float delta)
    {
        physics.ProcessPhysics(delta);
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
            DestroyBody(createdBodies[createdBodies.Count - 1]);
        }

        physics.Dispose();
    }
}
