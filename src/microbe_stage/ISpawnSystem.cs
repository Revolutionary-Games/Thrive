using DefaultEcs.Command;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base interface for spawn systems for the stages
/// </summary>
[JsonObject(IsReference = true)]
public interface ISpawnSystem
{
    /// <summary>
    ///   Despawns all spawned entities
    /// </summary>
    public void DespawnAll();

    /// <summary>
    ///   Reports the current player position around which spawning happens. Needs to be called before
    ///   <see cref="Update"/>
    /// </summary>
    public void ReportPlayerPosition(Vector3 position);

    /// <summary>
    ///   Processes spawning and despawning things
    /// </summary>
    public void Update(float delta);

    /// <summary>
    ///   Notifies this that an externally created entity is now in the world. And needs to be despawned by this.
    ///   Used to setup the despawn radius for it and make sure entity count is up to date.
    /// </summary>
    /// <param name="entity">The entity that needs proper despawning support</param>
    /// <param name="despawnRadiusSquared">
    ///   How far the entity can be from the player before being despawned, this value needs to be squared (this is
    ///   done to speed up distance checks).
    /// </param>
    /// <param name="entityWeight">How much "space" the entity takes up in the spawn system</param>
    public void NotifyExternalEntitySpawned(in EntityRecord entity, float despawnRadiusSquared, float entityWeight);

    /// <summary>
    ///   Checks if the approximate entity count is not too much over the entity limit
    /// </summary>
    /// <returns>True if creatures are allowed to spawn offspring entities</returns>
    public bool IsUnderEntityLimitForReproducing();
}
