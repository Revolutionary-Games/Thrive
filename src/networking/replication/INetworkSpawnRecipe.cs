using Arch.Core;

/// <summary>
///   Describes how to recreate one kind of entity on a client
/// </summary>
public interface INetworkSpawnRecipe
{
    /// <summary>
    ///   Assigned when registered
    /// </summary>
    public ushort ArchetypeId { get; set; }

    /// <summary>
    ///   Writes what a client needs in order to recreate this entity
    /// </summary>
    public void WriteSpawnData(in Entity entity, NetworkWriter writer);

    /// <summary>
    ///   Reads past spawn data without creating anything
    /// </summary>
    public void SkipSpawnData(NetworkReader reader);

    /// <summary>
    ///   Creates the client side copy of an entity
    /// </summary>
    /// <param name="reader">Reader positioned at the data written by <see cref="WriteSpawnData"/></param>
    /// <param name="networkEntity">Network identity to attach to the created entity</param>
    /// <returns>The created entity</returns>
    public Entity Spawn(NetworkReader reader, in Components.NetworkEntity networkEntity);
}
