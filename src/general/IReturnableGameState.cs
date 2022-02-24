using Newtonsoft.Json;

public interface IReturnableGameState : ILoadableGameState
{
    [JsonProperty]
    public GameProperties? CurrentGame { get; set; }
}
