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
    private PatchManager patchManager;

    public Microbe Player { get; private set; }

    public MicrobeCamera Camera { get; private set; }

    public MicrobeHUD HUD { get; private set; }

    public CompoundCloudSystem Clouds { get; private set; }

    public FluidSystem FluidSystem { get; private set; }

    public TimedLifeSystem TimedLifeSystem { get; private set; }

    public TimedWorldOperations TimedEffects { get; private set; }

    public ProcessSystem ProcessSystem { get; private set; }

    public GameWorld GameWorld { get; private set; }

    /// <summary>
    ///   This should get called the first time the stage scene is put
    ///   into an active scene tree. So returning from the editor
    ///   might be safe without it unloading this.
    /// </summary>
    public override void _Ready()
    {
        world = GetNode<Node>("World");
        HUD = GetNode<MicrobeHUD>("MicrobeHUD");
        rootOfDynamicallySpawned = GetNode<Node>("World/DynamicallySpawned");
        spawner = new SpawnSystem(rootOfDynamicallySpawned);
        Camera = world.GetNode<MicrobeCamera>("PrimaryCamera");
        Clouds = world.GetNode<CompoundCloudSystem>("CompoundClouds");
        TimedLifeSystem = new TimedLifeSystem(rootOfDynamicallySpawned);
        ProcessSystem = new ProcessSystem(rootOfDynamicallySpawned);
        microbeAISystem = new MicrobeAISystem(rootOfDynamicallySpawned);

        patchManager = new PatchManager(spawner, ProcessSystem, Clouds, TimedLifeSystem);

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
        Clouds.Init(FluidSystem);

        if (GameWorld == null)
        {
            StartNewGame();
        }
    }

    public void StartNewGame()
    {
        GameWorld = new GameWorld(new WorldGenerationSettings());

        patchManager.ApplyChangedPatchSettingsIfNeeded(GameWorld.Map.CurrentPatch);

        // Register glucose reduction
        TimedEffects.RegisterEffect("reduce_glucose", new WorldEffectLambda((elapsed, total) =>
        {
            foreach (var key in GameWorld.Map.Patches.Keys)
            {
                var patch = GameWorld.Map.Patches[key];

                foreach (var compound in patch.Biome.Compounds.Keys)
                {
                    if (compound == "glucose")
                    {
                        var data = patch.Biome.Compounds[compound];

                        // TODO: verify that this change is picked up by the patch manager
                        data.Density *= 0.8f;
                    }
                }
            }
        }));

        SpawnPlayer();
        Camera.ResetHeight();
    }

    /// <summary>
    ///   Spawns the player if there isn't currently a player node existing
    /// </summary>
    public void SpawnPlayer()
    {
        if (Player != null)
            return;

        Player = SpawnHelpers.SpawnMicrobe(GameWorld.PlayerSpecies, new Vector3(0, 0, 0),
            rootOfDynamicallySpawned, SpawnHelpers.LoadMicrobeScene(), false, Clouds);
        Player.AddToGroup("player");

        Camera.ObjectToFollow = Player;
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
