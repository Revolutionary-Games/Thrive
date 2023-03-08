/// <summary>
///   All nodes that can be spawned with the spawn system must implement this interface
/// </summary>
public interface ISpawned : IEntity
{
    /// <summary>
    ///   If the squared distance to the player of this object is
    ///   greater than this, it is despawned.
    /// </summary>
    public int DespawnRadiusSquared { get; set; }

    /// <summary>
    ///   How much this entity contributes to the entity limit relative to a single node
    /// </summary>
    public float EntityWeight { get; }
}
