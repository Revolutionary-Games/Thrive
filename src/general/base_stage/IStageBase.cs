﻿using Newtonsoft.Json;

/// <summary>
///   Truly core things a stage needs
/// </summary>
public interface IStageBase : ILoadableGameState, ICurrentGameInfo
{
    [JsonIgnore]
    public bool TransitionFinished { get; set; }

    public GameWorld GameWorld { get; }

    [JsonIgnore]
    public MainGameState GameState { get; }

    /// <summary>
    ///   Called by the HUD when the stage has faded in from a black screen
    /// </summary>
    public void OnFinishTransitioning();

    /// <summary>
    ///   Called on the fully black frame just before the stage starts to fade in, this is the last time to load
    ///   something that causes a lag spike
    /// </summary>
    public void OnBlankScreenBeforeFadeIn();
}
