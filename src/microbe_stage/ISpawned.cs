using Godot;

/// <summary>
///   All nodes that can be spawned with the spawn system must implement this interface
/// </summary>
public interface ISpawned : IEntity
{
    /// <summary>
    ///   If the squared distance to the player of this object is
    ///   greater than this, it is despawned.
    /// </summary>
    int DespawnRadiusSquared { get; set; }

    /// <summary>
    ///   The Node that was spawned for this entity, used to tag it
    ///   for detecting despawning.
    /// </summary>
    Node SpawnedNode { get; }
}
