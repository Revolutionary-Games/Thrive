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

    private bool wonOnce = false;

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
        FluidSystem = new FluidSystem(rootOfDynamicallySpawned);

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

        spawner.Init();
        Clouds.Init(FluidSystem);

        if (CurrentGame == null)
        {
            StartNewGame();
        }

        CreatePatchManagerIfNeeded();

        StartMusic();
    }

    public void StartNewGame()
    {
        CurrentGame = GameProperties.StartNewMicrobeGame();

        CreatePatchManagerIfNeeded();

        patchManager.ApplyChangedPatchSettingsIfNeeded(GameWorld.Map.CurrentPatch);
        HUD.UpdatePatchInfo(GameWorld.Map.CurrentPatch.Name);
        UpdateBackground();

        SpawnPlayer();
        Camera.ResetHeight();
    }

    public void StartMusic()
    {
        Jukebox.Instance.PlayingCategory = "MicrobeStage";
        Jukebox.Instance.Resume();
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
            CurrentGame);
        Player.AddToGroup("player");

        Player.OnDeath = (microbe) =>
        {
            GD.Print("The player has died");
            Player = null;
            Camera.ObjectToFollow = null;
        };

        Player.OnReproductionStatus = (microbe, ready) =>
        {
            if (ready)
            {
                HUD.ShowReproductionDialog();
            }
            else
            {
                HUD.HideReproductionDialog();
            }
        };

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

    public override void _PhysicsProcess(float delta)
    {
        FluidSystem.PhysicsProcess(delta);
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
            // Show the game lost popup if not already visible
            HUD.ShowExtinctionBox();

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

        // Start auto-evo if not already and settings have auto-evo be started during gameplay
        if (Settings.Instance.RunAutoEvoDuringGamePlay)
            GameWorld.IsAutoEvoFinished(true);
    }

    /// <summary>
    ///   Switches to the editor
    /// </summary>
    public void MoveToEditor()
    {
        // Increase the population by the constant for the player reproducing
        var playerSpecies = GameWorld.PlayerSpecies;
        GameWorld.AlterSpeciesPopulation(
            playerSpecies, Constants.PLAYER_REPRODUCTION_POPULATION_GAIN, "player reproduced");

        var scene = GD.Load<PackedScene>("res://src/microbe_stage/editor/MicrobeEditor.tscn");

        var editor = (MicrobeEditor)scene.Instance();

        editor.CurrentGame = CurrentGame;
        editor.ReturnToStage = this;
        var parent = GetParent();
        parent.RemoveChild(this);
        parent.AddChild(editor);
    }

    /// <summary>
    ///   Called when returning from the editor
    /// </summary>
    public void OnReturnFromEditor()
    {
        patchManager.ApplyChangedPatchSettingsIfNeeded(GameWorld.Map.CurrentPatch);
        HUD.UpdatePatchInfo(GameWorld.Map.CurrentPatch.Name);
        UpdateBackground();

        // Now the editor increases the generation so we don't do that here anymore

        // Make sure player is spawned
        SpawnPlayer();

        // Check win conditions
        if (!CurrentGame.FreeBuild && Player.Species.Generation >= 20 &&
            Player.Species.Population >= 300 && !wonOnce)
        {
            HUD.ToggleWinBox();
            wonOnce = true;
        }

        // Update the player's cell
        Player.ApplySpecies(Player.Species);

        // Spawn another cell from the player species
        Player.ResetOrganelleLayout();

        Player.Divide();

        HUD.OnEnterStageTransition();
        HUD.HideReproductionDialog();

        StartMusic();
    }

    private void CreatePatchManagerIfNeeded()
    {
        if (patchManager != null)
            return;
        patchManager = new PatchManager(spawner, ProcessSystem, Clouds, TimedLifeSystem,
            CurrentGame);
    }

    /// <summary>
    ///   Handles respawning the player and checking for extinction
    /// </summary>
    private void HandlePlayerRespawn()
    {
        var playerSpecies = GameWorld.PlayerSpecies;

        // Decrease the population by the constant for the player dying
        GameWorld.AlterSpeciesPopulation(
            playerSpecies, Constants.PLAYER_DEATH_POPULATION_LOSS, "player died", true);

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

    private void UpdateBackground()
    {
        Camera.SetBackground(SimulationParameters.Instance.GetBackground(GameWorld.Map.CurrentPatch.Biome.Background));
    }
}
