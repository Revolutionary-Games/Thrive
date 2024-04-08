using Godot;
using Newtonsoft.Json;

/// <summary>
///   All Godot-based game entities implement this interface to provide support for needed operations regarding them.
///   For other simulated entities see <see cref="IWorldSimulation"/>.
/// </summary>
public interface IEntity : IAliveTracked
{
    /// <summary>
    ///   The Node that this entity is in the game world as
    /// </summary>
    [JsonIgnore]
    public Node3D EntityNode { get; }
}
