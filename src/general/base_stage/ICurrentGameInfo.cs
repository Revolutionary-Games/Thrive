using Newtonsoft.Json;

/// <summary>
///   Type that holds info about the currently played game
/// </summary>
public interface ICurrentGameInfo
{
    /// <summary>
    ///   The current game data. Only null when the object is not fully initialized yet (stages auto start a new game
    ///   if missing)
    /// </summary>
    [JsonProperty]
    public GameProperties? CurrentGame { get; set; }
}
