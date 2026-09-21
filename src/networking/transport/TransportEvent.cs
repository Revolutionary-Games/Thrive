/// <summary>
///   Kind of a <see cref="TransportEvent"/>
/// </summary>
public enum TransportEventType
{
    /// <summary>
    ///   No event
    /// </summary>
    None = 0,

    PeerConnected = 1,
    PeerDisconnected = 2,
    DataReceived = 3,
}

/// <summary>
///   A single thing that happened in an <see cref="ITransport"/> since it was last polled
/// </summary>
public struct TransportEvent
{
    public TransportEventType Type;

    /// <summary>
    ///   Peer this event relates to.
    /// </summary>
    public int PeerId;

    /// <summary>
    ///   Received data for <see cref="TransportEventType.DataReceived"/>. Only the first <see cref="DataLength"/>
    ///   bytes are valid.
    /// </summary>
    public byte[]? Data;

    public int DataLength;

    public NetworkChannel Channel;
}
