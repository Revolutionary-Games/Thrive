/// <summary>
///   Identifies what a network message contains. This is the first byte of every message.
/// </summary>
/// <remarks>
///   <para>
///     Values must *never* be reused for a different meaning. Add new values at the end and bump
///     <see cref="NetworkConstants.PROTOCOL_VERSION"/> instead.
///   </para>
/// </remarks>
public enum MessageType : byte
{
    // Session and lobby, on NetworkChannel.Control

    /// <summary>
    ///   First message a client sends, carrying the protocol version and an identity ticket
    /// </summary>
    Handshake = 1,

    HandshakeAccepted = 2,
    HandshakeRejected = 3,
    PeerJoined = 4,
    PeerLeft = 5,
    LobbyState = 6,
    PlayerReadyState = 7,
    Chat = 8,
    Ping = 9,
    Pong = 10,
    Disconnect = 11,

    // Match flow, on NetworkChannel.Control

    MatchStateChanged = 20,
    PlayerScoreUpdate = 21,

    // Replication, on NetworkChannel.Snapshot unless noted

    /// <summary>
    ///   Full world state for a joining client, sent on <see cref="NetworkChannel.Bulk"/>
    /// </summary>
    JoinSnapshot = 30,

    EntitySpawn = 31,
    EntityDespawn = 32,
    Snapshot = 33,

    /// <summary>
    ///   Client acknowledging the newest snapshot it has, so the server knows what to delta against
    /// </summary>
    SnapshotAcknowledgement = 34,

    // Client to server, on NetworkChannel.Input

    PlayerInput = 40,

    /// <summary>
    ///   A species definition, sent on <see cref="NetworkChannel.Bulk"/> when a player finishes editing
    /// </summary>
    SpeciesData = 41,
}
