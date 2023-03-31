using Newtonsoft.Json;

/// <summary>
///   Truly core things a stage needs
/// </summary>
public interface IStageBase : ILoadableGameState, ICurrentGameInfo
{
    [JsonIgnore]
    public bool TransitionFinished { get; set; }

    public GameWorld GameWorld { get; }

    /// <summary>
    ///   Called by the HUD when the stage has faded in from a black screen
    /// </summary>
    public void OnFinishTransitioning();
}
