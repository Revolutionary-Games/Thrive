using Godot;

/// <summary>
///   Main class for managing the microbe stage
/// </summary>
public class MicrobeStage : Node
{
    private Node world;
    private Node rootOfDynamicallySpawned;
    private SpawnSystem spawner;
    private MicrobeAISystem microbeAISystem;

    private PackedScene playerScene;
    public Microbe Player { get; private set; }

    public MicrobeCamera Camera { get; private set; }

    public MicrobeHUD HUD { get; private set; }

    public CompoundCloudSystem Clouds { get; private set; }

    public FluidSystem FluidSystem { get; private set; }

    public TimedLifeSystem TimedLifeSystem { get; private set; }

    public TimedWorldOperations TimedEffects { get; private set; }

    public ProcessSystem ProcessSystem { get; private set; }

    /// <summary>
    ///   This should get called the first time the stage scene is put
    ///   into an active scene tree. So returning from the editor
    ///   might be safe without it unloading this.
    /// </summary>
    public override void _Ready()
    {
        playerScene = GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");

        world = GetNode<Node>("World");
        HUD = GetNode<MicrobeHUD>("MicrobeHUD");
        rootOfDynamicallySpawned = GetNode<Node>("World/DynamicallySpawned");
        spawner = new SpawnSystem(rootOfDynamicallySpawned);
        Camera = world.GetNode<MicrobeCamera>("PrimaryCamera");
        Clouds = world.GetNode<CompoundCloudSystem>("CompoundClouds");
        TimedLifeSystem = new TimedLifeSystem(rootOfDynamicallySpawned);
        ProcessSystem = new ProcessSystem(rootOfDynamicallySpawned);
        microbeAISystem = new MicrobeAISystem(rootOfDynamicallySpawned);

        HUD.Init(this);

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

        // Make sure settings is loaded
        if (Settings.Instance == null)
            GD.PrintErr("Settings load problem");

        FluidSystem = new FluidSystem();
        TimedEffects = new TimedWorldOperations();

        spawner.Init();
        SpawnPlayer();
        Camera.ResetHeight();
        Clouds.Init(FluidSystem);

        ProcessSystem.SetBiome(SimulationParameters.Instance.GetBiome("default"));

        // Register glucose reduction
        TimedEffects.RegisterEffect("reduce_glucose", new WorldEffectLambda((elapsed, total) =>
        {
            GD.Print("TODO: reduce glucose");
        }));
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
        Player.AddToGroup("process");

        Camera.ObjectToFollow = Player;

        // For testing purposes only, please remove on release.
        PlacedOrganelle testOrganelle = new PlacedOrganelle();
        testOrganelle.Definition = SimulationParameters.Instance.GetOrganelleType("nucleus");
        testOrganelle.OnAddedToMicrobe(Player, new Hex(0, -5), 3);
    }

    public override void _Process(float delta)
    {
        FluidSystem.Process(delta);
        TimedLifeSystem.Process(delta);
        ProcessSystem.Process(delta);
        microbeAISystem.Process(delta);

        if (Player != null)
        {
            spawner.Process(delta, Player.Translation);
            Clouds.ReportPlayerPosition(Player.Translation);
        }
    }
}
