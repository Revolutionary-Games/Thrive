using Newtonsoft.Json;

/// <summary>
///   See the documentation on <see cref="ICreatureStageHUD"/>
/// </summary>
public interface ICreatureStage : IStageBase, IReturnableGameState
{
    [JsonIgnore]
    public bool HasPlayer { get; }

    [JsonIgnore]
    public bool MovingToEditor { get; set; }

    public void OnSuicide();

    public void MoveToEditor();

    public void MoveToPatch(Patch patch);
}
