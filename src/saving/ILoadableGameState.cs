using Godot;

/// <summary>
///   Game state interface for callbacks after loading
/// </summary>
public interface ILoadableGameState
{
    Node GameStateRoot { get; }

    bool IsLoadedFromSave { get; set; }

    void OnFinishLoading(Save save);
}
