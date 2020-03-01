using Godot;

public class MicrobeStage : Node
{
    private Spatial world;
    private SpawnSystem spawner;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        world = GetNode<Spatial>("World");
        spawner = new SpawnSystem(world);
        SetupStage();
    }

    // Prepares the stage for playing
    // Also begins a new game if one hasn't been started yet for easier debugging
    public void SetupStage()
    {
        spawner.Init();
    }

    public override void _Process(float delta)
    {
        spawner.Process(delta);
    }
}
