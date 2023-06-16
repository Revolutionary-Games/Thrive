using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base interface for spawn systems for the stages
/// </summary>
[JsonObject(IsReference = true)]
public interface ISpawnSystem
{
    /// <summary>
    ///   Prepares the spawn system for a new game
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: if later spawn systems than microbe don't need this either, this should probably be refactored out
    ///   </para>
    /// </remarks>
    public void Init();

    /// <summary>
    ///   Clears the registered spawners
    /// </summary>
    public void Clear();

    /// <summary>
    ///   Despawns all spawned entities
    /// </summary>
    public void DespawnAll();

    /// <summary>
    ///   Processes spawning and despawning things
    /// </summary>
    public void Process(float delta, Vector3 playerPosition);

    /// <summary>
    ///   Adds an externally spawned entity to be despawned and tracked by the system
    /// </summary>
    public void NotifyExternalEntitySpawned(ISpawned entity);

    /// <summary>
    ///   Checks if the approximate entity count is not too much over the entity limit
    /// </summary>
    /// <returns>True if creatures are allowed to spawn offspring entities</returns>
    public bool IsUnderEntityLimitForReproducing();
}
