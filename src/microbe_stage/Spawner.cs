using Godot;

/// <summary>
///   Spawner that can be added to a SpawnSystem to be used for spawning things
/// </summary>
public abstract class Spawner
{
    public const float DEFAULT_SPAWN_RADIUS = 5.5f;

    /// <summary>
    ///   Whether this spawner spawns items contributing to the entity limit
    /// </summary>
    public abstract bool SpawnsEntities { get; }

    /// <summary>
    ///   How much room this spawned thing needs from terrain to not overlap it
    /// </summary>
    public virtual float TerrainCollisionRadius => DEFAULT_SPAWN_RADIUS;

    /// <summary>
    ///   The distance at which spawning happens
    /// </summary>
    public int SpawnRadius { get; set; }

    /// <summary>
    ///   How often the SpawnSystem will call this spawner
    /// </summary>
    /// <value>How often the SpawnSystem will call this spawner.</value>
    public float Density { get; set; }

    /// <summary>
    ///   If this is queued to be destroyed the spawn system will remove this on next update
    /// </summary>
    /// <value><c>true</c> if destroy queued; otherwise, <c>false</c>.</value>
    public bool DestroyQueued { get; set; }

    /// <summary>
    ///   Name used for internal logging etc. purposes
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    ///   Spawns the next thing. This is an enumerator to be able to control how many things to spawn per frame easily
    /// </summary>
    /// <param name="worldSimulation">The simulation to create the entity in</param>
    /// <param name="location">Location the spawn system wants to spawn a thing at</param>
    /// <param name="spawnSystem">The spawn system that is requesting the spawn to happen</param>
    /// <returns>
    ///   A spawn queue that on each next call spawns one thing. Null if this spawner doesn't spawn entities,
    ///   for example compound clouds.
    /// </returns>
    public abstract SpawnQueue? Spawn(IWorldSimulation worldSimulation, Vector3 location, ISpawnSystem spawnSystem);
}
