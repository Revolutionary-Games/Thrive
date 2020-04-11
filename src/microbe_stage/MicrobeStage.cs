using System;
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

    /// <summary>
    ///   Used to differentiate between spawning the player and respawning
    /// </summary>
    private bool spawnedPlayer = false;

    /// <summary>
    ///   True when the player is extinct
    /// </summary>
    private bool gameOver = false;

    private float playerRespawnTimer;

    public Microbe Player { get; private set; }

    public MicrobeCamera Camera { get; private set; }

    public MicrobeHUD HUD { get; private set; }

    public CompoundCloudSystem Clouds { get; private set; }

    public FluidSystem FluidSystem { get; private set; }

    public TimedLifeSystem TimedLifeSystem { get; private set; }

    public ProcessSystem ProcessSystem { get; private set; }

    /// <summary>
    ///   The main current game object holding various details
    /// </summary>
    public GameProperties CurrentGame { get; set; }

    public GameWorld GameWorld
    {
        get
        {
            return CurrentGame.GameWorld;
        }
    }

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

        spawner.Init();
        Clouds.Init(FluidSystem);

        if (CurrentGame == null)
        {
            StartNewGame();
        }

        // This needs the GameWorld so this is created here
        patchManager = new PatchManager(spawner, ProcessSystem, Clouds, TimedLifeSystem,
            GameWorld);
    }

    public void StartNewGame()
    {
        CurrentGame = GameProperties.StartNewMicrobeGame();

        patchManager.ApplyChangedPatchSettingsIfNeeded(GameWorld.Map.CurrentPatch);

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
            rootOfDynamicallySpawned, SpawnHelpers.LoadMicrobeScene(), false, Clouds,
            GameWorld);
        Player.AddToGroup("player");

        Camera.ObjectToFollow = Player;

        if (spawnedPlayer)
        {
            // Random location on respawn
            var random = new Random();
            Player.Translation = new Vector3(
                random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE), 0,
                random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE));
        }

        spawnedPlayer = true;
        playerRespawnTimer = Constants.PLAYER_RESPAWN_TIME;
    }

    public override void _Process(float delta)
    {
        FluidSystem.Process(delta);
        TimedLifeSystem.Process(delta);
        ProcessSystem.Process(delta);
        microbeAISystem.Process(delta);

        if (gameOver)
        {
            // Player is extinct and has lost the game
            // TODO: show the game lost popup if not already visible
            return;
        }

        if (Player != null)
        {
            spawner.Process(delta, Player.Translation);
            Clouds.ReportPlayerPosition(Player.Translation);
        }
        else
        {
            if (!spawnedPlayer)
            {
                GD.PrintErr("MicrobeStage was entered without spawning the player");
                SpawnPlayer();
            }
            else
            {
                // Respawn the player once the timer is up
                playerRespawnTimer -= delta;

                if (playerRespawnTimer <= 0)
                {
                    HandlePlayerRespawn();
                }
            }
        }
    }

    /// <summary>
    ///   Called when returning from the editor
    /// </summary>
    public void OnReturnFromEditor()
    {
        // Now the editor increases the generation so we don't do that here anymore

        // TODO: fix
        // // Call event that checks win conditions
        // if(!GetThriveGame().playerData().isFreeBuilding()){
        //     GenericEvent@ event = GenericEvent("CheckWin");
        //     NamedVars@ vars = event.GetNamedVars();
        //     vars.AddValue(ScriptSafeVariableBlock("generation", playerSpecies.generation));
        //     vars.AddValue(ScriptSafeVariableBlock("population", playerSpecies.population));
        //     GetEngine().GetEventHandler().CallEvent(event);
        // }

        // Make sure player is spawned
        SpawnPlayer();

        Player.ApplySpecies(Player.Species);

        // Spawn another cell from the player species
        Player.ResetOrganelleLayout();

        Player.Divide();
    }

    /// <summary>
    ///   Handles respawning the player and checking for extinction
    /// </summary>
    private void HandlePlayerRespawn()
    {
        var playerSpecies = GameWorld.PlayerSpecies;

        // Decrease the population by 20
        GameWorld.AlterSpeciesPopulation(playerSpecies, -20, "player died", true);

        // Respawn if not extinct (or freebuild)
        if (playerSpecies.Population <= 0 && !CurrentGame.FreeBuild)
        {
            gameOver = true;
        }
        else
        {
            // Player is not extinct, so can respawn
            SpawnPlayer();
        }
    }
}
