using Newtonsoft.Json;

/// <summary>
///   See the documentation on <see cref="IStageHUD"/>
/// </summary>
public interface IStage : IReturnableGameState
{
    [JsonIgnore]
    public bool HasPlayer { get; }

    [JsonIgnore]
    public bool MovingToEditor { get; set; }

    [JsonIgnore]
    public bool TransitionFinished { get; set; }

    public GameWorld GameWorld { get; }

    public void OnSuicide();

    /// <summary>
    ///   Called by the HUD when the stage has faded in from a black screen
    /// </summary>
    public void OnFinishTransitioning();

    public void MoveToEditor();
}
