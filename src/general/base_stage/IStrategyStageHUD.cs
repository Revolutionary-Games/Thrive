using Newtonsoft.Json;

public interface IStrategyStageHUD : IStageHUD
{
    [JsonIgnore]
    public bool Paused { get; }

    public void PauseButtonPressed(bool paused);
}
