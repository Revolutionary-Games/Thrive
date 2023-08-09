using System.Collections.Generic;
using Godot;

/// <summary>
///   World simulation that uses the external physics engine in the native code module
/// </summary>
public abstract class WorldSimulationWithPhysics : WorldSimulation, IWorldSimulationWithPhysics
{
    protected readonly PhysicalWorld physics;

    /// <summary>
    ///   All created physics bodies. Must be tracked to correctly destroy them all
    /// </summary>
    protected readonly List<NativePhysicsBody> createdBodies = new();

    protected WorldSimulationWithPhysics()
    {
        physics = PhysicalWorld.Create();
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

        physics.DestroyBody(body);
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
        ReleaseUnmanagedResources();
        if (disposing)
        {
            physics.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ReleaseUnmanagedResources()
    {
        foreach (var createdBody in createdBodies)
        {
            physics.DestroyBody(createdBody);
        }

        createdBodies.Clear();
    }
}
