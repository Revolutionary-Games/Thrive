using Godot;

/// <summary>
///   Spawns AI cells and other environmental things as the player moves around
/// </summary>
public class SpawnSystem
{
    /// <summary>
    ///   Sets how often the spawn system runs and checks things
    /// </summary>
    private float interval = 1.0f;

    private float elapsed = 0.0f;

    private Node worldRoot;

    public SpawnSystem(Node root)
    {
        worldRoot = root;
    }

    // Processes spawning and despawning things
    public void Process(float delta)
    {
        elapsed += delta;

        while (elapsed >= interval)
        {
            elapsed -= interval;
        }
    }

    /// <summary>
    ///   Prepares the spawn system for a new game
    /// </summary>
    public void Init()
    {
    }
}
