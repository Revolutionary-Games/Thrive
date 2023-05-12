using Godot;
using Newtonsoft.Json;

/// <summary>
///   Game state interface for callbacks after loading
/// </summary>
public interface ILoadableGameState : ISaveLoadedTracked
{
    [JsonIgnore]
    public Node GameStateRoot { get; }

    public void OnFinishLoading(Save save);
}
