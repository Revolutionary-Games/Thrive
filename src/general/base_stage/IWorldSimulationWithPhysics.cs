using Godot;
using Newtonsoft.Json;

public interface IWorldSimulationWithPhysics : IWorldSimulation
{
    /// <summary>
    ///   The physical world of this simulation. This is accessible to allow body operations that need to be called
    ///   through the world object.
    /// </summary>
    [JsonIgnore]
    public PhysicalWorld PhysicalWorld { get; }

    public NativePhysicsBody CreateMovingBody(PhysicsShape shape, Vector3 position, Quaternion rotation);

    public NativePhysicsBody CreateMovingBodyWithAxisLock(PhysicsShape shape, Vector3 position, Quaternion rotation,
        Vector3 lockedAxis, bool lockRotation);

    public NativePhysicsBody CreateStaticBody(PhysicsShape shape, Vector3 position, Quaternion rotation);

    public NativePhysicsBody CreateSensor(PhysicsShape sensorShape, Vector3 position, Quaternion rotation,
        bool detectSleepingBodies = false, bool detectStaticBodies = false);

    public void DestroyBody(NativePhysicsBody body);
}
