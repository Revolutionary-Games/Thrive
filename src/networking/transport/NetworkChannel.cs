public enum NetworkChannel : byte
{
    /// <summary>
    ///   Session level messages
    /// </summary>
    Control = 0,

    /// <summary>
    ///   Entity snapshots sent by the server
    /// </summary>
    Snapshot = 1,

    /// <summary>
    ///   Player input sent by clients
    /// </summary>
    Input = 2,

    /// <summary>
    ///   Large transfers
    /// </summary>
    Bulk = 3,
}
