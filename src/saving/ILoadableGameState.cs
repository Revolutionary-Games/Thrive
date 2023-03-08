using Godot;

/// <summary>
///   Game state interface for callbacks after loading
/// </summary>
public interface ILoadableGameState : ISaveLoadedTracked
{
    public Node GameStateRoot { get; }

    public void OnFinishLoading(Save save);
}
