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
    ///   Version of the wire protocol. Must be incremented whenever the meaning of any message changes so that
    ///   mismatched clients are rejected at handshake instead of misreading data.
    /// </summary>
    public const ushort PROTOCOL_VERSION = 1;
}
