/// <summary>
///   A single byte header for differentiating raw packets (non-RPC).
/// </summary>
public enum RawPacketFlag
{
    /// <summary>
    ///   Client-to-server.
    /// </summary>
    Ping,

    /// <summary>
    ///   Server-to-client.
    /// </summary>
    Pong,
}
