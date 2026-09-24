/// <summary>
///   A single connected peer as the local side knows it
/// </summary>
public class NetworkPeer(int peerId)
{
    public int PeerId { get; } = peerId;

    /// <summary>
    ///   Who this peer is. Valid once <see cref="HandshakeComplete"/> is true.
    /// </summary>
    public PlayerIdentity Identity { get; set; }

    public bool HandshakeComplete { get; set; }

    public bool Ready { get; set; }

    public float RoundTripTime { get; set; } = -1;

    /// <summary>
    ///   Newest snapshot tick this peer confirmed receiving, used as the delta baseline
    /// </summary>
    public uint AcknowledgedSnapshotTick { get; set; }

    /// <summary>
    ///   Newest input tick received from this peer
    /// </summary>
    public uint LastInputTick { get; set; }

    /// <summary>
    ///   Seconds since this peer connected without completing the handshake
    /// </summary>
    public float TimeWaitingForHandshake { get; set; }
}
