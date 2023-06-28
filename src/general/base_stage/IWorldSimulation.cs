using System;
using DefaultEcs;
using DefaultEcs.Command;

/// <summary>
///   Interface for <see cref="WorldSimulation"/> to give flexibility for swapping out things
/// </summary>
public interface IWorldSimulation : IEntityContainer, IDisposable
{
    /// <summary>
    ///   Access to the ECS system for adding and modifying components. Note that entity modification is not allowed
    ///   during certain system running. For that see <see cref="StartRecordingEntityCommands"/>
    /// </summary>
    public World EntitySystem { get; }

    /// <summary>
    ///   Thread safe variant of <see cref="IEntityContainer.CreateEmptyEntity"/>
    /// </summary>
    /// <returns>Record of the deferred entity creation referring to it</returns>
    public EntityRecord CreateEntityDeferred(WorldRecord activeRecording);

    /// <summary>
    ///   Checks that the entity is in this world and is not being deleted
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>True when the entity is in this world and is not queued for deletion</returns>
    public bool IsEntityInWorld(Entity entity);

    /// <summary>
    ///   Returns true when the given entity is queued for destruction
    /// </summary>
    public bool IsQueuedForDeletion(Entity entity);

    /// <summary>
    ///   Starts recording a new set of entity commands. The commands will be applied automatically near the end of the
    ///   current (or next) entity update cycle.
    /// </summary>
    /// <returns>An object that records instead of applies the entity modification commands performed on it</returns>
    public EntityCommandRecorder StartRecordingEntityCommands();

    /// <summary>
    ///   Activates a recorder and gets an entity manager proxy that can be used to perform entity operations
    /// </summary>
    /// <param name="recorder">
    ///   The recorder in use by the caller (received from <see cref="StartRecordingEntityCommands"/>)
    /// </param>
    /// <returns>An entity manager instance that is safe to call entity modification operations on</returns>
    public WorldRecord GetRecorderWorld(EntityCommandRecorder recorder);

    /// <summary>
    ///   Notify that the code using a command recorder is now done. This must be called when done with the recorder,
    ///   otherwise an error will be reported about an unfinished recorder. This allows early reuse of the recorder
    ///   as well.
    /// </summary>
    /// <param name="recorder">The recorder to return</param>
    public void FinishRecordingEntityCommands(EntityCommandRecorder recorder);
}
