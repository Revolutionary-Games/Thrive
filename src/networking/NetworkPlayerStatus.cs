/// <summary>
///   Status of a networked player with respect to the current game instance.
/// </summary>
public enum NetworkPlayerStatus
{
    /// <summary>
    ///   Player is in the lobby screen.
    /// </summary>
    Lobby,

    /// <summary>
    ///   Player is setting up their world and is receiving world data from host.
    /// </summary>
    Joining,

    /// <summary>
    ///   Player has been set up and can actively engage in gameplay.
    /// </summary>
    Active,

    /// <summary>
    ///   Player is releasing the world and its data (but not the session information).
    /// </summary>
    Leaving,
}
