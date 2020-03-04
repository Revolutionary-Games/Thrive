using Godot;

/// <summary>
///   Main class for managing the microbe stage
/// </summary>
public class MicrobeStage : Node
{
    private Node world;
    private Node rootOfDynamicallySpawned;
    private SpawnSystem spawner;

    private PackedScene playerScene;
    public Microbe Player { get; private set; }

    /// <summary>
    ///   This should get called the first time the stage scene is put
    ///   into an active scene tree. So returning from the editor
    ///   might be safe without it unloading this.
    /// </summary>
    public override void _Ready()
    {
        playerScene = GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");

        world = GetNode<Node>("World");
        rootOfDynamicallySpawned = GetNode<Node>("World/DynamicallySpawned");
        spawner = new SpawnSystem(rootOfDynamicallySpawned);

        // Do stage setup to spawn things and setup all parts of the stage
        SetupStage();
    }

    // Prepares the stage for playing
    // Also begins a new game if one hasn't been started yet for easier debugging
    public void SetupStage()
    {
        // Make sure simulation parameters is loaded
        if (SimulationParameters.Instance == null)
            GD.PrintErr("Something bad happened with SimulationParameters loading");

        spawner.Init();
        SpawnPlayer();
    }

    /// <summary>
    ///   Spawns the player if there isn't currently a player node existing
    /// </summary>
    public void SpawnPlayer()
    {
        if (Player != null)
            return;

        Player = (Microbe)playerScene.Instance();
        rootOfDynamicallySpawned.AddChild(Player);
        Player.AddToGroup("player");
    }

    public override void _Process(float delta)
    {
        spawner.Process(delta);
    }
}
