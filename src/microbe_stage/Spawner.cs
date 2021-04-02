/// <summary>
///   Spawner that can be added to a SpawnSystem to be used for spawning things
/// </summary>
public abstract class Spawner
{
    /// <summary>
    ///   The distance at which spawning happens
    /// </summary>
    public int SpawnRadius { get; set; }

    /// <summary>
    ///   Minimum spawn distance allowed
    /// </summary>
    /// <value>The minimum allowed spawn radius.</value>
    public float MinSpawnRadius { get; set; }

    /// <summary>
    ///   How much stuff spawns
    /// </summary>
    /// <value>The spawn frequency.</value>
    public int SpawnFrequency { get; set; }

    public void SetSpawnRadius(int spawnRadius)
    {
        SpawnRadius = spawnRadius;
        SpawnFrequency = 122;

        float minSpawnRadius = spawnRadius * Constants.MIN_SPAWN_RADIUS_RATIO;
        MinSpawnRadius = minSpawnRadius;
    }
}
