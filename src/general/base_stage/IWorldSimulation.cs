using System;
using DefaultEcs;

/// <summary>
///   Interface for <see cref="WorldSimulation"/> to give flexibility for swapping out things
/// </summary>
public interface IWorldSimulation : IEntityContainer, IDisposable
{
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
}
