namespace Components;

/// <summary>
///   Marks an entity as replicated and carries the ID both sides know it by
/// </summary>
/// <remarks>
///   <para>
///     Arch entity IDs are not stable, and are remapped on load, so they cannot be used to refer to the same
///     entity on two machines. The server assigns these IDs instead and sends them with every snapshot.
///   </para>
/// </remarks>
[ComponentIsReadByDefault]
public struct NetworkEntity(uint id, ushort archetypeId, int owningPeerId)
{
    /// <summary>
    ///   Server-assigned ID. Not zero for a live entity.
    /// </summary>
    public uint Id = id;

    /// <summary>
    ///   Which spawn recipe created this entity, so a client can build its own copy
    /// </summary>
    public ushort ArchetypeId = archetypeId;

    /// <summary>
    ///   Peer this entity belongs to, or <see cref="NetworkConstants.INVALID_PEER_ID"/> when it is not a
    ///   player's entity.
    /// </summary>
    public int OwningPeerId = owningPeerId;
}
