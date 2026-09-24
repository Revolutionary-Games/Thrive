using System;

/// <summary>
///   Transport layer
/// </summary>
/// <remarks>
///   <para>
///     All methods must be called from the thread that owns the transport. These are not thread safe.
///   </para>
/// </remarks>
public interface ITransport : IDisposable
{
    public bool IsRunning { get; }

    /// <summary>
    ///   True when this transport is acting as the server
    /// </summary>
    public bool IsServer { get; }

    /// <summary>
    ///   Starts listening for connections
    /// </summary>
    /// <param name="address">
    ///   Transport specific address to listen on. For a socket transport this is a port number, for a relay based
    ///   transport this may be a lobby identifier.
    /// </param>
    /// <param name="maxPeers">Maximum number of peers allowed to be connected at once</param>
    public bool StartServer(string address, int maxPeers);

    /// <summary>
    ///   Starts connecting to a server. Completion is reported later as a
    ///   <see cref="TransportEventType.PeerConnected"/> event.
    /// </summary>
    /// <param name="address">Transport specific address of the server to connect to</param>
    public bool StartClient(string address);

    /// <summary>
    ///   Queues a message for sending
    /// </summary>
    /// <param name="peerId">
    ///   Peer to send to. Clients must use <see cref="NetworkConstants.SERVER_PEER_ID"/>.
    /// </param>
    /// <param name="data">Data to send, which is copied and does not need to be kept valid after this call</param>
    /// <param name="delivery">Delivery guarantee to use</param>
    /// <param name="channel">Channel to send on</param>
    public void Send(int peerId, ReadOnlySpan<byte> data, NetworkDelivery delivery, NetworkChannel channel);

    /// <summary>
    ///   Takes the next pending event. Must be called regularly, as some transports only process their sockets
    ///   while being polled.
    /// </summary>
    /// <param name="transportEvent">Set to the event that happened, when this returns true</param>
    /// <returns>True when an event was returned, false when there is nothing pending</returns>
    public bool PollEvent(out TransportEvent transportEvent);

    /// <summary>
    ///   Disconnects a single peer. Only valid on a server.
    /// </summary>
    public void DisconnectPeer(int peerId, string reason = "");

    /// <summary>
    ///   Estimated round trip time to a peer in seconds, or a negative value when not known yet
    /// </summary>
    public float GetRoundTripTime(int peerId);

    /// <summary>
    ///   Stops the transport and disconnects everyone. The transport may be started again afterwards.
    /// </summary>
    public void Stop();
}
