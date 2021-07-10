﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main class for managing the microbe stage
/// </summary>
[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/microbe_stage/MicrobeStage.tscn")]
[DeserializedCallbackTarget]
public class MicrobeStage : NodeWithInput, ILoadableGameState, IGodotEarlyNodeResolve
{
    [Export]
    public NodePath GuidanceLinePath;

    [Export]
    public NodePath PauseMenuPath;

    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");

    private Node world;
    private Node rootOfDynamicallySpawned;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private SpawnSystem spawner;

    private MicrobeAISystem microbeAISystem;
    private PatchManager patchManager;

    private DirectionalLight worldLight;

    private MicrobeTutorialGUI tutorialGUI;
    private GuidanceLine guidanceLine;
    private Vector3? guidancePosition;
    private PauseMenu pauseMenu;

    /// <summary>
    ///   Used to control how often compound position info is sent to the tutorial
    /// </summary>
    [JsonProperty]
    private float elapsedSinceCompoundPositionCheck;

    /// <summary>
    ///   Used to differentiate between spawning the player and respawning
    /// </summary>
    [JsonProperty]
    private bool spawnedPlayer;

    /// <summary>
    ///   True when the player is extinct
    /// </summary>
    [JsonProperty]
    private bool gameOver;

    [JsonProperty]
    private bool wonOnce;

    [JsonProperty]
    private float playerRespawnTimer;

    /// <summary>
    ///   True if auto save should trigger ASAP
    /// </summary>
    private bool wantsToSave;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public CompoundCloudSystem Clouds { get; private set; }

    [JsonIgnore]
    public FluidSystem FluidSystem { get; private set; }

    [JsonIgnore]
    public TimedLifeSystem TimedLifeSystem { get; private set; }

    [JsonIgnore]
    public ProcessSystem ProcessSystem { get; private set; }

    /// <summary>
    ///   The main camera, needs to be after anything with AssignOnlyChildItemsOnDeserialize due to load order
    /// </summary>
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public MicrobeCamera Camera { get; private set; }

    [JsonIgnore]
    public MicrobeHUD HUD { get; private set; }

    /// <summary>
    ///   The current player or null. Due to references on save load this needs to be after the systems
    /// </summary>
    [JsonProperty]
    public Microbe Player { get; private set; }

    /// <summary>
    ///   The main current game object holding various details
    /// </summary>
    [JsonProperty]
    public GameProperties CurrentGame { get; set; }

    /// <summary>
    ///   All compounds the user is hovering over
    /// </summary>
    [JsonIgnore]
    public Dictionary<Compound, float> CompoundsAtMouse { get; private set; }

    /// <summary>
    ///   All microbes the user is hovering over
    /// </summary>
    [JsonIgnore]
    public List<Microbe> MicrobesAtMouse { get; private set; } = new List<Microbe>();

    [JsonIgnore]
    public GameWorld GameWorld => CurrentGame.GameWorld;

    [JsonIgnore]
    public TutorialState TutorialState => CurrentGame.TutorialState;

    public Node GameStateRoot => this;

    public bool IsLoadedFromSave { get; set; }

    /// <summary>
    ///   True once stage fade-in is complete
    /// </summary>
    [JsonIgnore]
    public bool TransitionFinished { get; internal set; }

    /// <summary>
    ///   True when transitioning to the editor
    /// </summary>
    [JsonIgnore]
    public bool MovingToEditor { get; internal set; }

    [JsonIgnore]
    public bool NodeReferencesResolved { get; private set; }

    /// <summary>
    ///   List access to the dynamic entities in the stage. This is used for saving and loading
    /// </summary>
    public List<Node> DynamicEntities
    {
        get
        {
            var results = new HashSet<Node>();

            foreach (var node in rootOfDynamicallySpawned.GetChildren())
            {
                bool disposed = false;

                var casted = (Spatial)node;

                // Objects that cause disposed exceptions. Seems still pretty important to protect saving against
                // very rare issues
                try
                {
                    // Skip objects that will be deleted. This might help with Microbe saving as it might be that
                    // the contained organelles are already disposed whereas the Microbe is just only queued for
                    // deletion
                    if (casted.IsQueuedForDeletion())
                    {
                        disposed = true;
                    }
                    else
                    {
                        if (casted.Transform.origin == Vector3.Zero)
                        {
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    disposed = true;
                }

                if (!disposed)
                    results.Add(casted);
            }

            return results.ToList();
        }
        set
        {
            rootOfDynamicallySpawned.FreeChildren();

            foreach (var entity in value)
            {
                rootOfDynamicallySpawned.AddChild(entity);
            }
        }
    }

    /// <summary>
    ///   This should get called the first time the stage scene is put
    ///   into an active scene tree. So returning from the editor
    ///   might be safe without it unloading this.
    /// </summary>
    public override void _Ready()
    {
        ResolveNodeReferences();

        tutorialGUI.Visible = true;
        HUD.Init(this);

        // Do stage setup to spawn things and setup all parts of the stage
        SetupStage();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            HUD.UpdatePatchInfo(TranslationServer.Translate(CurrentGame.GameWorld.Map.CurrentPatch.Name));
        }
    }

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        world = GetNode<Node>("World");
        HUD = GetNode<MicrobeHUD>("MicrobeHUD");
        tutorialGUI = GetNode<MicrobeTutorialGUI>("TutorialGUI");
        rootOfDynamicallySpawned = GetNode<Node>("World/DynamicallySpawned");
        Camera = world.GetNode<MicrobeCamera>("PrimaryCamera");
        Clouds = world.GetNode<CompoundCloudSystem>("CompoundClouds");
        worldLight = world.GetNode<DirectionalLight>("WorldLight");
        guidanceLine = GetNode<GuidanceLine>(GuidanceLinePath);
        pauseMenu = GetNode<PauseMenu>(PauseMenuPath);

        // These need to be created here as well for child property save load to work
        TimedLifeSystem = new TimedLifeSystem(rootOfDynamicallySpawned);
        ProcessSystem = new ProcessSystem(rootOfDynamicallySpawned);
        microbeAISystem = new MicrobeAISystem(rootOfDynamicallySpawned);
        FluidSystem = new FluidSystem(rootOfDynamicallySpawned);
        spawner = new SpawnSystem(rootOfDynamicallySpawned);

        NodeReferencesResolved = true;
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

        if (!IsLoadedFromSave)
        {
            spawner.Init();

            if (CurrentGame == null)
            {
                StartNewGame();
            }
        }

        pauseMenu.GameProperties = CurrentGame ?? throw new InvalidOperationException("current game is not set");

        tutorialGUI.EventReceiver = TutorialState;

        Clouds.Init(FluidSystem);

        CreatePatchManagerIfNeeded();

        StartMusic();

        if (IsLoadedFromSave)
        {
            HUD.OnEnterStageTransition(false);
            UpdatePatchSettings(true);
        }
        else
        {
            HUD.OnEnterStageTransition(true);
            TutorialState.SendEvent(TutorialEventType.EnteredMicrobeStage, EventArgs.Empty, this);
        }
    }

    public void OnFinishLoading(Save save)
    {
        OnFinishLoading();
    }

    public void OnFinishLoading()
    {
        Camera.ObjectToFollow = Player;
    }

    public void StartNewGame()
    {
        CurrentGame = GameProperties.StartNewMicrobeGame();

        CreatePatchManagerIfNeeded();

        UpdatePatchSettings(false);

        SpawnPlayer();
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

        Player.OnDeath = OnPlayerDied;

        Player.OnReproductionStatus = OnPlayerReproductionStatusChanged;

        Player.OnUnbound = OnPlayerUnbound;

        Player.OnUnbindEnabled = OnPlayerUnbindEnabled;

        Camera.ObjectToFollow = Player;

        if (spawnedPlayer)
        {
            // Random location on respawn
            var random = new Random();
            Player.Translation = new Vector3(
                random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE), 0,
                random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE));
        }

        TutorialState.SendEvent(TutorialEventType.MicrobePlayerSpawned, new MicrobeEventArgs(Player), this);

        spawnedPlayer = true;
        playerRespawnTimer = Constants.PLAYER_RESPAWN_TIME;
    }

    public override void _PhysicsProcess(float delta)
    {
        FluidSystem.PhysicsProcess(delta);
    }

    public override void _Process(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        FluidSystem.Process(delta);
        TimedLifeSystem.Process(delta);
        ProcessSystem.Process(delta);
        microbeAISystem.Process(delta);

        UpdateMouseHover();

        if (gameOver)
        {
            guidanceLine.Visible = false;

            // Player is extinct and has lost the game
            // Show the game lost popup if not already visible
            HUD.ShowExtinctionBox();

            return;
        }

        if (Player != null)
        {
            spawner.Process(delta, Player.Translation, Player.Rotation);
            Clouds.ReportPlayerPosition(Player.Translation);

            TutorialState.SendEvent(TutorialEventType.MicrobePlayerOrientation,
                new RotationEventArgs(Player.Transform.basis, Player.RotationDegrees), this);

            TutorialState.SendEvent(TutorialEventType.MicrobePlayerCompounds,
                new CompoundBagEventArgs(Player.Compounds), this);

            TutorialState.SendEvent(TutorialEventType.MicrobePlayerTotalCollected,
                new CompoundEventArgs(Player.TotalAbsorbedCompounds), this);

            elapsedSinceCompoundPositionCheck += delta;

            if (elapsedSinceCompoundPositionCheck > Constants.TUTORIAL_COMPOUND_POSITION_UPDATE_INTERVAL)
            {
                elapsedSinceCompoundPositionCheck = 0;

                if (TutorialState.WantsNearbyCompoundInfo())
                {
                    TutorialState.SendEvent(TutorialEventType.MicrobeCompoundsNearPlayer,
                        new CompoundPositionEventArgs(Clouds.FindCompoundNearPoint(Player.Translation, glucose)),
                        this);
                }

                guidancePosition = TutorialState.GetPlayerGuidancePosition();
            }

            if (guidancePosition != null)
            {
                guidanceLine.Visible = true;
                guidanceLine.LineStart = Player.Translation;
                guidanceLine.LineEnd = guidancePosition.Value;
            }
            else
            {
                guidanceLine.Visible = false;
            }
        }
        else
        {
            guidanceLine.Visible = false;

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

        // Start auto-evo if stage entry finished, don't need to auto save,
        // settings have auto-evo be started during gameplay and auto-evo is not already started
        if (TransitionFinished && !wantsToSave && Settings.Instance.RunAutoEvoDuringGamePlay)
        {
            GameWorld.IsAutoEvoFinished(true);
        }

        // Save if wanted
        if (TransitionFinished && wantsToSave)
        {
            if (!CurrentGame.FreeBuild)
                SaveHelper.AutoSave(this);

            wantsToSave = false;
        }
    }

    [RunOnKeyDown("g_quick_save")]
    public void QuickSave()
    {
        if (!TransitionFinished)
        {
            GD.Print("quick save is disabled while transitioning");
            return;
        }

        GD.Print("quick saving microbe stage");
        SaveHelper.QuickSave(this);
    }

    /// <summary>
    ///   Switches to the editor
    /// </summary>
    public void MoveToEditor()
    {
        // Increase the population by the constant for the player reproducing
        var playerSpecies = GameWorld.PlayerSpecies;
        GameWorld.AlterSpeciesPopulation(
            playerSpecies, Constants.PLAYER_REPRODUCTION_POPULATION_GAIN_CONSTANT,
            TranslationServer.Translate("PLAYER_REPRODUCED"),
            false, Constants.PLAYER_REPRODUCTION_POPULATION_GAIN_COEFFICIENT);

        var scene = SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor);

        var editor = (MicrobeEditor)scene.Instance();

        editor.CurrentGame = CurrentGame;
        editor.ReturnToStage = this;

        // We don't free this here as the editor will return to this scene
        if (SceneManager.Instance.SwitchToScene(editor, true) != this)
        {
            throw new Exception("failed to keep the current scene root");
        }

        MovingToEditor = false;
    }

    /// <summary>
    ///   Called when returning from the editor
    /// </summary>
    public void OnReturnFromEditor()
    {
        UpdatePatchSettings(false);

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

        // Reset all the duplicates organelles of the player
        Player.ResetOrganelleLayout();

        // Spawn another cell from the player species
        Player.Divide();

        HUD.OnEnterStageTransition(false);
        HUD.HideReproductionDialog();

        StartMusic();

        // Apply language settings here to be sure the stage doesn't continue to use the wrong language
        // Because the stage scene tree being unattached during editor,
        // if language was changed while in the editor, it doesn't properly propagate
        Settings.Instance.ApplyLanguageSettings();

        // Auto save is wanted once possible
        wantsToSave = true;
    }

    public void OnFinishTransitioning()
    {
        TransitionFinished = true;
        TutorialState.SendEvent(TutorialEventType.EnteredMicrobeStage, EventArgs.Empty, this);
    }

    /// <summary>
    ///   Updates CompoundsAtMouse and MicrobesAtMouse
    /// </summary>
    private void UpdateMouseHover()
    {
        CompoundsAtMouse = Clouds.GetAllAvailableAt(Camera.CursorWorldPos);

        var microbes = GetTree().GetNodesInGroup(Constants.AI_TAG_MICROBE);

        foreach (var microbe in MicrobesAtMouse)
            microbe.IsHoveredOver = false;

        MicrobesAtMouse.Clear();

        foreach (Microbe entry in microbes)
        {
            var distance = (entry.GlobalTransform.origin - Camera.CursorWorldPos).Length();

            // Find only cells that have the mouse
            // position within their membrane
            if (distance > entry.Radius + Constants.MICROBE_HOVER_DETECTION_EXTRA_RADIUS)
                continue;

            entry.IsHoveredOver = true;
            MicrobesAtMouse.Add(entry);
        }
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerDied(Microbe player)
    {
        GD.Print("The player has died");

        HUD.HideReproductionDialog();

        TutorialState.SendEvent(TutorialEventType.MicrobePlayerDied, EventArgs.Empty, this);

        Player = null;
        Camera.ObjectToFollow = null;
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerReproductionStatusChanged(Microbe player, bool ready)
    {
        if (ready && player.Colony == null)
        {
            TutorialState.SendEvent(TutorialEventType.MicrobePlayerReadyToEdit, EventArgs.Empty, this);

            // This is to prevent the editor button being able to be clicked multiple times in freebuild mode
            if (!MovingToEditor)
                HUD.ShowReproductionDialog();
        }
        else
        {
            HUD.HideReproductionDialog();
        }
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerUnbindEnabled(Microbe player)
    {
        TutorialState.SendEvent(TutorialEventType.MicrobePlayerUnbindEnabled, EventArgs.Empty, this);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerUnbound(Microbe player)
    {
        TutorialState.SendEvent(TutorialEventType.MicrobePlayerUnbound, EventArgs.Empty, this);
    }

    private void CreatePatchManagerIfNeeded()
    {
        if (patchManager != null)
            return;

        patchManager = new PatchManager(spawner, ProcessSystem, Clouds, TimedLifeSystem,
            worldLight, CurrentGame);
    }

    /// <summary>
    ///   Handles respawning the player and checking for extinction
    /// </summary>
    private void HandlePlayerRespawn()
    {
        var playerSpecies = GameWorld.PlayerSpecies;

        // Decrease the population by the constant for the player dying
        GameWorld.AlterSpeciesPopulation(
            playerSpecies, Constants.PLAYER_DEATH_POPULATION_LOSS_CONSTANT,
            TranslationServer.Translate("PLAYER_DIED"),
            true, Constants.PLAYER_DEATH_POPULATION_LOSS_COEFFICIENT);

        HUD.HintText = string.Empty;

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

    private void UpdatePatchSettings(bool isLoading)
    {
        patchManager.ApplyChangedPatchSettingsIfNeeded(GameWorld.Map.CurrentPatch, !isLoading);

        HUD.UpdatePatchInfo(TranslationServer.Translate(GameWorld.Map.CurrentPatch.Name));
        HUD.UpdateEnvironmentalBars(GameWorld.Map.CurrentPatch.Biome);

        UpdateBackground();
    }

    private void UpdateBackground()
    {
        Camera.SetBackground(SimulationParameters.Instance.GetBackground(
            GameWorld.Map.CurrentPatch.BiomeTemplate.Background));
    }

    private void SaveGame(string name)
    {
        SaveHelper.Save(name, this);
    }
}
