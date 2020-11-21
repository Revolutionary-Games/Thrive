using Godot;

/// <summary>
///   Game state interface for callbacks after loading
/// </summary>
public interface ILoadableGameState : ISaveLoadedTracked
{
    Node GameStateRoot { get; }

    void OnFinishLoading(Save save);
}
