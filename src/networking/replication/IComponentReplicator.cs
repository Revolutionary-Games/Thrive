using Arch.Core;

/// <summary>
///   Writes and reads one component type over the network
/// </summary>
public interface IComponentReplicator
{
    /// <summary>
    ///   Network ID of the component type.
    /// </summary>
    public byte ComponentNetworkId { get; set; }

    /// <summary>
    ///   Whether a client should interpolate this component between snapshots
    /// </summary>
    public bool Interpolated { get; }

    /// <summary>
    ///   True when the entity has this component and it should be sent to the given peer
    /// </summary>
    public bool ShouldSend(in Entity entity, int peerId);

    /// <summary>
    ///   Writes the component of an entity. Only called when <see cref="ShouldSend"/> returned true.
    /// </summary>
    public void Write(in Entity entity, NetworkWriter writer);

    /// <summary>
    ///   Applies received data to a client side entity, adding the component if it is missing
    /// </summary>
    public void Read(in Entity entity, NetworkReader reader);

    /// <summary>
    ///   Skips this component's data without applying it, used when the entity is unknown or was already
    ///   destroyed locally
    /// </summary>
    public void Skip(NetworkReader reader);
}
