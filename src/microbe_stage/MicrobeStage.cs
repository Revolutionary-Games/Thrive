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
public partial class MicrobeStage : CreatureStageBase<Entity, MicrobeWorldSimulation>, IMicrobeSpawnEnvironment
{
    [Export]
    public NodePath? GuidanceLinePath;

    private readonly Dictionary<MicrobeSpecies, ResolvedMicrobeTolerances> resolvedTolerancesCache = new();

    private OrganelleDefinition cytoplasm = null!;

    // This is no longer saved with child properties as it gets really complicated trying to load data into this from
    // a save
    private PatchManager patchManager = null!;

    private Patch? tempPatchManagerCurrentPatch;
    private float tempPatchManagerBrightness;

#pragma warning disable CA2213
    [Export]
    private Node3D heatViewOverlay = null!;

    [Export]
    private FluidCurrentDisplay fluidCurrentDisplay = null!;

    [Export]
    private MovementModeSelectionPopup movementModeSelectionPopup = null!;

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
    private double elapsedSinceEntityPositionCheck = Constants.TUTORIAL_ENTITY_POSITION_UPDATE_INTERVAL + 1;

    [JsonProperty]
    private bool wonOnce;

    [JsonProperty]
    private double movementModeShowTimer;

    /// <summary>
    ///   Used to mark the first time the player turns off tutorials in the game
    /// </summary>
    [JsonProperty]
    private bool tutorialCanceledOnce;

    /// <summary>
    ///   Used to detect when the welcome tutorial is over and some state should be checked.
    ///   For example, having already played certain tutorials requires restoring the compounds panel to open.
    /// </summary>
    private bool waitingForWelcomeTutorialToEnd;

    [JsonProperty]
    private bool environmentPanelAutomaticallyOpened;

    /// <summary>
    ///   Used to give increasing numbers to player offspring to know which is the latest
    /// </summary>
    [JsonProperty]
    private int playerOffspringTotalCount;

    private float maxLightLevel;

    private float templateMaxLightLevel;

    [JsonProperty]
    private bool appliedPlayerGodMode;

    private bool appliedUnlimitGrowthSpeed;

    /// <summary>
    ///   Used to ferry data between the patch change logic and handling the player cell splitting (as I couldn't
    ///   figure out another way to fit this into the code architecture -hhyyrylainen)
    /// </summary>
    private bool switchedPatchInEditorForCompounds;

    // Because this is a scene-loaded class, we can't do the following to avoid a temporary unused world simulation
    // from being created
    // [JsonConstructor]
    // public MicrobeStage(MicrobeWorldSimulation worldSimulation) : base(worldSimulation)
    // {
    // }

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public CompoundCloudSystem Clouds { get; private set; } = null!;

    /// <summary>
    ///   The main camera. This needs to be after anything with AssignOnlyChildItemsOnDeserialize due to load order
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

    [JsonIgnore]
    public override bool HasAlivePlayer => HasPlayer && IsPlayerAlive();

    [JsonIgnore]
    public IDaylightInfo DaylightInfo => GameWorld.LightCycle;

    public WorldGenerationSettings WorldSettings => GameWorld.WorldSettings;

    [JsonIgnore]
    public BiomeConditions CurrentBiome => GameWorld.Map.CurrentPatch?.Biome ??
        throw new InvalidOperationException("no current patch set");

    /// <summary>
    ///   Makes saving information related to the patch manager work. This checks the patch manager against null to
    ///   make saves made in the editor after loading a save made in the editor work.
    /// </summary>
    [JsonProperty]
    public Patch? SavedPatchManagerPatch
    {
        get => patchManager == null! ? tempPatchManagerCurrentPatch : patchManager.ReadPreviousPatchForSave();
        set => tempPatchManagerCurrentPatch = value;
    }

    [JsonProperty]
    public float SavedPatchManagerBrightness
    {
        get => patchManager == null! ? tempPatchManagerBrightness : patchManager.ReadBrightnessForSave();
        set => tempPatchManagerBrightness = value;
    }

    public override MainGameState GameState => MainGameState.MicrobeStage;

    protected override ICreatureStageHUD BaseHUD => HUD;

    private LocalizedString CurrentPatchName =>
        GameWorld.Map.CurrentPatch?.Name ?? throw new InvalidOperationException("no current patch");

    /// <summary>
    ///   This method gets called the first time the stage scene is put into an active scene tree.
    ///   So returning from the editor doesn't cause this to re-run.
    /// </summary>
    public override void _Ready()
    {
        base._Ready();

        // Start a new game if started directly from MicrobeStage.tscn
        CurrentGame ??= GameProperties.StartNewMicrobeGame(new WorldGenerationSettings());

        ResolveNodeReferences();

        var simulationParameters = SimulationParameters.Instance;
        cytoplasm = simulationParameters.GetOrganelleType("cytoplasm");

        tutorialGUI.Visible = true;
        HUD.Init(this);
        HoverInfo.Init(Clouds, Camera);

        // Do stage setup to spawn things and setup all parts of the stage
        SetupStage();

        fluidCurrentDisplay.Init(WorldSimulation, Camera);
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

        // Re-register these callbacks in case it is necessary
        // The primary registration for this is in OnGameStarted
        if (CurrentGame != null && HUD != null!)
        {
            TutorialState.GlucoseCollecting.OnOpened += SetupPlayerForGlucoseCollecting;
            TutorialState.DayNightTutorial.OnOpened += HUD.CloseProcessPanel;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        CheatManager.OnSpawnEnemyCheatUsed -= OnSpawnEnemyCheatUsed;
        CheatManager.OnPlayerDuplicationCheatUsed -= OnDuplicatePlayerCheatUsed;
        CheatManager.OnDespawnAllEntitiesCheatUsed -= OnDespawnAllEntitiesCheatUsed;

        DebugOverlays.Instance.OnWorldDisabled(WorldSimulation);

        if (CurrentGame != null)
        {
            TutorialState.GlucoseCollecting.OnOpened -= SetupPlayerForGlucoseCollecting;
            TutorialState.DayNightTutorial.OnOpened -= HUD.CloseProcessPanel;
        }
    }

    public override void _Process(double delta)
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

            HandleMovementModePrompt(delta);
        }

        bool playerAlive = HasAlivePlayer;

        if (WorldSimulation.ProcessAll((float)delta))
        {
            // If game logic didn't run, the debug labels don't need to update
            DebugOverlays.Instance.UpdateActiveEntities(WorldSimulation);
        }

        if (gameOver || playerExtinctInCurrentPatch)
            return;

        if (playerAlive != HasAlivePlayer)
        {
            // Player just became dead
            GD.Print("Detected player is no longer alive after last simulation update");
            OnPlayerDied(Player);
            playerAlive = false;
        }

        if (HasPlayer)
        {
            DebugOverlays.Instance.ReportLookingAtCoordinates(Camera.CursorWorldPos);
            DebugOverlays.Instance.ReportHeatValue(WorldSimulation.SampleTemperatureAt(Camera.CursorWorldPos));

            TutorialState.SendEvent(TutorialEventType.MicrobePlayerOrientation,
                new RotationEventArgs(playerPosition.Rotation, playerPosition.Rotation.GetEuler()), this);

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
                    new MicrobeColonyEventArgs(true, Player.Get<MicrobeColony>().ColonyMembers.Length,
                        Player.Has<MulticellularSpeciesMember>()), this);

                if (playerAlive && GameWorld.PlayerSpecies is MulticellularSpecies)
                {
                    MakeEditorForFreebuildAvailable();
                }
            }
            else if (playerAlive)
            {
                MakeEditorForFreebuildAvailable();
            }

            if (Player.Has<CompoundStorage>())
            {
                var storage = Player.Get<CompoundStorage>();
                var compounds = storage.Compounds;
                HUD.UpdateRadiationBar(compounds.GetCompoundAmount(Compound.Radiation),
                    compounds.GetCapacityForCompound(Compound.Radiation), Constants.RADIATION_WARNING);
            }

            elapsedSinceEntityPositionCheck += delta;

            if (elapsedSinceEntityPositionCheck > Constants.TUTORIAL_ENTITY_POSITION_UPDATE_INTERVAL)
            {
                elapsedSinceEntityPositionCheck = 0;

                if (TutorialState.WantsNearbyCompoundInfo())
                {
                    TutorialState.SendEvent(TutorialEventType.MicrobeCompoundsNearPlayer,
                        new EntityPositionEventArgs(Clouds.FindCompoundNearPoint(playerPosition.Position,
                            Compound.Glucose)), this);
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

            if (waitingForWelcomeTutorialToEnd)
            {
                // Check if the welcome tutorial has ended
                if (TutorialState.MicrobeStageWelcome.Complete)
                {
                    waitingForWelcomeTutorialToEnd = false;

                    // Restore panel state if tutorials won't be played
                    bool showCompounds = TutorialState.GlucoseCollecting.Complete;
                    bool showEnvironment = TutorialState.DayNightTutorial.Complete;

                    if (showEnvironment)
                        HUD.ShowEnvironmentPanel();

                    if (showCompounds)
                        HUD.ShowCompoundPanel();
                }
            }

            if (!environmentPanelAutomaticallyOpened)
            {
                // Open panel automatically if taking radiation
                var compounds = Player.Get<CompoundStorage>().Compounds;
                if (compounds.GetCompoundAmount(Compound.Radiation) > 0.1f)
                {
                    HUD.ShowEnvironmentPanel();
                    environmentPanelAutomaticallyOpened = true;
                }
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

            if (appliedUnlimitGrowthSpeed != CheatManager.UnlimitedGrowthSpeed)
            {
                appliedUnlimitGrowthSpeed = CheatManager.UnlimitedGrowthSpeed;
                CurrentGame!.GameWorld.WorldSettings.Difficulty.SetGrowthRateLimitCheatOverride(!CheatManager
                    .UnlimitedGrowthSpeed);
            }
        }
        else
        {
            guidanceLine.Visible = false;

            HUD.UpdateRadiationBar(0, 1, 1);
        }

        UpdateLinePlayerPosition();
    }

    public override void OnFinishTransitioning()
    {
        base.OnFinishTransitioning();

        if (GameWorld.PlayerSpecies is not MulticellularSpecies)
        {
            TutorialState.SendEvent(TutorialEventType.EnteredMicrobeStage,
                new AggregateEventArgs(new CallbackEventArgs(() => HUD.ShowPatchName(CurrentPatchName.ToString())),
                    new GameWorldEventArgs(GameWorld)), this);
        }
        else
        {
            TutorialState.SendEvent(TutorialEventType.EnteredMulticellularStage, EventArgs.Empty, this);
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

        Jukebox.Instance.PlayCategory(GameWorld.PlayerSpecies is MulticellularSpecies ?
            "MulticellularStage" :
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

    [RunOnKeyDown("g_toggle_speed_mode")]
    public void ToggleSpeedMode()
    {
        HUD.ApplySpeedMode(!HUD.GetCurrentSpeedMode());
    }

    public override void SetSpecialViewMode(ViewMode mode)
    {
        if (mode == ViewMode.Normal)
        {
            heatViewOverlay.Visible = false;
        }
        else if (mode == ViewMode.Heat)
        {
            heatViewOverlay.Visible = true;
        }
        else
        {
            base.SetSpecialViewMode(mode);
        }
    }

    public void RecordPlayerReproduction()
    {
        GameWorld.StatisticsTracker.PlayerReproductionStatistic.RecordPlayerReproduction(Player,
            GameWorld.Map.CurrentPatch!.BiomeTemplate);
    }

    public ResolvedMicrobeTolerances GetSpeciesTolerances(MicrobeSpecies microbeSpecies)
    {
        // Use caching to speed up spawning
        lock (resolvedTolerancesCache)
        {
            if (resolvedTolerancesCache.TryGetValue(microbeSpecies, out var cached))
                return cached;

            var tolerances =
                MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(microbeSpecies, CurrentBiome);

            cached = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(tolerances);

            resolvedTolerancesCache[microbeSpecies] = cached;

            return cached;
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

        // Update endosymbiosis progress now that the player got to the editor
        if (Player.Has<TemporaryEndosymbiontInfo>())
        {
            ref var endosymbiontInfo = ref Player.Get<TemporaryEndosymbiontInfo>();

            endosymbiontInfo.UpdateEndosymbiosisProgress(Player.Get<SpeciesMember>().Species);
        }

        Node sceneInstance;

        if (Player.Has<MulticellularSpeciesMember>())
        {
            // Player is a multicellular species, go to multicellular editor

            var scene = SceneManager.Instance.LoadScene(MainGameState.MulticellularEditor);

            sceneInstance = scene.Instantiate();
            var editor = (MulticellularEditor)sceneInstance;

            editor.CurrentGame = CurrentGame;
            editor.ReturnToStage = this;

            // TODO: severely limit the MP points in awakening stage
        }
        else
        {
            // This might not be required anymore but just for extra safety this is here
            if (Player.Has<MicrobeColony>())
            {
                GD.PrintErr("Editor button was enabled and pressed while the player is in a colony");
                MovingToEditor = false;
                return;
            }

            var scene = SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor);

            sceneInstance = scene.Instantiate();
            var editor = (MicrobeEditor)sceneInstance;

            editor.CurrentGame = CurrentGame;
            editor.ReturnToStage = this;
        }

        GiveReproductionPopulationBonus();

        RecordPlayerReproduction();

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
        GameWorld.LogEvent(new LocalizedString("TIMELINE_SPECIES_BECAME_MULTICELLULAR",
            previousSpecies.FormattedNameBbCodeUnstyled), true, false, "multicellularTimelineMembraneTouch.png");

        GameWorld.Map.CurrentPatch!.LogEvent(
            new LocalizedString("TIMELINE_SPECIES_BECAME_MULTICELLULAR", previousSpecies.FormattedNameBbCodeUnstyled),
            true, false, "multicellularTimelineMembraneTouch.png");

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
            microbe.Set(new MulticellularSpeciesMember(multicellularSpecies, multicellularSpecies.CellTypes[0], 0));

            microbe.Set(new MulticellularGrowth(multicellularSpecies));

            if (microbe.Has<PlayerMarker>())
                playerHandled = true;
        }

        if (!playerHandled)
            throw new Exception("Did not find player to apply multicellular species to");

        GameWorld.NotifySpeciesChangedStages();

        // Make sure no queued player species can spawn with the old species
        WorldSimulation.SpawnSystem.ClearSpawnQueue();

        var scene = SceneManager.Instance.LoadScene(MainGameState.MulticellularEditor);

        var editor = scene.Instantiate<MulticellularEditor>();

        editor.CurrentGame = CurrentGame ?? throw new InvalidOperationException("Stage has no current game");
        editor.ReturnToStage = this;

        GD.Print("Switching to multicellular editor");

        // We don't free this here as the editor will return to this scene
        if (SceneManager.Instance.SwitchToScene(editor, true) != this)
        {
            throw new Exception("failed to keep the current scene root");
        }

        GameWorld.PlayerSpecies.Endosymbiosis.CancelAllEndosymbiosisTargets();

        // TODO: The multicellular stage needs to be able to track statistics and not break organelle unlocks
        GameWorld.UnlockProgress.UnlockAll = true;

        MovingToEditor = false;
    }

    /// <summary>
    ///   Moves to the macroscopic editor (the first time)
    /// </summary>
    public void MoveToMacroscopic()
    {
        if (!HasPlayer || Player.Get<Health>().Dead || !Player.Has<MicrobeColony>())
        {
            GD.PrintErr("Player object disappeared or died (or not in a colony) while trying to become macroscopic");
            return;
        }

        GD.Print("Becoming macroscopic");

        // We don't really need to handle the player state or anything like that here as once we go to the late
        // multicellular editor, we never return to the microbe stage. So we don't need that much setup as becoming
        // multicellular

        // We don't really need to disband the colony here

        GiveReproductionPopulationBonus();

        CurrentGame!.EnterPrototypes();

        var modifiedSpecies = GameWorld.ChangeSpeciesToMacroscopic(Player.Get<SpeciesMember>().Species);

        // Similar code as in the MetaballBodyEditorComponent to prevent the player automatically getting stuck
        // underwater in the awakening stage
        if (modifiedSpecies.MacroscopicType == MacroscopicSpeciesType.Awakened)
        {
            GD.Print("Preventing player from becoming awakened too soon");
            modifiedSpecies.KeepPlayerInAwareStage();
        }

        GameWorld.NotifySpeciesChangedStages();

        var scene = SceneManager.Instance.LoadScene(MainGameState.MacroscopicEditor);

        var editor = scene.Instantiate<MacroscopicEditor>();

        editor.CurrentGame = CurrentGame ?? throw new InvalidOperationException("Stage has no current game");

        // We'll start off in a brand-new stage in the macroscopic part
        editor.ReturnToStage = null;

        GD.Print("Switching to macroscopic editor");

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
            Clouds.AddCloud(Compound.Glucose, 200000.0f,
                Player.Get<WorldPosition>().Position + new Vector3(0.0f, 0.0f, -25.0f));
        }

        // Check win conditions

        if (!CurrentGame!.FreeBuild && GameWorld.PlayerSpecies.Generation >= 20 &&
            GameWorld.PlayerSpecies.Population >= 300 && !wonOnce)
        {
            HUD.ToggleWinBox();
            wonOnce = true;
        }

        var playerSpecies = Player.Get<SpeciesMember>().Species;

        // Update the player environmental properties
        ref var bioProcesses = ref Player.Get<BioProcesses>();

        // Make sure there's no way this cache has outdated values
        ClearResolvedTolerancesCache();

        var environmentalEffects = new MicrobeEnvironmentalEffects
        {
            OsmoregulationMultiplier = 1,
            HealthMultiplier = 1,
            ProcessSpeedModifier = 1,
        };

        var workData1 = new List<Hex>();
        var workData2 = new List<Hex>();
        bool playerHasThermoplast = false;

        // Update the player's cell
        ref var cellProperties = ref Player.Get<CellProperties>();

        bool playerIsMulticellular = Player.Has<MulticellularSpeciesMember>();

        if (playerIsMulticellular)
        {
            ref var earlySpeciesType = ref Player.Get<MulticellularSpeciesMember>();

            // TODO: multicellular tolerances

            // Allow updating the first cell type to reproduce (reproduction order changed)
            earlySpeciesType.MulticellularCellType = earlySpeciesType.Species.Cells[0].CellType;

            cellProperties.ReApplyCellTypeProperties(ref environmentalEffects, Player,
                earlySpeciesType.MulticellularCellType, earlySpeciesType.Species, WorldSimulation, workData1,
                workData2);
        }
        else
        {
            ref var species = ref Player.Get<MicrobeSpeciesMember>();

            var resolvedTolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(
                MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(species.Species, CurrentBiome));

            environmentalEffects.ApplyEffects(resolvedTolerances, ref bioProcesses);

            cellProperties.ReApplyCellTypeProperties(ref environmentalEffects, Player,
                species.Species, species.Species, WorldSimulation,
                workData1, workData2);

            foreach (var organelle in species.Species.Organelles)
            {
                if (organelle.Definition.HasHeatCollection)
                {
                    playerHasThermoplast = true;
                    break;
                }
            }
        }

        Player.Set(environmentalEffects);

        var playerCompounds = Player.Get<CompoundStorage>().Compounds;

        var playerPosition = Player.Get<WorldPosition>().Position;

        // Setup handling for what happens to compounds after reproduction
        Dictionary<Compound, float>? topUpToCustom = null;
        bool topUp = false;

        switch (GameWorld.WorldSettings.Difficulty.ReproductionCompounds)
        {
            case ReproductionCompoundHandling.SplitWithSister:
                // Default handling
                break;
            case ReproductionCompoundHandling.KeepAsIs:
                topUpToCustom = playerCompounds.Compounds.CloneShallow();
                break;
            case ReproductionCompoundHandling.TopUpWithInitial:
                topUp = true;
                break;
            case ReproductionCompoundHandling.TopUpOnPatchChange:
                if (switchedPatchInEditorForCompounds)
                {
                    topUp = true;
                }

                break;
            default:
                GD.PrintErr("Unknown handling of reproduction compounds mode: " +
                    $"{GameWorld.WorldSettings.Difficulty.ReproductionCompounds}");
                break;
        }

        switchedPatchInEditorForCompounds = false;

        // Spawn another cell from the player species
        // This needs to be done after updating the player so that multicellular organisms are accurately separated
        cellProperties.Divide(ref Player.Get<OrganelleContainer>(), Player, playerSpecies, WorldSimulation,
            this, WorldSimulation.SpawnSystem, (ref EntityRecord daughter) =>
            {
                // Mark as a player-reproduced entity
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

        // Handle the compound modes
        if (topUp)
        {
            // Top up with the player species definitions of the initial compounds
            playerCompounds.AddInitialCompounds(playerSpecies.InitialCompounds);
        }
        else if (topUpToCustom != null)
        {
            foreach (var originalCompound in topUpToCustom)
            {
                // Copy each compound but ignore is useful setting to ensure all are copied and nothing is accidentally
                // wasted (but also don't allow adding past capacity so this specific add method is used)
                playerCompounds.TopUpCompound(originalCompound.Key, originalCompound.Value);
            }
        }

        if (!CurrentGame.TutorialState.Enabled)
        {
            tutorialGUI.EventReceiver?.OnTutorialDisabled();

            if (!tutorialCanceledOnce)
            {
                GD.Print("Showing compounds panel as tutorial has been cancelled");
                HUD.ShowCompoundPanel();
                HUD.ShowEnvironmentPanel();

                tutorialCanceledOnce = true;
            }
        }
        else
        {
            // Show day/night cycle tutorial when entering a patch with sunlight
            if (GameWorld.WorldSettings.DayNightCycleEnabled)
            {
                var patchSunlight = GameWorld.Map.CurrentPatch!.Biome
                    .GetCompound(Compound.Sunlight, CompoundAmountType.Biome).Ambient;

                if (patchSunlight > Constants.DAY_NIGHT_TUTORIAL_LIGHT_MIN)
                {
                    TutorialState.SendEvent(TutorialEventType.MicrobePlayerEnterSunlightPatch, EventArgs.Empty, this);
                }
            }
        }

        // When adding a thermoplast, the environment panel should be made visible
        if (!environmentPanelAutomaticallyOpened && playerHasThermoplast)
        {
            HUD.ShowEnvironmentPanel();
            environmentPanelAutomaticallyOpened = true;
        }

        if (TutorialState.Enabled && !TutorialState.ProcessPanelTutorial.Complete)
        {
            // Ensure the player accepts the glucose, this only really happens when going directly from starting in
            // the editor to the game
            playerCompounds.SetUseful(Compound.Glucose);

            // Give some free glucose to make sure the player doesn't die during the tutorial at a terrible time
            // if they haven't been collecting glucose diligently
            playerCompounds.AddCompound(Compound.Glucose, 1.5f);

            // If the player immediately switched to iron eating, make sure they don't die immediately
            if (playerSpecies.InitialCompounds.TryGetValue(Compound.Iron, out var ironAmount) &&
                ironAmount > MathUtils.EPSILON)
            {
                playerCompounds.SetUseful(Compound.Iron);
                playerCompounds.AddCompound(Compound.Iron, 1.5f);
            }
        }
    }

    public override void OnSuicide()
    {
        if (HasPlayer)
        {
            ref var health = ref Player.Get<Health>();

            // Kill the player even if invulnerable
            health.Invulnerable = false;

            // This doesn't use the microbe damage calculation as this damage can't be resisted
            // And Kill() would skip the population penalty
            health.CurrentHealth = 0;

            // Force digestion to complete immediately
            if (Player.Has<Engulfable>())
            {
                ref var engulfable = ref Player.Get<Engulfable>();

                if (engulfable.PhagocytosisStep is not (PhagocytosisPhase.None or PhagocytosisPhase.Ejection))
                {
                    GD.Print("Forcing player digestion to progress much faster");

                    // Seems like there's no very good way to force digestion to complete immediately, so instead we
                    // clear everything here to force the digestion to complete immediately
                    engulfable.AdditionalEngulfableCompounds?.Clear();

                    ref var storage = ref Player.Get<CompoundStorage>();
                    storage.Compounds.ClearCompounds();
                }
            }

            // Force player despawn to happen if there is a problem that prevented the player timed life from being
            // added
            if (!Player.Has<TimedLife>())
            {
                Player.Set<TimedLife>();
            }

            ref var timed = ref Player.Get<TimedLife>();
            timed.FadeTimeRemaining =
                Math.Min(Constants.MAX_PLAYER_DYING_TIME, timed.FadeTimeRemaining ?? float.MaxValue);
        }
    }

    protected override void SetupStage()
    {
        EnsureWorldSimulationIsCreated();

        // Initialise the simulation on a basic level first to ensure the base stage setup has all the objects it needs
        WorldSimulation.Init(rootOfDynamicallySpawned, Clouds, this);

        patchManager = new PatchManager(WorldSimulation.SpawnSystem, WorldSimulation.ProcessSystem, Clouds,
            WorldSimulation.TimedLifeSystem, worldLight);

        if (IsLoadedFromSave)
        {
            patchManager.ApplySaveState(tempPatchManagerCurrentPatch, tempPatchManagerBrightness);
            tempPatchManagerCurrentPatch = null;
        }

        base.SetupStage();

        // Hook up the simulation to some of the other systems
        WorldSimulation.CameraFollowSystem.Camera = Camera;
        HoverInfo.PhysicalWorld = WorldSimulation.PhysicalWorld;

        // Init the simulation and finish setting up the systems (for example, cloud init happens here)
        WorldSimulation.InitForCurrentGame(CurrentGame!);

        tutorialGUI.EventReceiver = TutorialState;
        HUD.SendObjectsToTutorials(TutorialState);

        ProceduralDataCache.Instance.OnEnterState(MainGameState.MicrobeStage);

        // If this is a new game, place some clouds as a learning tool
        if (!IsLoadedFromSave)
        {
            // Place some phosphates to have something on screen at the start
            Clouds.AddCloud(Compound.Phosphates, 5000.0f, new Vector3(40.0f, 0.0f, 0.5f));
            Clouds.AddCloud(Compound.Phosphates, 20000.0f, new Vector3(45.0f, 0.0f, 0.0f));
            Clouds.AddCloud(Compound.Phosphates, 30000.0f, new Vector3(50.0f, 0.0f, 0.0f));

            // If we are starting with tutorials on, disable extra panels that don't matter right now
            if (TutorialState.Enabled)
            {
                HUD.HideEnvironmentAndCompoundPanels(false);
                waitingForWelcomeTutorialToEnd = true;
            }
        }

        patchManager.CurrentGame = CurrentGame;

        if (IsLoadedFromSave)
        {
            UpdatePatchSettings();
        }

        // Reset any cheat state if there was some active
        CurrentGame!.GameWorld.WorldSettings.Difficulty.ClearGrowthRateLimitOverride();
    }

    protected override void OnGameStarted()
    {
        patchManager.CurrentGame = CurrentGame;

        UpdatePatchSettings(!TutorialState.Enabled);

        SpawnPlayer();

        // Can now register this callback with the game set
        TutorialState.GlucoseCollecting.OnOpened += SetupPlayerForGlucoseCollecting;
        TutorialState.DayNightTutorial.OnOpened += HUD.CloseProcessPanel;
    }

    protected override void SpawnPlayer()
    {
        if (HasPlayer)
            return;

        var spawnLocation = Vector3.Zero;

        if (spawnedPlayer)
        {
            // Random location on respawn
            spawnLocation = new Vector3(random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE), 0,
                random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE));

            WorldSimulation.ClearPlayerLocationDependentCaches();
        }

        var (recorder, _) = SpawnHelpers.SpawnMicrobeWithoutFinalizing(WorldSimulation, this,
            GameWorld.PlayerSpecies,
            spawnLocation, false, (null, 0), out var entityRecord);

        entityRecord.Set(new MicrobeEventCallbacks
        {
            OnReproductionStatus = OnPlayerReproductionStatusChanged,

            OnUnbound = OnPlayerUnbound,
            OnUnbindEnabled = OnPlayerUnbindEnabled,

            OnChemoreceptionInfo = HandlePlayerChemoreception,

            OnIngestedByHostile = OnPlayerEngulfedByHostile,
            OnSuccessfulEngulfment = OnPlayerIngesting,
            OnEngulfmentStorageFull = OnPlayerEngulfmentLimitReached,
            OnEngulfmentStorageNearlyEmpty = OnPlayerEngulfmentNearlyEmpty,
            OnEjectedFromHostileEngulfer = OnPlayerEjectedFromHostileEngulfer,

            OnOrganelleDuplicated = OnPlayerOrganelleDuplicated,

            OnNoticeMessage = OnPlayerNoticeMessage,
        });

        entityRecord.Set<CameraFollowTarget>();

        // Spawn and grab the player
        SpawnHelpers.FinalizeEntitySpawn(recorder, WorldSimulation);
        WorldSimulation.ProcessDelaySpawnedEntitiesImmediately();

        Player = WorldSimulation.FindFirstEntityWithComponent<PlayerMarker>();

        if (!HasAlivePlayer)
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
        // Ensure the can edit status is still up to date as the change signal is triggered with one frame delay
        // In freebuild checks need to be skipped to not block the normal freebuild editor availability logic
        if (!IsPlayerAlive())
        {
            canEdit = false;
        }
        else if (Player.Get<Engulfable>().PhagocytosisStep != PhagocytosisPhase.None)
        {
            canEdit = false;
        }
        else if (CurrentGame?.FreeBuild != true)
        {
            if (Player.Has<MicrobeColony>() && GameWorld.PlayerSpecies is MicrobeSpecies)
            {
                canEdit = false;
            }

            if (!Player.Get<OrganelleContainer>().AllOrganellesDivided)
                canEdit = false;
        }

        base.OnCanEditStatusChanged(canEdit);

        if (!canEdit)
            return;

        if (!Player.Has<MulticellularSpeciesMember>())
            TutorialState.SendEvent(TutorialEventType.MicrobePlayerReadyToEdit, EventArgs.Empty, this);

        if (CurrentGame == null)
        {
            GD.PrintErr("Current game is null when player is ready to edit");
            return;
        }

        // Trigger an auto-save the first time the editor becomes available
        if (!CurrentGame.IsBoolSet("edited_microbe") && Settings.Instance.AutoSaveEnabled.Value)
        {
            // But not if already triggered, this is important when loading such an auto-save so that an immediate save
            // isn't triggered
            if (!CurrentGame.IsBoolSet("first_editor_autosaved"))
            {
                CurrentGame.SetBool("first_editor_autosaved", true);

                Invoke.Instance.QueueForObject(() =>
                {
                    GD.Print("Auto-saving game for the first time editor is available");
                    AutoSave();
                }, this);
            }
        }
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
        ClearResolvedTolerancesCache();

        // TODO: would be nice to skip this if we are loading a save made in the editor as this gets called twice when
        // going back to the stage
        if (patchManager.ApplyChangedPatchSettingsIfNeeded(GameWorld.Map.CurrentPatch!, this))
        {
            if (promptPatchNameChange)
                HUD.ShowPatchName(CurrentPatchName.ToString());

            if (HasPlayer)
            {
                ref var engulfer = ref Player.Get<Engulfer>();

                engulfer.DeleteEngulfedObjects(WorldSimulation);
            }

            switchedPatchInEditorForCompounds = true;
        }
        else
        {
            switchedPatchInEditorForCompounds = false;
        }

        HUD.UpdateEnvironmentalBars(GameWorld.Map.CurrentPatch!.Biome);

        UpdateBackground();

        UpdatePatchLightLevelSettings();

        fluidCurrentDisplay.ApplyBiome(GameWorld.Map.CurrentPatch.BiomeTemplate);
    }

    protected override void OnGameContinuedAsSpecies(Species newPlayerSpecies, Patch inPatch)
    {
        base.OnGameContinuedAsSpecies(newPlayerSpecies, inPatch);

        // Update spawners if staying in the same patch as otherwise they wouldn't be updated and would spawn the
        // obsolete species
        if (inPatch == GameWorld.Map.CurrentPatch)
        {
            patchManager.UpdateSpawners(inPatch, this);
        }
    }

    protected override void OnLightLevelUpdate()
    {
        if (GameWorld.Map.CurrentPatch == null)
            return;

        var currentPatch = GameWorld.Map.CurrentPatch;

        patchManager.UpdatePatchBiome(currentPatch);
        patchManager.UpdateAllPatchLightLevels(currentPatch);

        HUD.UpdateEnvironmentalBars(GameWorld.Map.CurrentPatch.Biome);

        // Updates the background lighting and does various post-effects
        if (templateMaxLightLevel > 0.0f && maxLightLevel > 0.0f)
        {
            // This might need to be refactored for efficiency, but it works for now
            var lightLevel =
                currentPatch.Biome.GetCompound(Compound.Sunlight, CompoundAmountType.Current).Ambient;

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
        Camera.SetBackground(SimulationParameters.Instance.GetBackground(GameWorld.Map.CurrentPatch!.Background));
    }

    private void UpdatePatchLightLevelSettings()
    {
        if (GameWorld.Map.CurrentPatch == null)
            throw new InvalidOperationException("Unknown current patch");

        // This wasn't updated to check if the patch has day / night cycle as it might be plausible in the future
        // that other compounds than sunlight are varying so in those cases stage visuals should probably not update
        maxLightLevel = GameWorld.Map.CurrentPatch.Biome.GetCompound(Compound.Sunlight, CompoundAmountType.Biome)
            .Ambient;
        templateMaxLightLevel = GameWorld.Map.CurrentPatch.BiomeTemplate.Conditions
            .GetCompound(Compound.Sunlight, CompoundAmountType.Biome).Ambient;
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
    ///   Handles showing a movement mode selection prompt when using the keyboard as that seems to be a pretty
    ///   common complaint of many new Thrive players that they can't find the movement mode and change it
    /// </summary>
    /// <param name="delta">Process delta time</param>
    private void HandleMovementModePrompt(double delta)
    {
        if (KeyPromptHelper.InputMethod != ActiveInputMethod.Keyboard)
            return;

        var previous = movementModeShowTimer;
        movementModeShowTimer += delta;

        // Trigger movement mode selection prompt just once
        if (previous < Constants.MOVEMENT_MODE_SELECTION_DELAY &&
            movementModeShowTimer >= Constants.MOVEMENT_MODE_SELECTION_DELAY)
        {
            // But only if it hasn't been permanently dismissed
            if (!Settings.Instance.IsNoticePermanentlyDismissed(DismissibleNotice.MicrobeMovementMode))
            {
                GD.Print("Showing movement mode selection prompt");
                movementModeSelectionPopup.ShowSelection();
            }
            else
            {
                GD.Print("Movement mode selection notice permanently dismissed");
            }
        }
    }

    /// <summary>
    ///   Helper function for transition to multicellular. For normal gameplay this would be not optimal as this
    ///   uses the slow world entity fetching.
    /// </summary>
    /// <returns>Enumerable of all microbes of Player's species</returns>
    private IEnumerable<Entity> GetAllPlayerSpeciesMicrobes()
    {
        if (Player == default)
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

    private void OnSpawnEnemyCheatUsed(object? sender, EventArgs e)
    {
        if (!HasPlayer)
            return;

        var species = GameWorld.Map.CurrentPatch!.SpeciesInPatch.Keys.Where(s => !s.PlayerSpecies).ToList();

        // No enemy species to spawn in this patch
        if (species.Count == 0)
        {
            ToolTipManager.Instance.ShowPopup(Localization.Translate("SPAWN_ENEMY_CHEAT_FAIL"), 2.0f);
            GD.PrintErr("Can't use spawn enemy cheat because this patch does not contain any enemy species");
            return;
        }

        var randomSpecies = species.Random(random);

        var playerPosition = Player.Get<WorldPosition>().Position;

        var (recorder, weight) = SpawnHelpers.SpawnMicrobeWithoutFinalizing(WorldSimulation, this,
            randomSpecies, playerPosition + Vector3.Forward * 20, true, (null, 0), out var entity);

        // Make the cell despawn like normal
        WorldSimulation.SpawnSystem.NotifyExternalEntitySpawned(entity, Constants.MICROBE_DESPAWN_RADIUS_SQUARED,
            weight);

        SpawnHelpers.FinalizeEntitySpawn(recorder, WorldSimulation);
    }

    private void OnDuplicatePlayerCheatUsed(object? sender, EventArgs e)
    {
        if (!HasAlivePlayer)
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

        cellProperties.Divide(ref organelles, Player, playerSpecies, WorldSimulation, this,
            WorldSimulation.SpawnSystem, null, MulticellularSpawnState.ChanceForFullColony);
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
                if (!player.IsAlive)
                {
                    GD.PrintErr("Got player engulfed callback but player entity is dead");
                    OnCanEditStatusChanged(false);
                    return;
                }

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
    private void OnPlayerEngulfmentNearlyEmpty(Entity player)
    {
        Invoke.Instance.QueueForObject(
            () => TutorialState.SendEvent(TutorialEventType.MicrobePlayerEngulfmentNotFull, EventArgs.Empty, this),
            this);
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerOrganelleDuplicated(Entity player, PlacedOrganelle organelle)
    {
        if (organelle.Definition.InternalName == cytoplasm.InternalName)
            return;

        Invoke.Instance.QueueForObject(() =>
                TutorialState.SendEvent(TutorialEventType.MicrobeNonCytoplasmOrganelleDivided, EventArgs.Empty, this),
            this);
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

        var position = microbe.Get<WorldPosition>().Position;

        // This must be ran on the main thread. For now this should be fine to allocate a bit of memory capturing
        // the parameters here.
        Invoke.Instance.QueueForObject(
            () => UpdateChemoreceptionLines(activeCompoundDetections, activeSpeciesDetections, position), this);
    }

    private void UpdateChemoreceptionLines(
        List<(Compound Compound, Color Colour, Vector3 Target)>? activeCompoundDetections,
        List<(Species Species, Entity Entity, Color Colour, Vector3 Target)>? activeSpeciesDetections, Vector3 position)
    {
        int currentLineIndex = 0;

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
        try
        {
            return HasPlayer && !Player.Get<Health>().Dead;
        }
        catch (Exception e)
        {
            GD.PrintErr("Couldn't read player health: " + e);
            return false;
        }
    }

    private void ClearResolvedTolerancesCache()
    {
        lock (resolvedTolerancesCache)
        {
            resolvedTolerancesCache.Clear();
        }
    }

    private void SetupPlayerForGlucoseCollecting()
    {
        // Reduce player glucose amount to have enough storage space to collect stuff
        if (!HasAlivePlayer)
        {
            GD.PrintErr("Cannot adjust player glucose as no alive player exists");
            return;
        }

        var compounds = Player.Get<CompoundStorage>().Compounds;

        var glucoseMax = compounds.GetCapacityForCompound(Compound.Glucose);

        var excess = compounds.GetCompoundAmount(Compound.Glucose) -
            (glucoseMax - Constants.TUTORIAL_GLUCOSE_MAKE_EMPTY_SPACE_AT_LEAST);

        if (excess > 0)
            compounds.TakeCompound(Compound.Glucose, excess);
    }

    private void TranslationsForFeaturesToReimplement()
    {
        // TODO: reimplement the microbe features that depend on these translations
        Localization.Translate("SUCCESSFUL_KILL");
        Localization.Translate("SUCCESSFUL_SCAVENGE");
        Localization.Translate("ESCAPE_ENGULFING");
    }
}
