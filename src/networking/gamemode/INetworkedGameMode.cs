/// <summary>
///   The rules of one multiplayer mode
/// </summary>
public interface INetworkedGameMode
{
    /// <summary>
    ///   Internal gamemode name to check if the gamemode state of both server and client is the same.
    /// </summary>
    public string InternalName { get; }

    public MatchState CurrentState { get; }

    /// <summary>
    ///   Maximum players this mode supports in one session
    /// </summary>
    public int MaxPlayers { get; }

    /// <summary>
    ///   Whether players may join while a match is running
    /// </summary>
    public bool AllowsJoiningInProgress { get; }

    /// <summary>
    ///   Registers the components and spawn recipes this mode replicates. Called on both sides with a fresh
    ///   registry, and must register the same things in the same order on each.
    /// </summary>
    public void RegisterReplication(ReplicationRegistry registry);

    /// <summary>
    ///   Creates the interest manager for this mode
    /// </summary>
    public IInterestManager CreateInterestManager();

    /// <summary>
    ///   Creates an empty input payload of the type this mode uses, for filling in from the network or from local
    ///   input
    /// </summary>
    public IInputPayload CreateInputPayload();

    /// <summary>
    ///   Runs the match rules. Server only, called once per simulation tick.
    /// </summary>
    public void UpdateMatch(float delta);

    public void OnPlayerJoined(int peerId, in PlayerIdentity identity);

    public void OnPlayerLeft(int peerId);
}
