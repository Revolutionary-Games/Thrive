﻿using Newtonsoft.Json;

/// <summary>
///   See the documentation on <see cref="ICreatureStageHUD"/>
/// </summary>
public interface ICreatureStage : IStageBase, IReturnableGameState
{
    [JsonIgnore]
    public bool HasPlayer { get; }

    /// <summary>
    ///   True when <see cref="HasPlayer"/> and the player is alive.
    /// </summary>
    [JsonIgnore]
    public bool HasAlivePlayer { get; }

    [JsonIgnore]
    public bool MovingToEditor { get; set; }

    public void OnSuicide();

    public void MoveToEditor();

    public void MoveToPatch(Patch patch);

    /// <summary>
    ///   Causes the game to switch the given species to be the player species and continuing
    /// </summary>
    /// <param name="species">Species to play as next</param>
    public void ContinueGameAsSpecies(Species species);

    public void SetSpecialViewMode(ViewMode mode);
}
