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

    public PhysicsBody CreateMovingBody(PhysicsShape shape, Vector3 position, Quat rotation);

    public void DestroyBody(PhysicsBody body);
}
