using Arch.Core;

/// <summary>
///   Decides which entities a peer is told about
/// </summary>
public interface IInterestManager
{
    /// <summary>
    ///   Called once per snapshot before any <see cref="IsRelevant"/> call, for gathering per-peer data such as
    ///   where that peer's cell currently is
    /// </summary>
    public void PrepareForPeer(int peerId, in Entity peerEntity);

    public bool IsRelevant(int peerId, in Entity entity);
}

/// <summary>
///   Sends every replicated entity to every peer
/// </summary>
public class ReplicateEverythingInterestManager : IInterestManager
{
    public void PrepareForPeer(int peerId, in Entity peerEntity)
    {
    }

    public bool IsRelevant(int peerId, in Entity entity)
    {
        return true;
    }
}
