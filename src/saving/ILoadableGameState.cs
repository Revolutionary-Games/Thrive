using Godot;

/// <summary>
///   Game state interface for callbacks after loading
/// </summary>
public interface ILoadableGameState : ISaveLoadedTracked
{
    public Node GameStateRoot { get; }

    /// <summary>
    ///   Called by the overall saving system when the save is finished loading. Called after the scene is attached.
    /// </summary>
    /// <param name="save">The save this state was loaded from</param>
    public void OnFinishLoading(Save save);
}
