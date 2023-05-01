using Newtonsoft.Json;

public interface IStrategyStageHUD : IStageHUD
{
    [JsonIgnore]
    public bool Paused { get; }

    public void PauseButtonPressed(bool paused);

    public void UpdateResourceDisplay(SocietyResourceStorage resourceStorage);

    public void OpenResearchScreen();

    public void UpdateScienceSpeed(float speed);
    public void UpdateResearchProgress(TechnologyProgress? currentResearch);
}
