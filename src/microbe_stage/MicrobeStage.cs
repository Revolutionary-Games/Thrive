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
[UseThriveSerializer]
public class MicrobeStage : CreatureStageBase<Microbe>
{
    [Export]
    public NodePath? GuidanceLinePath;

    private Compound glucose = null!;
    private Compound phosphate = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private SpawnSystem spawner = null!;

    private MicrobeAISystem microbeAISystem = null!;
    private MicrobeSystem microbeSystem = null!;

    private FloatingChunkSystem floatingChunkSystem = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private PatchManager patchManager = null!;

#pragma warning disable CA2213
    private MicrobeTutorialGUI tutorialGUI = null!;
    private GuidanceLine guidanceLine = null!;
#pragma warning restore CA2213
    private Vector3? guidancePosition;

    /// <summary>
    ///   Used to track chemoreception lines. If TargetMicrobe is null the target is static.
    /// </summary>
    private List<(GuidanceLine Line, Microbe? TargetMicrobe)> chemoreceptionLines = new();

    /// <summary>
    ///   Used to control how often compound position info is sent to the tutorial
    /// </summary>
    [JsonProperty]
    private float elapsedSinceEntityPositionCheck;

    [JsonProperty]
    private bool wonOnce;

    private float maxLightLevel;

    private float templateMaxLightLevel;

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

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public MicrobeHUD HUD { get; private set; } = null!;

    [JsonIgnore]
    public MicrobeInspectInfo HoverInfo { get; private set; } = null!;

    [JsonIgnore]
    public TutorialState TutorialState =>
        CurrentGame?.TutorialState ?? throw new InvalidOperationException("Game not started yet");

    protected override ICreatureStageHUD BaseHUD => HUD;

    private LocalizedString CurrentPatchName =>
        GameWorld.Map.CurrentPatch?.Name ?? throw new InvalidOperationException("no current patch");

    /// <summary>
    ///   This gets called the first time the stage scene is put into an active scene tree.
    ///   So returning from the editor doesn't cause this to re-run.
    /// </summary>
    public override void _Ready()
    {
        base._Ready();

        // Start a new game if started directly from MicrobeStage.tscn
        CurrentGame ??= GameProperties.StartNewMicrobeGame(new WorldGenerationSettings());

        ResolveNodeReferences();

        glucose = SimulationParameters.Instance.GetCompound("glucose");
        phosphate = SimulationParameters.Instance.GetCompound("phosphates");

        tutorialGUI.Visible = true;
        HUD.Init(this);
        HoverInfo.Init(Clouds, Camera);

        // Do stage setup to spawn things and setup all parts of the stage
        SetupStage();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        HUD = GetNode<MicrobeHUD>("MicrobeHUD");
        tutorialGUI = GetNode<MicrobeTutorialGUI>("TutorialGUI");
        HoverInfo = GetNode<MicrobeInspectInfo>("PlayerHoverInfo");
        Camera = world.GetNode<MicrobeCamera>("PrimaryCamera");
        Clouds = world.GetNode<CompoundCloudSystem>("CompoundClouds");
        guidanceLine = GetNode<GuidanceLine>(GuidanceLinePath);

        // These need to be created here as well for child property save load to work
        TimedLifeSystem = new TimedLifeSystem(rootOfDynamicallySpawned);
        ProcessSystem = new ProcessSystem(rootOfDynamicallySpawned);
        microbeAISystem = new MicrobeAISystem(rootOfDynamicallySpawned, Clouds);
        microbeSystem = new MicrobeSystem(rootOfDynamicallySpawned);
        floatingChunkSystem = new FloatingChunkSystem(rootOfDynamicallySpawned, Clouds);
        FluidSystem = new FluidSystem(rootOfDynamicallySpawned);
        spawner = new SpawnSystem(rootOfDynamicallySpawned);
        patchManager = new PatchManager(spawner, ProcessSystem, Clouds, TimedLifeSystem, worldLight, CurrentGame);
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        CheatManager.OnSpawnEnemyCheatUsed += OnSpawnEnemyCheatUsed;
        CheatManager.OnDespawnAllEntitiesCheatUsed += OnDespawnAllEntitiesCheatUsed;
        CheatManager.OnSetTimeCheatUsed += OnSetTimeCheatUsed;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        CheatManager.OnSpawnEnemyCheatUsed -= OnSpawnEnemyCheatUsed;
        CheatManager.OnDespawnAllEntitiesCheatUsed -= OnDespawnAllEntitiesCheatUsed;
        CheatManager.OnSetTimeCheatUsed -= OnSetTimeCheatUsed;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        FluidSystem.Process(delta);
        TimedLifeSystem.Process(delta);
        ProcessSystem.Process(delta);
        floatingChunkSystem.Process(delta, Player?.Translation);
        microbeAISystem.Process(delta);
        microbeSystem.Process(delta);

        if (gameOver)
            return;

        if (playerExtinctInCurrentPatch)
            return;

        if (Player != null)
        {
            var playerTransform = Player.GlobalTransform;

            DebugOverlays.Instance.ReportPositionCoordinates(playerTransform.origin);
            DebugOverlays.Instance.ReportLookingAtCoordinates(Camera.CursorWorldPos);

            spawner.Process(delta, playerTransform.origin);
            Clouds.ReportPlayerPosition(playerTransform.origin);

            TutorialState.SendEvent(TutorialEventType.MicrobePlayerOrientation,
                new RotationEventArgs(playerTransform.basis, Player.RotationDegrees), this);

            TutorialState.SendEvent(TutorialEventType.MicrobePlayerCompounds,
                new CompoundBagEventArgs(Player.Compounds), this);

            TutorialState.SendEvent(TutorialEventType.MicrobePlayerTotalCollected,
                new CompoundEventArgs(Player.TotalAbsorbedCompounds), this);

            // TODO: if we start getting a ton of tutorial stuff reported each frame we should only report stuff when
            // relevant, for example only when in a colony or just leaving a colony should the player colony
            // info be sent
            TutorialState.SendEvent(TutorialEventType.MicrobePlayerColony,
                new MicrobeColonyEventArgs(Player.Colony), this);

            elapsedSinceEntityPositionCheck += delta;

            if (elapsedSinceEntityPositionCheck > Constants.TUTORIAL_ENTITY_POSITION_UPDATE_INTERVAL)
            {
                elapsedSinceEntityPositionCheck = 0;

                if (TutorialState.WantsNearbyCompoundInfo())
                {
                    TutorialState.SendEvent(TutorialEventType.MicrobeCompoundsNearPlayer,
                        new EntityPositionEventArgs(Clouds.FindCompoundNearPoint(playerTransform.origin,
                            glucose)),
                        this);
                }

                if (TutorialState.WantsNearbyEngulfableInfo())
                {
                    var entities = rootOfDynamicallySpawned.GetChildrenToProcess<ISpawned>(Constants.SPAWNED_GROUP);
                    var engulfables = entities.OfType<IEngulfable>().ToList();

                    TutorialState.SendEvent(TutorialEventType.MicrobeChunksNearPlayer,
                        new EntityPositionEventArgs(Player.FindNearestEngulfable(engulfables)), this);
                }

                guidancePosition = TutorialState.GetPlayerGuidancePosition();
            }

            if (guidancePosition != null)
            {
                guidanceLine.Visible = true;
                guidanceLine.LineStart = playerTransform.origin;
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
        }

        UpdateLinePlayerPosition();
    }

    public override void _PhysicsProcess(float delta)
    {
        FluidSystem.PhysicsProcess(delta);
    }

    public override void OnFinishTransitioning()
    {
        base.OnFinishTransitioning();

        if (GameWorld.PlayerSpecies is not EarlyMulticellularSpecies)
        {
            TutorialState.SendEvent(
                TutorialEventType.EnteredMicrobeStage,
                new CallbackEventArgs(() => HUD.ShowPatchName(CurrentPatchName.ToString())), this);
        }
        else
        {
            TutorialState.SendEvent(TutorialEventType.EnteredEarlyMulticellularStage, EventArgs.Empty, this);
        }
    }

    public override void OnFinishLoading(Save save)
    {
        OnFinishLoading();
    }

    public override void StartNewGame()
    {
        CurrentGame = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings());

        UpdatePatchSettings(!TutorialState.Enabled);

        base.StartNewGame();
    }

    public override void StartMusic()
    {
        Jukebox.Instance.PlayCategory(GameWorld.PlayerSpecies is EarlyMulticellularSpecies ?
            "EarlyMulticellularStage" :
            "MicrobeStage");
    }

    [RunOnKeyDown("g_pause")]
    public void PauseKeyPressed()
    {
        // Check nothing else has keyboard focus and pause the game
        if (HUD.GetFocusOwner() == null)
        {
            HUD.PauseButtonPressed(!HUD.Paused);
        }
    }

    /// <summary>
    ///   Switches to the editor
    /// </summary>
    public override void MoveToEditor()
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

            // TODO: severely limit the MP points in awakening stage
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

        var previousSpecies = Player.Species;
        previousSpecies.Obsolete = true;

        var multicellularSpecies = GameWorld.ChangeSpeciesToMulticellular(previousSpecies);
        foreach (var microbe in playerSpeciesMicrobes)
        {
            microbe.ApplySpecies(multicellularSpecies);

            if (microbe == Player)
                playerHandled = true;

            if (microbe.Species != multicellularSpecies)
                throw new Exception("Failed to apply multicellular species");
        }

        if (!playerHandled)
            throw new Exception("Did not find player to apply multicellular species to");

        GameWorld.NotifySpeciesChangedStages();

        // Make sure no queued player species can spawn with the old species
        spawner.ClearSpawnQueue();

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
    ///   Moves to the late multicellular (macroscopic) editor (the first time)
    /// </summary>
    public void MoveToMacroscopic()
    {
        if (Player?.Dead != false || Player.Colony == null)
        {
            GD.PrintErr("Player object disappeared or died (or not in a colony) while trying to become macroscopic");
            return;
        }

        GD.Print("Becoming late multicellular (macroscopic)");

        // We don't really need to handle the player state or anything like that here as once we go to the late
        // multicellular editor, we never return to the microbe stage. So we don't need that much setup as becoming
        // multicellular

        // Move to multicellular always happens when the player is in a colony, so we force disband that here before
        // proceeding
        Player.UnbindAll();

        GiveReproductionPopulationBonus();

        CurrentGame!.EnterPrototypes();

        var modifiedSpecies = GameWorld.ChangeSpeciesToLateMulticellular(Player.Species);

        // Similar code as in the MetaballBodyEditorComponent to prevent the player automatically getting stuck
        // underwater in the awakening stage
        if (modifiedSpecies.MulticellularType == MulticellularSpeciesType.Awakened)
        {
            GD.Print("Preventing player from becoming awakened too soon");
            modifiedSpecies.KeepPlayerInAwareStage();
        }

        GameWorld.NotifySpeciesChangedStages();

        var scene = SceneManager.Instance.LoadScene(MainGameState.LateMulticellularEditor);

        var editor = (LateMulticellularEditor)scene.Instance();

        editor.CurrentGame = CurrentGame ?? throw new InvalidOperationException("Stage has no current game");

        // We'll start off in a brand new stage in the late multicellular part
        editor.ReturnToStage = null;

        GD.Print("Switching to late multicellular editor");

        SceneManager.Instance.SwitchToScene(editor, false);

        MovingToEditor = false;
    }

    public override void OnReturnFromEditor()
    {
        UpdatePatchSettings();

        base.OnReturnFromEditor();

        // Add a cloud of glucose if difficulty settings call for it
        if (GameWorld.WorldSettings.FreeGlucoseCloud)
        {
            Clouds.AddCloud(glucose, 200000.0f, Player!.Translation + new Vector3(0.0f, 0.0f, -25.0f));
        }

        // Check win conditions
        if (!CurrentGame!.FreeBuild && Player!.Species.Generation >= 20 &&
            Player.Species.Population >= 300 && !wonOnce)
        {
            HUD.ToggleWinBox();
            wonOnce = true;
        }

        // Update the player's cell
        Player!.ApplySpecies(Player.Species);

        // Reset all the duplicates organelles of the player
        Player.ResetOrganelleLayout();

        var playerPosition = Player.GlobalTransform.origin;

        // Spawn another cell from the player species
        // This needs to be done after updating the player so that multicellular organisms are accurately separated
        var daughter = Player.Divide();

        daughter.AddToGroup(Constants.PLAYER_REPRODUCED_GROUP);

        // If multicellular, we want that other cell colony to be fully grown to show budding in action
        if (Player.IsMulticellular)
        {
            daughter.BecomeFullyGrownMulticellularColony();

            if (daughter.Colony != null)
            {
                // Add more extra offset between the player and the divided cell
                var daughterPosition = daughter.GlobalTransform.origin;
                var direction = (playerPosition - daughterPosition).Normalized();

                var colonyMembers = daughter.Colony.ColonyMembers.Select(c => c.GlobalTransform.origin);

                float distance = MathUtils.GetMaximumDistanceInDirection(direction, daughterPosition, colonyMembers);

                daughter.Translation += -direction * distance;
            }
        }

        // This is queued to run to reduce the massive lag spike that anyway happens on this frame
        // The dynamically spawned is used here as the object to detect if the entire stage is getting disposed this
        // frame and won't be available on the next one
        Invoke.Instance.QueueForObject(() => spawner.EnsureEntityLimitAfterPlayerReproduction(playerPosition, daughter),
            rootOfDynamicallySpawned);

        if (!CurrentGame.TutorialState.Enabled)
        {
            tutorialGUI.EventReceiver?.OnTutorialDisabled();
        }
        else
        {
            // Show day/night cycle tutorial when entering a patch with sunlight
            if (GameWorld.WorldSettings.DayNightCycleEnabled)
            {
                var sunlight = SimulationParameters.Instance.GetCompound("sunlight");
                var patchSunlight = GameWorld.Map.CurrentPatch!.GetCompoundAmount(sunlight, CompoundAmountType.Biome);

                if (patchSunlight > Constants.DAY_NIGHT_TUTORIAL_LIGHT_MIN)
                {
                    TutorialState.SendEvent(TutorialEventType.MicrobePlayerEnterSunlightPatch, EventArgs.Empty, this);
                }
            }
        }
    }

    public override void OnSuicide()
    {
        Player?.Damage(9999.0f, "suicide");
    }

    protected override void SetupStage()
    {
        // Initialise the cloud system first so we can apply patch-specific brightness in OnGameStarted
        Clouds.Init(FluidSystem);

        // Initialise spawners next, since this removes existing spawners if present
        if (!IsLoadedFromSave)
            spawner.Init();

        base.SetupStage();

        tutorialGUI.EventReceiver = TutorialState;
        HUD.SendEditorButtonToTutorial(TutorialState);

        // If this is a new game, place some phosphates as a learning tool
        if (!IsLoadedFromSave)
        {
            Clouds.AddCloud(phosphate, 50000.0f, new Vector3(50.0f, 0.0f, 0.0f));
        }

        patchManager.CurrentGame = CurrentGame;

        if (IsLoadedFromSave)
        {
            UpdatePatchSettings();
        }
    }

    protected override void OnGameStarted()
    {
        patchManager.CurrentGame = CurrentGame;

        UpdatePatchSettings(!TutorialState.Enabled);

        SpawnPlayer();
    }

    protected override void SpawnPlayer()
    {
        if (HasPlayer)
            return;

        Player = SpawnHelpers.SpawnMicrobe(GameWorld.PlayerSpecies, new Vector3(0, 0, 0),
            rootOfDynamicallySpawned, SpawnHelpers.LoadMicrobeScene(), false, Clouds, spawner, CurrentGame!);
        Player.AddToGroup(Constants.PLAYER_GROUP);

        Player.OnDeath = OnPlayerDied;

        Player.OnReproductionStatus = OnPlayerReproductionStatusChanged;

        Player.OnUnbound = OnPlayerUnbound;

        Player.OnUnbindEnabled = OnPlayerUnbindEnabled;

        Player.OnChemoreceptionInfo = HandlePlayerChemoreception;

        Player.OnIngestedByHostile = OnPlayerEngulfedByHostile;

        Player.OnSuccessfulEngulfment = OnPlayerIngesting;

        Player.OnEngulfmentStorageFull = OnPlayerEngulfmentLimitReached;

        Player.OnNoticeMessage = OnPlayerNoticeMessage;

        Camera.ObjectToFollow = Player;

        spawner.DespawnAll();

        if (spawnedPlayer)
        {
            // Random location on respawn
            Player.Translation = new Vector3(
                random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE), 0,
                random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE));

            spawner.ClearSpawnCoordinates();
        }

        TutorialState.SendEvent(TutorialEventType.MicrobePlayerSpawned, new MicrobeEventArgs(Player), this);

        spawnedPlayer = true;
        playerRespawnTimer = Constants.PLAYER_RESPAWN_TIME;

        ModLoader.ModInterface.TriggerOnPlayerMicrobeSpawned(Player);
    }

    protected override void OnCanEditStatusChanged(bool canEdit)
    {
        base.OnCanEditStatusChanged(canEdit);

        if (!canEdit)
            return;

        if (Player is { IsMulticellular: false })
            TutorialState.SendEvent(TutorialEventType.MicrobePlayerReadyToEdit, EventArgs.Empty, this);
    }

    protected override void OnGameOver()
    {
        base.OnGameOver();

        guidanceLine.Visible = false;
    }

    protected override void PlayerExtinctInPatch()
    {
        base.PlayerExtinctInPatch();

        guidanceLine.Visible = false;
    }

    protected override void AutoSave()
    {
        SaveHelper.AutoSave(this);
    }

    protected override void PerformQuickSave()
    {
        SaveHelper.QuickSave(this);
    }

    protected override void UpdatePatchSettings(bool promptPatchNameChange = true)
    {
        // TODO: would be nice to skip this if we are loading a save made in the editor as this gets called twice when
        // going back to the stage
        if (patchManager.ApplyChangedPatchSettingsIfNeeded(GameWorld.Map.CurrentPatch!))
        {
            if (promptPatchNameChange)
                HUD.ShowPatchName(CurrentPatchName.ToString());

            Player?.ClearEngulfedObjects();
        }

        HUD.UpdateEnvironmentalBars(GameWorld.Map.CurrentPatch!.Biome);

        UpdateBackground();

        UpdatePatchLightLevelSettings();
    }

    protected override void OnLightLevelUpdate()
    {
        if (GameWorld.Map.CurrentPatch == null)
            return;

        // TODO: it would make more sense for the GameWorld to update its patch map data based on the
        // light cycle in it.
        patchManager.UpdatePatchBiome(GameWorld.Map.CurrentPatch);
        GameWorld.UpdateGlobalLightLevels();

        HUD.UpdateEnvironmentalBars(GameWorld.Map.CurrentPatch.Biome);

        // Updates the background lighting and does various post-effects
        if (templateMaxLightLevel > 0.0f && maxLightLevel > 0.0f)
        {
            // This might need to be refactored for efficiency but, it works for now
            var lightLevel = GameWorld.Map.CurrentPatch!.GetCompoundAmount("sunlight") *
                GameWorld.LightCycle.DayLightFraction;

            // Normalise by maximum light level in the patch
            Camera.LightLevel = lightLevel / maxLightLevel;
        }
        else
        {
            // Don't change lighting for patches without day/night effects
            Camera.LightLevel = 1.0f;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GuidanceLinePath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateBackground()
    {
        Camera.SetBackground(SimulationParameters.Instance.GetBackground(
            GameWorld.Map.CurrentPatch!.BiomeTemplate.Background));
    }

    private void UpdatePatchLightLevelSettings()
    {
        if (GameWorld.Map.CurrentPatch == null)
            throw new InvalidOperationException("Unknown current patch");

        maxLightLevel = GameWorld.Map.CurrentPatch.GetCompoundAmount("sunlight", CompoundAmountType.Maximum);
        templateMaxLightLevel = GameWorld.Map.CurrentPatch.GetCompoundAmount("sunlight", CompoundAmountType.Template);
    }

    private void SaveGame(string name)
    {
        SaveHelper.Save(name, this);
    }

    private void OnFinishLoading()
    {
        Camera.ObjectToFollow = Player;
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
            rootOfDynamicallySpawned, SpawnHelpers.LoadMicrobeScene(), true, Clouds, spawner,
            CurrentGame!);

        // Make the cell despawn like normal
        spawner.AddEntityToTrack(copyEntity);
    }

    private void OnDespawnAllEntitiesCheatUsed(object? sender, EventArgs args)
    {
        spawner.DespawnAll();
    }

    private void OnSetTimeCheatUsed(object? sender, float timeFraction)
    {
        GameWorld.LightCycle.FractionOfDayElapsed = timeFraction;
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerDied(Microbe player)
    {
        HandlePlayerDeath();

        if (player.PhagocytosisStep == PhagocytosisPhase.None)
            TutorialState.SendEvent(TutorialEventType.MicrobePlayerDied, EventArgs.Empty, this);

        Player = null;
        Camera.ObjectToFollow = null;
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerReproductionStatusChanged(Microbe player, bool ready)
    {
        OnCanEditStatusChanged(ready && (player.Colony == null || player.IsMulticellular));
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
    private void OnPlayerIngesting(Microbe player, IEngulfable ingested)
    {
        TutorialState.SendEvent(TutorialEventType.MicrobePlayerEngulfing, EventArgs.Empty, this);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerEngulfedByHostile(Microbe player, Microbe hostile)
    {
        if (hostile.CanDigestObject(player) == Microbe.DigestCheckResult.Ok)
            TutorialState.SendEvent(TutorialEventType.MicrobePlayerIsEngulfed, EventArgs.Empty, this);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerEngulfmentLimitReached(Microbe player)
    {
        TutorialState.SendEvent(TutorialEventType.MicrobePlayerEngulfmentFull, EventArgs.Empty, this);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerNoticeMessage(Microbe player, IHUDMessage message)
    {
        HUD.HUDMessages.ShowMessage(message);
    }

    /// <summary>
    ///   Updates the chemoreception lines
    /// </summary>
    [DeserializedCallbackAllowed]
    private void HandlePlayerChemoreception(Microbe microbe,
        IEnumerable<(Compound Compound, float Range, float MinAmount, Color Colour)> activeCompoundDetections,
        IEnumerable<(Species Species, float Range, Color Colour)> activeSpeciesDetections)
    {
        if (microbe != Player)
            GD.PrintErr("Chemoreception data reported for non-player cell");

        int currentLineIndex = 0;
        var position = microbe.GlobalTransform.origin;

        foreach (var detectedCompound in microbe.GetDetectedCompounds(Clouds))
        {
            UpdateOrCreateGuidanceLine(currentLineIndex++,
                null, detectedCompound.Colour, position, detectedCompound.Target, true);
        }

        foreach (var detectedSpecies in microbe.GetDetectedSpecies(microbeSystem))
        {
            UpdateOrCreateGuidanceLine(currentLineIndex++,
                detectedSpecies.Microbe, detectedSpecies.Colour, position, detectedSpecies.Target, true);
        }

        // Remove excess lines
        while (currentLineIndex < chemoreceptionLines.Count)
        {
            var line = chemoreceptionLines[chemoreceptionLines.Count - 1].Line;
            chemoreceptionLines.RemoveAt(chemoreceptionLines.Count - 1);

            RemoveChild(line);
            line.QueueFree();
        }
    }

    private void UpdateLinePlayerPosition()
    {
        if (Player == null || Player?.Dead == true)
        {
            foreach (var chemoreception in chemoreceptionLines)
                chemoreception.Line.Visible = false;

            return;
        }

        var position = Player!.GlobalTranslation;

        foreach (var chemoreception in chemoreceptionLines)
        {
            if (!chemoreception.Line.Visible)
                continue;

            chemoreception.Line.LineStart = position;

            // The target needs to be updated for detected microbes but not compounds.
            if (chemoreception.TargetMicrobe != null)
            {
                // Use colony parent position to avoid calling GlobalTranslation
                if (chemoreception.TargetMicrobe.Colony != null)
                {
                    chemoreception.Line.LineEnd = chemoreception.TargetMicrobe.Colony.Master.Translation;
                }
                else
                {
                    chemoreception.Line.LineEnd = chemoreception.TargetMicrobe.Translation;
                }
            }
        }
    }

    private void UpdateOrCreateGuidanceLine(int index,
        Microbe? targetMicrobe, Color colour, Vector3 lineStart, Vector3 lineEnd, bool visible)
    {
        if (index >= chemoreceptionLines.Count)
        {
            // The lines are created here and added as children of the stage because if they were in the microbe
            // then rotation and it moving cause implementation difficulties
            var line = new GuidanceLine();

            AddChild(line);
            if (targetMicrobe != null)
            {
                chemoreceptionLines.Add((line, targetMicrobe));
            }
            else
            {
                chemoreceptionLines.Add((line, null));
            }
        }
        else
        {
            chemoreceptionLines[index] = (chemoreceptionLines[index].Line, targetMicrobe);
        }

        chemoreceptionLines[index].Line.Colour = colour;
        chemoreceptionLines[index].Line.LineStart = lineStart;
        chemoreceptionLines[index].Line.LineEnd = lineEnd;
        chemoreceptionLines[index].Line.Visible = visible;
    }
}
