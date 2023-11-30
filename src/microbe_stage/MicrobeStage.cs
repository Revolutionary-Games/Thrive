using System;
using System.Collections.Generic;
using System.Linq;
using Components;
using DefaultEcs;
using DefaultEcs.Command;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main class for managing the microbe stage
/// </summary>
[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/microbe_stage/MicrobeStage.tscn")]
[DeserializedCallbackTarget]
[UseThriveSerializer]
public class MicrobeStage : CreatureStageBase<Entity, MicrobeWorldSimulation>
{
    [Export]
    public NodePath? GuidanceLinePath;

    private Compound glucose = null!;
    private Compound phosphate = null!;

    // This is no longer saved with child properties as it gets really complicated trying to load data into this from
    // a save
    private PatchManager patchManager = null!;

    private Patch? tempPatchManagerCurrentPatch;
    private float tempPatchManagerBrightness;

#pragma warning disable CA2213
    private MicrobeTutorialGUI tutorialGUI = null!;
    private GuidanceLine guidanceLine = null!;
#pragma warning restore CA2213

    private Vector3? guidancePosition;

    /// <summary>
    ///   Used to track chemoreception lines. If TargetMicrobe is null the target is static.
    /// </summary>
    private List<(GuidanceLine Line, Entity TargetEntity)> chemoreceptionLines = new();

    /// <summary>
    ///   Used to control how often compound position info is sent to the tutorial
    /// </summary>
    [JsonProperty]
    private float elapsedSinceEntityPositionCheck;

    [JsonProperty]
    private bool wonOnce;

    /// <summary>
    ///   Used to give increasing numbers to player offspring to know which is the latest
    /// </summary>
    [JsonProperty]
    private int playerOffspringTotalCount;

    private float maxLightLevel;

    private float templateMaxLightLevel;

    [JsonProperty]
    private bool appliedPlayerGodMode;

    // Because this is a scene loaded class, we can't do the following to avoid a temporary unused world simulation
    // from being created
    // [JsonConstructor]
    // public MicrobeStage(MicrobeWorldSimulation worldSimulation) : base(worldSimulation)
    // {
    // }

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public CompoundCloudSystem Clouds { get; private set; } = null!;

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

    [JsonIgnore]
    public override bool HasPlayer => Player.IsAlive;

    [JsonProperty]
    public Patch? SavedPatchManagerPatch
    {
        get => patchManager.ReadPreviousPatchForSave();
        set => tempPatchManagerCurrentPatch = value;
    }

    [JsonProperty]
    public float SavedPatchManagerBrightness
    {
        get => patchManager.ReadBrightnessForSave();
        set => tempPatchManagerBrightness = value;
    }

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
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        CheatManager.OnSpawnEnemyCheatUsed += OnSpawnEnemyCheatUsed;
        CheatManager.OnPlayerDuplicationCheatUsed += OnDuplicatePlayerCheatUsed;
        CheatManager.OnDespawnAllEntitiesCheatUsed += OnDespawnAllEntitiesCheatUsed;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        CheatManager.OnSpawnEnemyCheatUsed -= OnSpawnEnemyCheatUsed;
        CheatManager.OnPlayerDuplicationCheatUsed -= OnDuplicatePlayerCheatUsed;
        CheatManager.OnDespawnAllEntitiesCheatUsed -= OnDespawnAllEntitiesCheatUsed;

        DebugOverlays.Instance.OnWorldDisabled(WorldSimulation);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        WorldPosition playerPosition = default;

        if (HasPlayer)
        {
            try
            {
                // Intentional copy here to simplify the later block that also wants to use the player position
                playerPosition = Player.Get<WorldPosition>();
                WorldSimulation.ReportPlayerPosition(playerPosition.Position);
                DebugOverlays.Instance.ReportPositionCoordinates(playerPosition.Position);
            }
            catch (Exception e)
            {
                GD.PrintErr("Can't read player position: " + e);
            }
        }

        bool playerAlive = IsPlayerAlive();

        if (WorldSimulation.ProcessAll(delta))
        {
            // If game logic didn't run, the debug labels don't need to update
            DebugOverlays.Instance.UpdateActiveEntities(WorldSimulation);
        }

        if (gameOver || playerExtinctInCurrentPatch)
            return;

        if (playerAlive != IsPlayerAlive())
        {
            // Player just became dead
            GD.Print("Detected player is no longer alive after last simulation update");
            OnPlayerDied(Player);
            playerAlive = false;
        }

        if (HasPlayer)
        {
            DebugOverlays.Instance.ReportLookingAtCoordinates(Camera.CursorWorldPos);

            TutorialState.SendEvent(TutorialEventType.MicrobePlayerOrientation,
                new RotationEventArgs(playerPosition.Rotation,
                    playerPosition.Rotation.GetEuler() * MathUtils.RADIANS_TO_DEGREES), this);

            TutorialState.SendEvent(TutorialEventType.MicrobePlayerCompounds,
                new CompoundBagEventArgs(Player.Get<CompoundStorage>().Compounds), this);

            TutorialState.SendEvent(TutorialEventType.MicrobePlayerTotalCollected,
                new CompoundEventArgs(Player.Get<CompoundAbsorber>().TotalAbsorbedCompounds ??
                    throw new Exception("Player is missing absorbed compounds")), this);

            // TODO: if we start getting a ton of tutorial stuff reported each frame we should only report stuff when
            // relevant, for example only when in a colony or just leaving a colony should the player colony
            // info be sent
            if (Player.Has<MicrobeColony>())
            {
                TutorialState.SendEvent(TutorialEventType.MicrobePlayerColony,
                    new MicrobeColonyEventArgs(true, Player.Get<MicrobeColony>().ColonyMembers.Length), this);

                if (playerAlive && GameWorld.PlayerSpecies is EarlyMulticellularSpecies)
                {
                    MakeEditorForFreebuildAvailable();
                }
            }
            else if (playerAlive)
            {
                MakeEditorForFreebuildAvailable();
            }

            elapsedSinceEntityPositionCheck += delta;

            if (elapsedSinceEntityPositionCheck > Constants.TUTORIAL_ENTITY_POSITION_UPDATE_INTERVAL)
            {
                elapsedSinceEntityPositionCheck = 0;

                if (TutorialState.WantsNearbyCompoundInfo())
                {
                    TutorialState.SendEvent(TutorialEventType.MicrobeCompoundsNearPlayer,
                        new EntityPositionEventArgs(Clouds.FindCompoundNearPoint(playerPosition.Position, glucose)),
                        this);
                }

                if (TutorialState.WantsNearbyEngulfableInfo())
                {
                    // Filter to spawned engulfables that can be despawned (this likely just filters out the player
                    // themselves
                    ref var engulfer = ref Player.Get<Engulfer>();

                    var position = engulfer.FindNearestEngulfableSlow(ref Player.Get<CellProperties>(),
                        ref Player.Get<OrganelleContainer>(), ref Player.Get<WorldPosition>(),
                        Player.Get<CompoundStorage>().Compounds, Player, Player.Get<SpeciesMember>().ID,
                        WorldSimulation);

                    TutorialState.SendEvent(TutorialEventType.MicrobeChunksNearPlayer,
                        new EntityPositionEventArgs(position), this);
                }

                guidancePosition = TutorialState.GetPlayerGuidancePosition();
            }

            if (guidancePosition != null)
            {
                guidanceLine.Visible = true;
                guidanceLine.LineStart = playerPosition.Position;
                guidanceLine.LineEnd = guidancePosition.Value;
            }
            else
            {
                guidanceLine.Visible = false;
            }

            // Apply player god mode
            ref var playerHealth = ref Player.Get<Health>();

            if (playerHealth.Invulnerable != CheatManager.GodMode)
            {
                // Only reset invulnerability if set by god mode
                if (playerHealth.Invulnerable && appliedPlayerGodMode)
                {
                    playerHealth.Invulnerable = false;
                    appliedPlayerGodMode = false;
                }
                else if (!playerHealth.Invulnerable)
                {
                    GD.Print("Enabling microbe god mode");
                    playerHealth.Invulnerable = true;
                    appliedPlayerGodMode = true;
                }
            }
        }
        else
        {
            guidanceLine.Visible = false;
        }

        UpdateLinePlayerPosition();
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
        var biome = CurrentGame!.GameWorld.Map.CurrentPatch!.BiomeTemplate;

        Jukebox.Instance.PlayCategory(GameWorld.PlayerSpecies is EarlyMulticellularSpecies ?
            "EarlyMulticellularStage" :
            "MicrobeStage", biome.ActiveMusicContexts);
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
        if (!HasPlayer || Player.Get<Health>().Dead || PlayerIsEngulfed(Player))
        {
            GD.PrintErr("Player object disappeared, died, or was engulfed while transitioning to the editor");
            HUD.OnCancelEditorEntry();
            return;
        }

        if (CurrentGame == null)
            throw new InvalidOperationException("Stage has no current game");

        Node sceneInstance;

        if (Player.Has<EarlyMulticellularSpeciesMember>())
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
            if (Player.Has<MicrobeColony>())
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
        if (!HasPlayer || Player.Get<Health>().Dead || !Player.Has<MicrobeColony>() || PlayerIsEngulfed(Player))
        {
            GD.PrintErr("Player object disappeared or died (or not in a colony) while trying to become multicellular");
            HUD.OnCancelEditorEntry();
            return;
        }

        ref var entitySpecies = ref Player.Get<SpeciesMember>();

        var previousSpecies = entitySpecies.Species;
        previousSpecies.Obsolete = true;

        // Log becoming multicellular in the timeline
        GameWorld.LogEvent(
            new LocalizedString("TIMELINE_SPECIES_BECAME_MULTICELLULAR", previousSpecies.FormattedName),
            true, "multicellularTimelineMembraneTouch.png");

        GameWorld.Map.CurrentPatch!.LogEvent(
            new LocalizedString("TIMELINE_SPECIES_BECAME_MULTICELLULAR", previousSpecies.FormattedName),
            true, "multicellularTimelineMembraneTouch.png");

        if (WorldSimulation.Processing)
            throw new Exception("This shouldn't be ran while world is in the middle of a simulation");

        GD.Print("Disbanding colony and becoming multicellular");

        // Move to multicellular always happens when the player is in a colony, so we force disband that here before
        // proceeding
        MicrobeColonyHelpers.UnbindAllOutsideGameUpdate(Player, WorldSimulation);

        if (Player.Has<MicrobeColony>())
            throw new Exception("Unbind failed");

        GiveReproductionPopulationBonus();

        CurrentGame!.EnterPrototypes();

        var playerSpeciesMicrobes = GetAllPlayerSpeciesMicrobes();

        // Re-apply species here so that the player cell knows it is multicellular after this
        // Also apply species here to other members of the player's previous species
        // This prevents previous members of the player's colony from immediately being hostile
        bool playerHandled = false;

        var multicellularSpecies = GameWorld.ChangeSpeciesToMulticellular(previousSpecies);
        foreach (var microbe in playerSpeciesMicrobes)
        {
            // Direct component setting is safe as we verified above we aren't running during a simulation update
            microbe.Remove<MicrobeSpeciesMember>();
            microbe.Set(new SpeciesMember(multicellularSpecies));
            microbe.Set(new EarlyMulticellularSpeciesMember(multicellularSpecies, multicellularSpecies.CellTypes[0]));

            microbe.Set(new MulticellularGrowth(multicellularSpecies));

            if (microbe.Has<PlayerMarker>())
                playerHandled = true;
        }

        if (!playerHandled)
            throw new Exception("Did not find player to apply multicellular species to");

        GameWorld.NotifySpeciesChangedStages();

        // Make sure no queued player species can spawn with the old species
        WorldSimulation.SpawnSystem.ClearSpawnQueue();

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
        if (!HasPlayer || Player.Get<Health>().Dead || !Player.Has<MicrobeColony>())
        {
            GD.PrintErr("Player object disappeared or died (or not in a colony) while trying to become macroscopic");
            return;
        }

        GD.Print("Becoming late multicellular (macroscopic)");

        // We don't really need to handle the player state or anything like that here as once we go to the late
        // multicellular editor, we never return to the microbe stage. So we don't need that much setup as becoming
        // multicellular

        // We don't really need to disband the colony here

        GiveReproductionPopulationBonus();

        CurrentGame!.EnterPrototypes();

        var modifiedSpecies = GameWorld.ChangeSpeciesToLateMulticellular(Player.Get<SpeciesMember>().Species);

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

        ProceduralDataCache.Instance.OnEnterState(MainGameState.MicrobeStage);

        // Add a cloud of glucose if difficulty settings call for it
        if (GameWorld.WorldSettings.FreeGlucoseCloud)
        {
            Clouds.AddCloud(glucose, 200000.0f, Player.Get<WorldPosition>().Position + new Vector3(0.0f, 0.0f, -25.0f));
        }

        // Check win conditions

        if (!CurrentGame!.FreeBuild && GameWorld.PlayerSpecies.Generation >= 20 &&
            GameWorld.PlayerSpecies.Population >= 300 && !wonOnce)
        {
            HUD.ToggleWinBox();
            wonOnce = true;
        }

        var playerSpecies = Player.Get<SpeciesMember>().Species;

        // Update the player's cell
        ref var cellProperties = ref Player.Get<CellProperties>();

        bool playerIsMulticellular = Player.Has<EarlyMulticellularSpeciesMember>();

        if (playerIsMulticellular)
        {
            ref var earlySpeciesType = ref Player.Get<EarlyMulticellularSpeciesMember>();

            // Allow updating the first cell type to reproduce (reproduction order changed)
            earlySpeciesType.MulticellularCellType = earlySpeciesType.Species.Cells[0].CellType;

            cellProperties.ReApplyCellTypeProperties(Player, earlySpeciesType.MulticellularCellType,
                earlySpeciesType.Species, WorldSimulation);
        }
        else
        {
            ref var species = ref Player.Get<MicrobeSpeciesMember>();
            cellProperties.ReApplyCellTypeProperties(Player, species.Species, species.Species, WorldSimulation);
        }

        var playerPosition = Player.Get<WorldPosition>().Position;

        // Spawn another cell from the player species
        // This needs to be done after updating the player so that multicellular organisms are accurately separated
        cellProperties.Divide(ref Player.Get<OrganelleContainer>(), Player, playerSpecies, WorldSimulation,
            WorldSimulation.SpawnSystem, (ref EntityRecord daughter) =>
            {
                // Mark as player reproduced entity
                daughter.Set(new PlayerOffspring
                {
                    OffspringOrderNumber = ++playerOffspringTotalCount,
                });

                // If multicellular, we want that other cell colony to be fully grown to show budding in action
            }, MulticellularSpawnState.FullColony);

        // This is queued to run on the world after the next update as that's when the duplicate entity will spawn
        // The entity is not forced to spawn here immediately to reduce the lag impact that is already caused by
        // switching from the editor back to the stage scene
        WorldSimulation.Invoke(() =>
        {
            // We need to find the entity reference of the offspring that was spawned last frame
            var doNotDespawn = PlayerOffspringHelpers.FindLatestSpawnedOffspring(WorldSimulation.EntitySystem);

#if DEBUG
            if (doNotDespawn.IsAlive && !doNotDespawn.Has<Spawned>())
            {
                throw new Exception(
                    "Spawned player offspring has no spawned component, microbe reproduction method is" +
                    "working incorrectly");
            }
#endif

            WorldSimulation.SpawnSystem.EnsureEntityLimitAfterPlayerReproduction(playerPosition, doNotDespawn);
        });

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
        if (HasPlayer)
        {
            ref var health = ref Player.Get<Health>();

            // This doesn't use the microbe damage calculation as this damage can't be resisted
            health.DealDamage(9999.0f, "suicide");

            // Force digestion to complete immediately
            if (Player.Has<Engulfable>())
            {
                ref var engulfable = ref Player.Get<Engulfable>();

                if (engulfable.PhagocytosisStep is not (PhagocytosisPhase.None or PhagocytosisPhase.Ejection))
                {
                    GD.Print("Forcing player digestion to progress much faster");

                    // Seems like there's no really good way to force digestion to complete immediately, so instead we
                    // clear everything here to force the digestion to complete immediately
                    engulfable.AdditionalEngulfableCompounds?.Clear();

                    ref var storage = ref Player.Get<CompoundStorage>();
                    storage.Compounds.ClearCompounds();
                }
            }
        }
    }

    protected override void SetupStage()
    {
        EnsureWorldSimulationIsCreated();

        // Initialise the simulation on a basic level first to ensure the base stage setup has all the objects it needs
        WorldSimulation.Init(rootOfDynamicallySpawned, Clouds);

        patchManager = new PatchManager(WorldSimulation.SpawnSystem, WorldSimulation.ProcessSystem,
            WorldSimulation.MicrobeProcessManagerSystem, Clouds, WorldSimulation.TimedLifeSystem, worldLight);

        if (IsLoadedFromSave)
        {
            patchManager.ApplySaveState(tempPatchManagerCurrentPatch, tempPatchManagerBrightness);
            tempPatchManagerCurrentPatch = null;
        }

        base.SetupStage();

        // Hook up the simulation to some of the other systems
        WorldSimulation.CameraFollowSystem.Camera = Camera;
        HoverInfo.PhysicalWorld = WorldSimulation.PhysicalWorld;

        // Init the simulation and finish setting up the systems (for example cloud init happens here)
        WorldSimulation.InitForCurrentGame(CurrentGame!);

        tutorialGUI.EventReceiver = TutorialState;
        HUD.SendEditorButtonToTutorial(TutorialState);

        ProceduralDataCache.Instance.OnEnterState(MainGameState.MicrobeStage);

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

        var spawnLocation = Vector3.Zero;

        if (spawnedPlayer)
        {
            // Random location on respawn
            spawnLocation = new Vector3(
                random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE), 0,
                random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE));

            WorldSimulation.ClearPlayerLocationDependentCaches();
        }

        var (recorder, _) = SpawnHelpers.SpawnMicrobeWithoutFinalizing(WorldSimulation, GameWorld.PlayerSpecies,
            spawnLocation, false, null, out var entityRecord);

        entityRecord.Set(new MicrobeEventCallbacks
        {
            OnReproductionStatus = OnPlayerReproductionStatusChanged,

            OnUnbound = OnPlayerUnbound,
            OnUnbindEnabled = OnPlayerUnbindEnabled,

            OnChemoreceptionInfo = HandlePlayerChemoreception,

            OnIngestedByHostile = OnPlayerEngulfedByHostile,
            OnSuccessfulEngulfment = OnPlayerIngesting,
            OnEngulfmentStorageFull = OnPlayerEngulfmentLimitReached,
            OnEjectedFromHostileEngulfer = OnPlayerEjectedFromHostileEngulfer,

            OnNoticeMessage = OnPlayerNoticeMessage,
        });

        entityRecord.Set<CameraFollowTarget>();

        // Spawn and grab the player
        SpawnHelpers.FinalizeEntitySpawn(recorder, WorldSimulation);
        WorldSimulation.ProcessDelaySpawnedEntitiesImmediately();

        Player = WorldSimulation.FindFirstEntityWithComponent<PlayerMarker>();

        if (!IsPlayerAlive())
            throw new InvalidOperationException("Player spawn didn't create player entity correctly");

        // We despawn everything here as either the player just spawned for the first time or died and is being spawned
        // at a different location
        WorldSimulation.SpawnSystem.DespawnAll();

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

        if (!Player.Has<EarlyMulticellularSpeciesMember>())
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

            if (HasPlayer)
            {
                ref var engulfer = ref Player.Get<Engulfer>();

                engulfer.DeleteEngulfedObjects(WorldSimulation);
            }
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
        // TODO: re-read the player entity from the simulation as it is not currently saved (there should be a TODO
        // in somewhere like the entity reference converter about making this possible)
        Player = WorldSimulation.FindFirstEntityWithComponent<PlayerMarker>();

        if (!HasPlayer)
        {
            GD.Print("Loaded game doesn't have a currently alive player");
        }
    }

    /// <summary>
    ///   Helper function for transition to multicellular. For normal gameplay this would be not optimal as this
    ///   uses the slow world entity fetching.
    /// </summary>
    /// <returns>Enumerable of all microbes of Player's species</returns>
    private IEnumerable<Entity> GetAllPlayerSpeciesMicrobes()
    {
        if (Player == null)
            throw new InvalidOperationException("Could not get player species microbes: no Player object");

        var species = Player.Get<SpeciesMember>().ID;

        foreach (var entity in WorldSimulation.EntitySystem)
        {
            if (!entity.Has<SpeciesMember>())
                continue;

            if (entity.Get<SpeciesMember>().ID == species)
                yield return entity;
        }
    }

    private void OnSpawnEnemyCheatUsed(object sender, EventArgs e)
    {
        if (!HasPlayer)
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

        var playerPosition = Player.Get<WorldPosition>().Position;

        var (recorder, weight) = SpawnHelpers.SpawnMicrobeWithoutFinalizing(WorldSimulation, randomSpecies,
            playerPosition + Vector3.Forward * 20, true, null, out var entity);

        // Make the cell despawn like normal
        WorldSimulation.SpawnSystem.NotifyExternalEntitySpawned(entity,
            Constants.MICROBE_SPAWN_RADIUS * Constants.MICROBE_SPAWN_RADIUS, weight);

        SpawnHelpers.FinalizeEntitySpawn(recorder, WorldSimulation);
    }

    private void OnDuplicatePlayerCheatUsed(object sender, EventArgs e)
    {
        if (!IsPlayerAlive())
        {
            GD.PrintErr("Can't use duplicate player cheat as player is dead");
            return;
        }

        // If this were allowed to happen during editor entry there would probably be some bugs
        if (MovingToEditor)
            return;

        ref var cellProperties = ref Player.Get<CellProperties>();
        ref var organelles = ref Player.Get<OrganelleContainer>();
        var playerSpecies = Player.Get<SpeciesMember>().Species;

        cellProperties.Divide(ref organelles, Player, playerSpecies, WorldSimulation, WorldSimulation.SpawnSystem,
            null, MulticellularSpawnState.ChanceForFullColony);
    }

    private void OnDespawnAllEntitiesCheatUsed(object? sender, EventArgs args)
    {
        WorldSimulation.SpawnSystem.DespawnAll();
    }

    /// <summary>
    ///   This is now handled by this class instead of adding extra functionality to the microbe simulation for only
    ///   thing that is needed for the player.
    /// </summary>
    private void OnPlayerDied(Entity player)
    {
        HandlePlayerDeath();

        bool engulfed = PlayerIsEngulfed(player);

        // Engulfing death has a different tutorial
        if (!engulfed)
            TutorialState.SendEvent(TutorialEventType.MicrobePlayerDied, EventArgs.Empty, this);

        // Don't clear the player object here as we want to wait until the player entity is deleted before creating
        // a new one to avoid having two player entities existing at the same time
    }

    private bool PlayerIsEngulfed(Entity player)
    {
        if (player.IsAlive && player.Has<Engulfable>())
        {
            return player.Get<Engulfable>().PhagocytosisStep != PhagocytosisPhase.None;
        }

        return false;
    }

    /// <summary>
    ///   Makes the freebuild editor immediately available (called each update as long as the player is alive)
    /// </summary>
    private void MakeEditorForFreebuildAvailable()
    {
        if (PlayerIsEngulfed(Player))
            return;

        if (!CurrentGame!.FreeBuild)
            return;

        OnCanEditStatusChanged(true);
    }

    // These need to use invoke as during gameplay code these can be called in a multithreaded way
    [DeserializedCallbackAllowed]
    private void OnPlayerReproductionStatusChanged(Entity player, bool ready)
    {
        Invoke.Instance.QueueForObject(() => OnCanEditStatusChanged(ready &&
            (!player.Has<MicrobeColony>() || GameWorld.PlayerSpecies is not MicrobeSpecies)), this);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerUnbindEnabled(Entity player)
    {
        Invoke.Instance.QueueForObject(
            () => TutorialState.SendEvent(TutorialEventType.MicrobePlayerUnbindEnabled, EventArgs.Empty, this), this);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerUnbound(Entity player)
    {
        Invoke.Instance.QueueForObject(
            () => TutorialState.SendEvent(TutorialEventType.MicrobePlayerUnbound, EventArgs.Empty, this), this);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerIngesting(Entity player, Entity ingested)
    {
        Invoke.Instance.QueueForObject(
            () => TutorialState.SendEvent(TutorialEventType.MicrobePlayerEngulfing, EventArgs.Empty, this), this);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerEngulfedByHostile(Entity player, Entity hostile)
    {
        Invoke.Instance.QueueForObject(() =>
        {
            try
            {
                ref var hostileCell = ref hostile.Get<OrganelleContainer>();

                ref var engulfable = ref player.Get<Engulfable>();

                if (hostileCell.CanDigestObject(ref engulfable) == DigestCheckResult.Ok)
                {
                    TutorialState.SendEvent(TutorialEventType.MicrobePlayerIsEngulfed, EventArgs.Empty, this);

                    OnCanEditStatusChanged(false);
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("Couldn't process player engulfed by hostile event: " + e);
            }
        }, this);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerEjectedFromHostileEngulfer(Entity player)
    {
        // Re-check the reproduction status with the normal reproduction status check
        OnPlayerReproductionStatusChanged(player, player.Get<OrganelleContainer>().AllOrganellesDivided);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerEngulfmentLimitReached(Entity player)
    {
        Invoke.Instance.QueueForObject(
            () => TutorialState.SendEvent(TutorialEventType.MicrobePlayerEngulfmentFull, EventArgs.Empty, this), this);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerNoticeMessage(Entity player, IHUDMessage message)
    {
        Invoke.Instance.QueueForObject(() => HUD.HUDMessages.ShowMessage(message), this);
    }

    /// <summary>
    ///   Updates the chemoreception lines. Not called in a multithreaded way
    /// </summary>
    [DeserializedCallbackAllowed]
    private void HandlePlayerChemoreception(Entity microbe,
        List<(Compound Compound, Color Colour, Vector3 Target)>? activeCompoundDetections,
        List<(Species Species, Entity Entity, Color Colour, Vector3 Target)>? activeSpeciesDetections)
    {
        if (microbe != Player)
            GD.PrintErr("Chemoreception data reported for non-player cell");

        int currentLineIndex = 0;
        var position = microbe.Get<WorldPosition>().Position;

        if (activeCompoundDetections != null)
        {
            foreach (var detectedCompound in activeCompoundDetections)
            {
                UpdateOrCreateGuidanceLine(currentLineIndex++,
                    default, detectedCompound.Colour, position, detectedCompound.Target, true);
            }
        }

        if (activeSpeciesDetections != null)
        {
            foreach (var detectedSpecies in activeSpeciesDetections)
            {
                UpdateOrCreateGuidanceLine(currentLineIndex++,
                    detectedSpecies.Entity, detectedSpecies.Colour, position, detectedSpecies.Target, true);
            }
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
        if (!HasPlayer || Player.Get<Health>().Dead)
        {
            foreach (var chemoreception in chemoreceptionLines)
                chemoreception.Line.Visible = false;

            return;
        }

        var position = Player.Get<WorldPosition>().Position;

        foreach (var chemoreception in chemoreceptionLines)
        {
            if (!chemoreception.Line.Visible)
                continue;

            chemoreception.Line.LineStart = position;

            // The target needs to be updated for entities with a position.
            if (chemoreception.TargetEntity.IsAlive && chemoreception.TargetEntity.Has<WorldPosition>())
            {
                chemoreception.Line.LineEnd = chemoreception.TargetEntity.Get<WorldPosition>().Position;
            }
        }
    }

    private void UpdateOrCreateGuidanceLine(int index,
        Entity potentialTargetEntity, Color colour, Vector3 lineStart, Vector3 lineEnd, bool visible)
    {
        if (index >= chemoreceptionLines.Count)
        {
            // The lines are created here and added as children of the stage because if they were in the microbe
            // then rotation and it moving cause implementation difficulties
            var line = new GuidanceLine();

            AddChild(line);
            chemoreceptionLines.Add((line, potentialTargetEntity));
        }
        else
        {
            chemoreceptionLines[index] = (chemoreceptionLines[index].Line, potentialTargetEntity);
        }

        chemoreceptionLines[index].Line.Colour = colour;
        chemoreceptionLines[index].Line.LineStart = lineStart;
        chemoreceptionLines[index].Line.LineEnd = lineEnd;
        chemoreceptionLines[index].Line.Visible = visible;
    }

    private bool IsPlayerAlive()
    {
        if (!HasPlayer)
            return false;

        try
        {
            return !Player.Get<Health>().Dead;
        }
        catch (Exception e)
        {
            GD.PrintErr("Couldn't read player health: " + e);
            return false;
        }
    }

    private void TranslationsForFeaturesToReimplement()
    {
        // TODO: reimplement the microbe features that depend on these translations
        TranslationServer.Translate("SUCCESSFUL_KILL");
        TranslationServer.Translate("SUCCESSFUL_SCAVENGE");
        TranslationServer.Translate("ESCAPE_ENGULFING");
    }
}
