using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main class for managing the late multicellular stage
/// </summary>
[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/late_multicellular_stage/MulticellularStage.tscn")]
[DeserializedCallbackTarget]
[UseThriveSerializer]
public class MulticellularStage : StageBase<MulticellularCreature>
{
    [Export]
    public NodePath? InteractableSystemPath;

    [Export]
    public NodePath InteractionPopupPath = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private SpawnSystem dummySpawner = null!;

#pragma warning disable CA2213
    private InteractableSystem interactableSystem = null!;
    private InteractablePopup interactionPopup = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Used to detect when the player automatically advances stages in the editor (awakening is explicit with a
    ///   button as it should be only used after moving to land)
    /// </summary>
    [JsonProperty]
    private MulticellularSpeciesType previousPlayerStage;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public MulticellularCamera PlayerCamera { get; private set; } = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public MulticellularHUD HUD { get; private set; } = null!;

    // TODO: create a multicellular equivalent class
    [JsonIgnore]
    public PlayerInspectInfo HoverInfo { get; private set; } = null!;

    protected override IStageHUD BaseHUD => HUD;

    private LocalizedString CurrentPatchName =>
        GameWorld.Map.CurrentPatch?.Name ?? throw new InvalidOperationException("no current patch");

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        HUD.Init(this);

        // HoverInfo.Init(Camera, Clouds);

        interactableSystem.Init(PlayerCamera.CameraNode, rootOfDynamicallySpawned);
        interactionPopup.OnInteractionSelectedHandler += ForwardInteractionSelectionToPlayer;

        SetupStage();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        HUD = GetNode<MulticellularHUD>("MulticellularHUD");
        HoverInfo = GetNode<PlayerInspectInfo>("PlayerLookingAtInfo");

        interactableSystem = GetNode<InteractableSystem>(InteractableSystemPath);
        interactionPopup = GetNode<InteractablePopup>(InteractionPopupPath);

        // TODO: implement late multicellular specific look at info, for now it's disabled by removing it
        HoverInfo.Free();

        PlayerCamera = world.GetNode<MulticellularCamera>("PlayerCamera");

        // These need to be created here as well for child property save load to work
        // TODO: systems

        // We don't actually spawn anything currently, and anyway will want a different spawn system for late
        // multicellular
        dummySpawner = new SpawnSystem(rootOfDynamicallySpawned);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (gameOver)
            return;

        if (playerExtinctInCurrentPatch)
            return;

        if (Player != null)
        {
            if (Player.Species.MulticellularType == MulticellularSpeciesType.Awakened)
            {
                // TODO: player interaction reach modifier from the species
                interactableSystem.UpdatePlayerPosition(Player.GlobalTranslation, 0);
                interactableSystem.SetActive(true);
            }
            else
            {
                interactableSystem.SetActive(false);
            }
        }

        // TODO: notify metrics
    }

    public override void StartMusic()
    {
        Jukebox.Instance.PlayCategory("LateMulticellularStage");
    }

    public override void OnFinishLoading(Save save)
    {
        OnFinishLoading();
    }

    public override void StartNewGame()
    {
        CurrentGame = GameProperties.StartNewLateMulticellularGame(new WorldGenerationSettings());

        UpdatePatchSettings();

        base.StartNewGame();
    }

    // TODO: different pause key as space will be for jumping
    // [RunOnKeyDown("g_pause")]
    public void PauseKeyPressed()
    {
        // Check nothing else has keyboard focus and pause the game
        if (HUD.GetFocusOwner() == null)
        {
            HUD.PauseButtonPressed(!HUD.Paused);
        }
    }

    public override void MoveToEditor()
    {
        if (Player?.Dead != false)
        {
            GD.PrintErr("Player object disappeared or died while transitioning to the editor");
            return;
        }

        if (CurrentGame == null)
            throw new InvalidOperationException("Stage has no current game");

        var scene = SceneManager.Instance.LoadScene(MainGameState.LateMulticellularEditor);

        Node sceneInstance = scene.Instance();
        var editor = (LateMulticellularEditor)sceneInstance;

        editor.CurrentGame = CurrentGame;
        editor.ReturnToStage = this;

        GiveReproductionPopulationBonus();

        // We don't free this here as the editor will return to this scene
        if (SceneManager.Instance.SwitchToScene(sceneInstance, true) != this)
        {
            throw new Exception("failed to keep the current scene root");
        }

        MovingToEditor = false;
    }

    public override void OnReturnFromEditor()
    {
        UpdatePatchSettings();

        base.OnReturnFromEditor();

        // TODO:
        // // Spawn free food if difficulty settings call for it
        // if (GameWorld.WorldSettings.FreeGlucoseCloud)
        // {
        // }

        // Spawn another cell from the player species
        // This is done first to ensure that the player colony is still intact for spawn separation calculation
        var parent = Player!.SpawnOffspring();
        parent.BecomeFullyGrown();

        // Update the player's creature
        Player.ApplySpecies(Player.Species);

        // Reset all growth progress of the player
        Player.ResetGrowth();

        if (!CurrentGame!.TutorialState.Enabled)
        {
            // tutorialGUI.EventReceiver?.OnTutorialDisabled();
        }

        // Update state transition triggers
        if (Player.Species.MulticellularType != previousPlayerStage)
        {
            previousPlayerStage = Player.Species.MulticellularType;

            if (previousPlayerStage == MulticellularSpeciesType.Aware)
            {
                // Intentionally not translatable as a placeholder prototype text
                HUD.HUDMessages.ShowMessage(
                    "You are now aware. This prototype has nothing extra yet, please move to the Awakening Stage.",
                    DisplayDuration.Long);
            }
            else if (previousPlayerStage == MulticellularSpeciesType.Awakened)
            {
                // TODO: something
            }
        }
    }

    public override void OnSuicide()
    {
        Player?.Damage(9999.0f, "suicide");
    }

    public void RotateCamera(float yawMovement, float pitchMovement)
    {
        PlayerCamera.XRotation += pitchMovement;
        PlayerCamera.YRotation += yawMovement;
    }

    /// <summary>
    ///   Temporary land part of the prototype, called from the HUD
    /// </summary>
    public void TeleportToLand()
    {
        if (Player == null)
        {
            GD.PrintErr("Player has disappeared");
            return;
        }

        // Despawn everything except the player
        foreach (Node child in rootOfDynamicallySpawned.GetChildren())
        {
            if (child != Player)
                child.QueueFree();
        }

        // And setup the land "environment"

        // Ground plane
        var ground = new StaticBody
        {
            PhysicsMaterialOverride = new PhysicsMaterial
            {
                Friction = 1,
                Bounce = 0.1f,
                Absorbent = true,
                Rough = true,
            },
        };

        ground.AddChild(new CollisionShape
        {
            Shape = new PlaneShape
            {
                Plane = new Plane(new Vector3(0, 1, 0), 0),
            },
        });

        ground.AddChild(new MeshInstance
        {
            Mesh = new PlaneMesh
            {
                Size = new Vector2(400, 400),
                Material = new SpatialMaterial
                {
                    AlbedoTexture = GD.Load<Texture>("res://assets/textures/environment/Terrain_01_Albedo.png"),
                    NormalEnabled = true,
                    NormalTexture = GD.Load<Texture>("res://assets/textures/environment/Terrain_01_Normals.png"),
                    Uv1Scale = new Vector3(42, 42, 42),
                },
            },
        });

        rootOfDynamicallySpawned.AddChild(ground);

        // A not super familiar (different than underwater) rock strewn around for reference
        var rockResource =
            new SimpleWorldResource(GD.Load<PackedScene>("res://assets/models/Iron4.tscn"), "RESOURCE_ROCK");
        var resourceScene = SpawnHelpers.LoadResourceEntityScene();

        foreach (var position in new[]
                 {
                     new Vector3(10, 0, 5),
                     new Vector3(15, 0, 5),
                     new Vector3(10, 0, 8),
                     new Vector3(-3, 0, 5),
                     new Vector3(-8, 0, 6),
                     new Vector3(18, 0, 11),
                     new Vector3(38, 0, 11),
                     new Vector3(-15, 0, 10),
                     new Vector3(-15, 0, -15),
                 })
        {
            // But create it as a resource entity so that it can be interacted with
            // TODO: is y-offset needed? still, maybe 0.01f
            SpawnHelpers.SpawnResourceEntity(rockResource, new Transform(Basis.Identity, position),
                rootOfDynamicallySpawned, resourceScene, true);
        }

        // Modify player state for being on land
        Player.MovementMode = MovementMode.Walking;

        if (Player.Translation.y <= 0)
        {
            Player.Translation = new Vector3(Player.Translation.x, 0.1f, Player.Translation.z);
        }

        // Modify the player species to be on land
        Player.Species.ReproductionLocation = ReproductionLocation.Land;

        // Fade back in after the "teleport"
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.3f, null, false);
    }

    public void MoveToAwakeningStage()
    {
        if (Player == null)
            return;

        GD.Print("Moving player to awakening stage prototype");

        Player.Species.MovePlayerToAwakenedStatus();

        // Intentionally not translated prototype message
        HUD.HUDMessages.ShowMessage(
            "You are now in the Awakening Stage prototype. You can now interact with more world objects. " +
            "Interact with tool parts to advance.", DisplayDuration.Long);
    }

    public void AttemptPlayerWorldInteraction()
    {
        // TODO: we might in the future have somethings that an aware creature can interact with
        if (Player == null || Player.Species.MulticellularType != MulticellularSpeciesType.Awakened)
            return;

        var target = interactableSystem.GetInteractionTarget();

        if (target == null)
        {
            // Did not find anything for the player to interact with
            HUD.HUDMessages.ShowMessage(TranslationServer.Translate("NOTHING_TO_INTERACT_WITH"), DisplayDuration.Short);
            return;
        }

        // Show interaction context menu for the player to do something with the target
        interactionPopup.ShowForInteractable(target, Player.CalculatePossibleActions(target));
    }

    protected override void SetupStage()
    {
        base.SetupStage();

        // if (!IsLoadedFromSave)
        //     spawner.Init();

        // TODO: implement
        if (!IsLoadedFromSave)
        {
            // If this is a new game (first time entering the stage), start the camera in top down view
            // as a learning tool
            if (!CurrentGame!.IsBoolSet("played_multicellular"))
            {
                CurrentGame.SetBool("played_multicellular", true);

                // TODO: change the view
            }

            // TODO: remove
            // Spawn a chunk to give the player some navigation reference
            var mesh = new ChunkConfiguration.ChunkScene
            {
                ScenePath = "res://assets/models/Iron5.tscn",
                ConvexShapePath = "res://assets/models/Iron5.shape",
            };
            mesh.LoadScene();
            SpawnHelpers.SpawnChunk(new ChunkConfiguration
                {
                    Name = "test",
                    Size = 10000,
                    Radius = 10,
                    Mass = 100,
                    ChunkScale = 1,
                    Meshes = new List<ChunkConfiguration.ChunkScene> { mesh },
                }, new Vector3(3, 0, -15), rootOfDynamicallySpawned, SpawnHelpers.LoadChunkScene(),
                random);
        }

        // patchManager.CurrentGame = CurrentGame;

        if (IsLoadedFromSave)
        {
            UpdatePatchSettings();
        }
    }

    protected override void OnGameStarted()
    {
        // patchManager.CurrentGame = CurrentGame;

        lightCycle.ApplyWorldSettings(GameWorld.WorldSettings);
        UpdatePatchSettings();

        SpawnPlayer();
    }

    protected override void UpdatePatchSettings(bool promptPatchNameChange = true)
    {
        // TODO: would be nice to skip this if we are loading a save made in the editor as this gets called twice when
        // going back to the stage
        // if (patchManager.ApplyChangedPatchSettingsIfNeeded(GameWorld.Map.CurrentPatch!))
        // {
        if (promptPatchNameChange)
            HUD.ShowPatchName(CurrentPatchName.ToString());

        // }

        HUD.UpdateEnvironmentalBars(GameWorld.Map.CurrentPatch!.Biome);

        // TODO: load background graphics
    }

    protected override void SpawnPlayer()
    {
        if (HasPlayer)
            return;

        Player = SpawnHelpers.SpawnCreature(GameWorld.PlayerSpecies, new Vector3(0, 0, 0),
            rootOfDynamicallySpawned, SpawnHelpers.LoadMulticellularScene(), false, dummySpawner, CurrentGame!);
        Player.AddToGroup(Constants.PLAYER_GROUP);

        Player.OnDeath = OnPlayerDied;

        Player.OnReproductionStatus = OnPlayerReproductionStatusChanged;

        PlayerCamera.FollowedNode = Player;

        // spawner.DespawnAll();

        if (spawnedPlayer)
        {
            // Random location on respawn
            // Player.Translation = new Vector3(
            //     random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE), 0,
            //     random.Next(Constants.MIN_SPAWN_DISTANCE, Constants.MAX_SPAWN_DISTANCE));

            // spawner.ClearSpawnCoordinates();
        }

        spawnedPlayer = true;
        playerRespawnTimer = Constants.PLAYER_RESPAWN_TIME;
    }

    protected override void AutoSave()
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    protected override void PerformQuickSave()
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (InteractableSystemPath != null)
            {
                InteractableSystemPath.Dispose();
                InteractionPopupPath.Dispose();
            }

            interactionPopup.OnInteractionSelectedHandler -= ForwardInteractionSelectionToPlayer;
        }

        base.Dispose(disposing);
    }

    private void SaveGame(string name)
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    private void OnFinishLoading()
    {
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerDied(MulticellularCreature player)
    {
        HandlePlayerDeath();

        PlayerCamera.FollowedNode = null;
        Player = null;
    }

    [DeserializedCallbackAllowed]
    private void OnPlayerReproductionStatusChanged(MulticellularCreature player, bool ready)
    {
        OnCanEditStatusChanged(ready);
    }

    private void ForwardInteractionSelectionToPlayer(IInteractableEntity entity, InteractionType interactionType)
    {
        if (Player == null)
            return;

        if (!Player.AttemptInteraction(entity, interactionType))
            GD.Print("Player couldn't perform the selected action");
    }
}
