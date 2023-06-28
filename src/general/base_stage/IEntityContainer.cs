using DefaultEcs;

/// <summary>
///   Anything that supports the entity management (creation, deletion) operations
/// </summary>
public interface IEntityContainer
{
    /// <summary>
    ///   Adds an entity to this simulation / container that is empty. Note not thread safe!
    /// </summary>
    public Entity CreateEmptyEntity();

    /// <summary>
    ///   Destroys an entity (some simulations will queue destroys and only perform them at the end of the current
    ///   simulation frame)
    /// </summary>
    /// <param name="entity">Entity to destroy</param>
    /// <returns>True when destroyed, false if the entity was not added</returns>
    public bool DestroyEntity(Entity entity);

    /// <summary>
    ///   Destroys all entities in this container
    /// </summary>
    /// <param name="skip">An optional entity to skip deleting</param>
    public void DestroyAllEntities(Entity? skip = null);
}
