/// <summary>
///   Constants shared by the whole networking stack.
/// </summary>
public static class NetworkConstants
{
    /// <summary>
    ///   Peer ID that always refers to the server.
    /// </summary>
    public const int SERVER_PEER_ID = 1;

    /// <summary>
    ///   Peer ID value meaning no peer
    /// </summary>
    public const int INVALID_PEER_ID = 0;

    /// <summary>
    ///   Number of channels a transport must support.
    /// </summary>
    public const int CHANNEL_COUNT = 4;

    /// <summary>
    ///   Maximum size of a single message.
    /// </summary>
    public const int MAX_MESSAGE_SIZE = 1024 * 512;

    /// <summary>
    ///   Size over which a message should be sent on <see cref="NetworkChannel.Bulk"/> to avoid delaying
    ///   gameplay data
    /// </summary>
    public const int BULK_MESSAGE_THRESHOLD = 4096;

    /// <summary>
    ///   Longest allowed player name
    /// </summary>
    public const int MAX_PLAYER_NAME_LENGTH = 32;

    /// <summary>
    ///   Longest allowed chat message
    /// </summary>
    public const int MAX_CHAT_LENGTH = 255;

    /// <summary>
    ///   Seconds a connected peer may take to complete the handshake before it is dropped
    /// </summary>
    public const float HANDSHAKE_TIMEOUT = 10;

    /// <summary>
    ///   How often an entity's full state is resent to a peer even when nothing changed
    /// </summary>
    public const uint SNAPSHOT_FORCED_FULL_UPDATE_TICKS = 300;

    /// <summary>
    ///   Size a snapshot packet is allowed to reach before the rest of the entities are left for the next one
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Kept below a typical MTU so packets are not fragmented. A fragmented unreliable datagram is lost
    ///     entirely when any one fragment is, which makes large packets progressively less likely to arrive.
    ///   </para>
    ///   <para>
    ///     Entities left out stay unconfirmed and go in a later packet, so nothing is lost by capping this.
    ///   </para>
    /// </remarks>
    public const int SNAPSHOT_MAX_PACKET_SIZE = 1200;

    /// <summary>
    ///   Version of the wire protocol. Must be incremented whenever the meaning of any message changes so that
    ///   mismatched clients are rejected at handshake instead of misreading data.
    /// </summary>
    public const ushort PROTOCOL_VERSION = 3;
}
