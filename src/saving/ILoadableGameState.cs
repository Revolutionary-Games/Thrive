using Godot;
using Newtonsoft.Json;

/// <summary>
///   Game state interface for callbacks after loading
/// </summary>
public interface ILoadableGameState
{
    Node GameStateRoot { get; }

    [JsonIgnore]
    bool IsLoadedFromSave { get; set; }

    void OnFinishLoading(Save save);
}
