using System;
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
public class MicrobeStage : NodeWithInput, IReturnableGameState, IGodotEarlyNodeResolve
{
    [Export]
    public NodePath GuidanceLinePath = null!;

    [Export]
    public NodePath PauseMenuPath = null!;

    [Export]
    public NodePath HUDRootPath = null!;

    private Compound glucose = null!;

    private Node world = null!;
    private Node rootOfDynamicallySpawned = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private SpawnSystem spawner = null!;

    private MicrobeAISystem microbeAISystem = null!;
    private MicrobeSystem microbeSystem = null!;

    private FloatingChunkSystem floatingChunkSystem = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private PatchManager patchManager = null!;

    private DirectionalLight worldLight = null!;

    private MicrobeTutorialGUI tutorialGUI = null!;
    private GuidanceLine guidanceLine = null!;
    private Vector3? guidancePosition;
    private PauseMenu pauseMenu = null!;
    private bool transitionFinished;

    private Control hudRoot = null!;

    private List<GuidanceLine> chemoreceptionLines = new();

    // TODO: make this be saved (and preserve old save compatibility by creating this in on save loaded callback
    // if null)
    private Random random = new();

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
    public CompoundCloudSystem Clouds { get; private set; } = null!;

    [JsonIgnore]
    public FluidSystem FluidSystem { get; private set; } = null!;

    [JsonIgnore]
    public TimedLifeSystem TimedLifeSystem { get; private set; } = null!;

    [JsonIgnore]
    public ProcessSystem ProcessSystem { get; private set; } = null!;

    /// <summary>
    ///   The main camera, needs to be after anything with AssignOnlyChildItemsOnDeserialize due to load order
    /// </summary>
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public MicrobeCamera Camera { get; private set; } = null!;

    [JsonIgnore]
    public MicrobeHUD HUD { get; private set; } = null!;

    /// <summary>
    ///   The current player or null. Due to references on save load this needs to be after the systems
    /// </summary>
    [JsonProperty]
    public Microbe? Player { get; private set; }

    [JsonIgnore]
    public PlayerHoverInfo HoverInfo { get; private set; } = null!;

    /// <summary>
    ///   The main current game object holding various details
    /// </summary>
    [JsonProperty]
    public GameProperties? CurrentGame { get; set; }

    [JsonIgnore]
    public GameWorld GameWorld => CurrentGame?.GameWorld ?? throw new InvalidOperationException("Game not started yet");

    [JsonIgnore]
    public TutorialState TutorialState =>
        CurrentGame?.TutorialState ?? throw new InvalidOperationException("Game not started yet");

    public Node GameStateRoot => this;

    public bool IsLoadedFromSave { get; set; }

    /// <summary>
    ///   True once stage fade-in is complete
    /// </summary>
    [JsonIgnore]
    public bool TransitionFinished
    {
        get => transitionFinished;
        internal set
        {
            transitionFinished = value;
            pauseMenu.GameLoading = !transitionFinished;
        }
    }

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

        glucose = SimulationParameters.Instance.GetCompound("glucose");

        tutorialGUI.Visible = true;
        HUD.Init(this);
        HoverInfo.Init(Camera, Clouds);

        // Do stage setup to spawn things and setup all parts of the stage
        SetupStage();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        CheatManager.OnSpawnEnemyCheatUsed += OnSpawnEnemyCheatUsed;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        CheatManager.OnSpawnEnemyCheatUsed -= OnSpawnEnemyCheatUsed;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            if (CurrentGame?.GameWorld.Map.CurrentPatch == null)
                throw new InvalidOperationException("Stage not initialized properly");

            HUD.UpdatePatchInfo(CurrentGame.GameWorld.Map.CurrentPatch.Name.ToString());
        }
    }

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        world = GetNode<Node>("World");
        HUD = GetNode<MicrobeHUD>("MicrobeHUD");
        tutorialGUI = GetNode<MicrobeTutorialGUI>("TutorialGUI");
        HoverInfo = GetNode<PlayerHoverInfo>("PlayerHoverInfo");
        rootOfDynamicallySpawned = GetNode<Node>("World/DynamicallySpawned");
        Camera = world.GetNode<MicrobeCamera>("PrimaryCamera");
        Clouds = world.GetNode<CompoundCloudSystem>("CompoundClouds");
        worldLight = world.GetNode<DirectionalLight>("WorldLight");
        guidanceLine = GetNode<GuidanceLine>(GuidanceLinePath);
        pauseMenu = GetNode<PauseMenu>(PauseMenuPath);
        hudRoot = GetNode<Control>(HUDRootPath);

        // These need to be created here as well for child property save load to work
        TimedLifeSystem = new TimedLifeSystem(rootOfDynamicallySpawned);
        ProcessSystem = new ProcessSystem(rootOfDynamicallySpawned);
        microbeAISystem = new MicrobeAISystem(rootOfDynamicallySpawned, Clouds);
        microbeSystem = new MicrobeSystem(rootOfDynamicallySpawned);
        floatingChunkSystem = new FloatingChunkSystem(rootOfDynamicallySpawned, Clouds);
        FluidSystem = new FluidSystem(rootOfDynamicallySpawned);
        spawner = new SpawnSystem(rootOfDynamicallySpawned);
        patchManager = new PatchManager(spawner, ProcessSystem, Clouds, TimedLifeSystem,
            worldLight, CurrentGame);

        NodeReferencesResolved = true;
    }

    // Prepares the stage for playing
    // Also begins a new game if one hasn't been started yet for easier debugging
    public void SetupStage()
    {
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

        patchManager.CurrentGame = CurrentGame;

        pauseMenu.SetNewSaveNameFromSpeciesName();

        StartMusic();

        if (IsLoadedFromSave)
        {
            HUD.OnEnterStageTransition(false);
            UpdatePatchSettings();
        }
        else
        {
            HUD.OnEnterStageTransition(true);
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

        patchManager.CurrentGame = CurrentGame;

        UpdatePatchSettings(!TutorialState.Enabled);

        SpawnPlayer();
    }

    public void StartMusic()
    {
        if (GameWorld.PlayerSpecies is EarlyMulticellularSpecies)
        {
            Jukebox.Instance.PlayCategory("EarlyMulticellularStage");
        }
        else
        {
            Jukebox.Instance.PlayCategory("MicrobeStage");
        }
    }

    /// <summary>
    ///   Spawns the player if there isn't currently a player node existing
    /// </summary>
    public void SpawnPlayer()
    {
        if (Player != null)
            return;

        Player = SpawnHelpers.SpawnMicrobe(GameWorld.PlayerSpecies, new Vector3(0, 0, 0),
            rootOfDynamicallySpawned, SpawnHelpers.LoadMicrobeScene(), false, Clouds, CurrentGame!);
        Player.AddToGroup("player");

        Player.OnDeath = OnPlayerDied;

        Player.OnIngested = OnPlayerEngulfed;

        Player.OnReproductionStatus = OnPlayerReproductionStatusChanged;

        Player.OnUnbound = OnPlayerUnbound;

        Player.OnUnbindEnabled = OnPlayerUnbindEnabled;

        Player.OnCompoundChemoreceptionInfo = HandlePlayerChemoreceptionDetection;

        Player.OnEngulfmentStorageFull = OnPlayerEngulfmentLimitReached;

        Camera.ObjectToFollow = Player;

        if (spawnedPlayer)
        {
            // Random location on respawn
            Player.Translation = new Vector3(
                random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE), 0,
                random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE));
        }

        TutorialState.SendEvent(TutorialEventType.MicrobePlayerSpawned, new MicrobeEventArgs(Player), this);

        spawnedPlayer = true;
        playerRespawnTimer = Constants.PLAYER_RESPAWN_TIME;

        ModLoader.ModInterface.TriggerOnPlayerMicrobeSpawned(Player);
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
        floatingChunkSystem.Process(delta, Player?.Translation);
        microbeAISystem.Process(delta);
        microbeSystem.Process(delta);

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
                        new CompoundPositionEventArgs(Clouds.FindCompoundNearPoint(Player.GlobalTransform.origin,
                            glucose)),
                        this);
                }

                guidancePosition = TutorialState.GetPlayerGuidancePosition();
            }

            if (guidancePosition != null)
            {
                guidanceLine.Visible = true;
                guidanceLine.LineStart = Player.GlobalTransform.origin;
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

        UpdateLinePlayerPosition();

        // Start auto-evo if stage entry finished, don't need to auto save,
        // settings have auto-evo be started during gameplay and auto-evo is not already started
        if (TransitionFinished && !wantsToSave && Settings.Instance.RunAutoEvoDuringGamePlay)
        {
            GameWorld.IsAutoEvoFinished(true);
        }

        // Save if wanted
        if (TransitionFinished && wantsToSave)
        {
            if (CurrentGame == null)
            {
                throw new InvalidOperationException(
                    "Stage doesn't have a game state even though it should be initialized");
            }

            if (!CurrentGame.FreeBuild)
                SaveHelper.AutoSave(this);

            wantsToSave = false;
        }

        var metrics = PerformanceMetrics.Instance;

        if (metrics.Visible)
        {
            var entities = rootOfDynamicallySpawned.GetChildrenToProcess<ISpawned>(Constants.SPAWNED_GROUP).Count();
            var childCount = rootOfDynamicallySpawned.GetChildCount();
            metrics.ReportEntities(entities, childCount - entities);
        }
    }

    [RunOnKeyDown("g_quick_save")]
    public void QuickSave()
    {
        if (!TransitionFinished || wantsToSave)
        {
            GD.Print("Skipping quick save as stage transition is not finished or saving is queued");
            return;
        }

        GD.Print("quick saving microbe stage");
        SaveHelper.QuickSave(this);
    }

    [RunOnKeyDown("g_toggle_gui")]
    public void ToggleGUI()
    {
        hudRoot.Visible = !hudRoot.Visible;
    }

    /// <summary>
    ///   Switches to the editor
    /// </summary>
    public void MoveToEditor()
    {
        if (Player?.Dead != false)
        {
            GD.PrintErr("Player object disappeared or died while transitioning to the editor");
            return;
        }

        if (CurrentGame == null)
            throw new InvalidOperationException("Stage has no current game");

        Node sceneInstance;

        if (Player.IsMulticellular)
        {
            // Player is a multicellular species, go to multicellular editor

            var scene = SceneManager.Instance.LoadScene(MainGameState.EarlyMulticellularEditor);

            sceneInstance = scene.Instance();
            var editor = (EarlyMulticellularEditor)sceneInstance;

            editor.CurrentGame = CurrentGame;
            editor.ReturnToStage = this;
        }
        else
        {
            // Might be related to saving but somehow the editor button can be enabled while in a colony
            // TODO: for now to prevent crashing, we just ignore that here, but this should be fixed by the button
            // becoming disabled properly
            // https://github.com/Revolutionary-Games/Thrive/issues/2504
            if (Player.Colony != null)
            {
                GD.PrintErr("Editor button was enabled and pressed while the player is in a colony");
                return;
            }

            var scene = SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor);

            sceneInstance = scene.Instance();
            var editor = (MicrobeEditor)sceneInstance;

            editor.CurrentGame = CurrentGame;
            editor.ReturnToStage = this;
        }

        GiveReproductionPopulationBonus();

        // We don't free this here as the editor will return to this scene
        if (SceneManager.Instance.SwitchToScene(sceneInstance, true) != this)
        {
            throw new Exception("failed to keep the current scene root");
        }

        MovingToEditor = false;
    }

    /// <summary>
    ///   Moves to the multicellular editor (the first time)
    /// </summary>
    public void MoveToMulticellular()
    {
        if (Player?.Dead != false || Player.Colony == null)
        {
            GD.PrintErr("Player object disappeared or died (or not in a colony) while trying to become multicellular");
            return;
        }

        GD.Print("Disbanding colony and becoming multicellular");

        // Move to multicellular always happens when the player is in a colony, so we force disband that here before
        // proceeding
        Player.UnbindAll();

        GiveReproductionPopulationBonus();

        CurrentGame!.EnterPrototypes();

        var playerSpeciesMicrobes = GetAllPlayerSpeciesMicrobes();

        // Re-apply species here so that the player cell knows it is multicellular after this
        // Also apply species here to other members of the player's previous species
        // This prevents previous members of the player's colony from immediately being hostile
        bool playerHandled = false;

        var multicellularSpecies = GameWorld.ChangeSpeciesToMulticellular(Player.Species);
        foreach (var microbe in playerSpeciesMicrobes)
        {
            microbe.ApplySpecies(multicellularSpecies);

            if (microbe == Player)
                playerHandled = true;
        }

        if (!playerHandled)
            throw new Exception("Did not find player to apply multicellular species to");

        GameWorld.NotifySpeciesChangedStages();

        var scene = SceneManager.Instance.LoadScene(MainGameState.EarlyMulticellularEditor);

        var editor = (EarlyMulticellularEditor)scene.Instance();

        editor.CurrentGame = CurrentGame ?? throw new InvalidOperationException("Stage has no current game");
        editor.ReturnToStage = this;

        GD.Print("Switching to multicellular editor");

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
        if (CurrentGame == null)
            throw new InvalidOperationException("Returning to stage from editor without a game setup");

        UpdatePatchSettings();

        // Now the editor increases the generation so we don't do that here anymore

        // Make sure player is spawned
        SpawnPlayer();

        // Check win conditions
        if (!CurrentGame.FreeBuild && Player!.Species.Generation >= 20 &&
            Player.Species.Population >= 300 && !wonOnce)
        {
            HUD.ToggleWinBox();
            wonOnce = true;
        }

        // Spawn another cell from the player species
        // This is done first to ensure that the player colony is still intact for spawn separation calculation
        var daughter = Player!.Divide();

        // If multicellular, we want that other cell colony to be fully grown to show budding in action
        if (Player.IsMulticellular)
        {
            daughter.BecomeFullyGrownMulticellularColony();

            // TODO: add more extra offset between the player and the divided cell
        }

        // Update the player's cell
        Player.ApplySpecies(Player.Species);

        // Reset all the duplicates organelles of the player
        Player.ResetOrganelleLayout();

        HUD.OnEnterStageTransition(false);
        HUD.HideReproductionDialog();

        if (!CurrentGame.TutorialState.Enabled)
        {
            tutorialGUI.EventReceiver?.OnTutorialDisabled();
        }

        StartMusic();

        // Reset locale to assure the stage's language.
        // Because the stage scene tree being unattached during editor, if language was
        // changed while in the editor, it doesn't update this stage's translation cache.
        TranslationServer.SetLocale(TranslationServer.GetLocale());

        // Auto save is wanted once possible (unless we are in prototypes)
        if (!CurrentGame.InPrototypes)
            wantsToSave = true;

        pauseMenu.SetNewSaveNameFromSpeciesName();
    }

    public void OnFinishTransitioning()
    {
        TransitionFinished = true;
        TutorialState.SendEvent(
            TutorialEventType.EnteredMicrobeStage, new CallbackEventArgs(HUD.PopupPatchInfo), this);
    }

    /// <summary>
    ///   Helper function for transition to multicellular
    /// </summary>
    /// <returns>Array of all microbes of Player's species</returns>
    private IEnumerable<Microbe> GetAllPlayerSpeciesMicrobes()
    {
        if (Player == null)
            throw new InvalidOperationException("Could not get player species microbes: no Player object");

        var microbes = rootOfDynamicallySpawned.GetTree().GetNodesInGroup(Constants.AI_TAG_MICROBE).Cast<Microbe>();

        return microbes.Where(m => m.Species == Player.Species);
    }

    /// <summary>
    ///   Increases the population by the constant for the player reproducing
    /// </summary>
    private void GiveReproductionPopulationBonus()
    {
        var playerSpecies = GameWorld.PlayerSpecies;
        GameWorld.AlterSpeciesPopulation(
            playerSpecies, Constants.PLAYER_REPRODUCTION_POPULATION_GAIN_CONSTANT,
            TranslationServer.Translate("PLAYER_REPRODUCED"),
            false, Constants.PLAYER_REPRODUCTION_POPULATION_GAIN_COEFFICIENT);
    }

    private void OnSpawnEnemyCheatUsed(object sender, EventArgs e)
    {
        if (Player == null)
            return;

        var species = GameWorld.Map.CurrentPatch!.SpeciesInPatch.Keys.Where(s => !s.PlayerSpecies).ToList();

        // No enemy species to spawn in this patch
        if (species.Count == 0)
        {
            ToolTipManager.Instance.ShowPopup(TranslationServer.Translate("SPAWN_ENEMY_CHEAT_FAIL"), 2.0f);
            GD.PrintErr("Can't use spawn enemy cheat because this patch does not contain any enemy species");
            return;
        }

        var randomSpecies = species.Random(random);

        var copyEntity = SpawnHelpers.SpawnMicrobe(randomSpecies, Player.Translation + Vector3.Forward * 20,
            rootOfDynamicallySpawned, SpawnHelpers.LoadMicrobeScene(), true, Clouds,
            CurrentGame!);

        // Make the cell despawn like normal
        SpawnSystem.AddEntityToTrack(copyEntity);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerDied(Microbe player)
    {
        GD.Print("The player has died");

        // Decrease the population by the constant for the player dying
        GameWorld.AlterSpeciesPopulation(
            GameWorld.PlayerSpecies, Constants.PLAYER_DEATH_POPULATION_LOSS_CONSTANT,
            TranslationServer.Translate("PLAYER_DIED"),
            true, Constants.PLAYER_DEATH_POPULATION_LOSS_COEFFICIENT);

        if (IsGameOver())
        {
            Jukebox.Instance.PlayCategory("Extinction");
        }

        HUD.HideReproductionDialog();

        TutorialState.SendEvent(TutorialEventType.MicrobePlayerDied, EventArgs.Empty, this);

        Player = null;
        Camera.ObjectToFollow = null;
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerEngulfed(Microbe player, Microbe engulfer)
    {
        // Counted as normal death
        OnPlayerDied(player);

        // To avoid camera position being reset to world origin
        Camera.ObjectToFollow = engulfer;
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerReproductionStatusChanged(Microbe player, bool ready)
    {
        if (ready && (player.Colony == null || player.IsMulticellular))
        {
            if (!player.IsMulticellular)
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

    [DeserializedCallbackAllowed]
    private void OnPlayerEngulfmentLimitReached(Microbe player)
    {
        TutorialState.SendEvent(TutorialEventType.MicrobePlayerEngulfmentFull, EventArgs.Empty, this);
    }

    /// <summary>
    ///   Handles respawning the player and checking for extinction
    /// </summary>
    private void HandlePlayerRespawn()
    {
        HUD.HintText = string.Empty;

        // Respawn if not extinct (or freebuild)
        if (IsGameOver())
        {
            gameOver = true;
        }
        else
        {
            // Player is not extinct, so can respawn
            SpawnPlayer();
        }
    }

    private void UpdatePatchSettings(bool promptPatchNameChange = true)
    {
        // TODO: would be nice to skip this if we are loading a save made in the editor as this gets called twice when
        // going back to the stage
        if (patchManager.ApplyChangedPatchSettingsIfNeeded(GameWorld.Map.CurrentPatch!) && promptPatchNameChange)
        {
            HUD.PopupPatchInfo();
        }

        HUD.UpdatePatchInfo(GameWorld.Map.CurrentPatch!.Name.ToString());
        HUD.UpdateEnvironmentalBars(GameWorld.Map.CurrentPatch.Biome);

        UpdateBackground();
    }

    private void UpdateBackground()
    {
        Camera.SetBackground(SimulationParameters.Instance.GetBackground(
            GameWorld.Map.CurrentPatch!.BiomeTemplate.Background));
    }

    private void SaveGame(string name)
    {
        SaveHelper.Save(name, this);
    }

    /// <summary>
    ///   Updates the chemoreception lines for stuff the player wants to detect
    /// </summary>
    [DeserializedCallbackAllowed]
    private void HandlePlayerChemoreceptionDetection(Microbe microbe,
        IEnumerable<(Compound Compound, float Range, float MinAmount, Color Colour)> activeCompoundDetections)
    {
        if (microbe != Player)
            GD.PrintErr("Chemoreception data reported for non-player cell");

        int currentLineIndex = 0;
        var position = microbe.GlobalTransform.origin;

        foreach (var tuple in microbe.GetDetectedCompounds(Clouds))
        {
            var line = GetOrCreateGuidanceLine(currentLineIndex++);

            line.Colour = tuple.Colour;
            line.LineStart = position;
            line.LineEnd = tuple.Target;
            line.Visible = true;
        }

        // Remove excess lines
        while (currentLineIndex < chemoreceptionLines.Count)
        {
            var line = chemoreceptionLines[chemoreceptionLines.Count - 1];
            chemoreceptionLines.RemoveAt(chemoreceptionLines.Count - 1);

            RemoveChild(line);
            line.QueueFree();
        }
    }

    private void UpdateLinePlayerPosition()
    {
        if (Player == null || Player?.Dead == true)
        {
            foreach (var chemoreceptionLine in chemoreceptionLines)
                chemoreceptionLine.Visible = false;

            return;
        }

        var position = Player!.GlobalTransform.origin;

        foreach (var chemoreceptionLine in chemoreceptionLines)
        {
            if (chemoreceptionLine.Visible)
                chemoreceptionLine.LineStart = position;
        }
    }

    private GuidanceLine GetOrCreateGuidanceLine(int index)
    {
        if (index >= chemoreceptionLines.Count)
        {
            // The lines are created here and added as children of the stage because if they were in the microbe
            // then rotation and it moving cause implementation difficulties
            var line = new GuidanceLine();
            AddChild(line);
            chemoreceptionLines.Add(line);
        }

        return chemoreceptionLines[index];
    }

    private bool IsGameOver()
    {
        return GameWorld.PlayerSpecies.Population <= 0 && !CurrentGame!.FreeBuild;
    }
}
